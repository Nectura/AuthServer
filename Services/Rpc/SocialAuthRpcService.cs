using AuthServer.Configuration;
using AuthServer.Database.Enums;
using AuthServer.Database.Models;
using AuthServer.Database.Models.Interfaces;
using AuthServer.Database.Repositories.Interfaces;
using AuthServer.Models.Jwt;
using AuthServer.Models.OAuth;
using AuthServer.Models.UserInfo;
using AuthServer.Services.Interfaces;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace AuthServer.Services.Rpc;

public sealed class SocialAuthRpcService : SocialAuthGrpcService.SocialAuthGrpcServiceBase
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly ISpotifyAuthService _spotifyAuthService;
    private readonly ITwitchAuthService _twitchAuthService;
    private readonly IDiscordAuthService _discordAuthService;
    private readonly ISocialUserRepository _userRepository;
    private readonly ISocialUserRefreshTokenRepository _refreshTokenRepository;
    private readonly ISocialUserAuthProviderTokenRepository _userAuthProviderTokenRepository;
    private readonly ISocialAuthService _socialAuthService;
    private readonly ILogger<SocialAuthRpcService> _logger;
    private readonly IOptions<SocialAuthConfig> _authConfig;

    public SocialAuthRpcService(
        IGoogleAuthService googleAuthService,
        ISpotifyAuthService spotifyAuthService,
        ISocialUserRepository userRepository,
        ISocialUserRefreshTokenRepository refreshTokenRepository,
        ISocialUserAuthProviderTokenRepository userAuthProviderTokenRepository,
        ITwitchAuthService twitchAuthService,
        IDiscordAuthService discordAuthService,
        ISocialAuthService socialAuthService,
        ILogger<SocialAuthRpcService> logger,
        IOptions<SocialAuthConfig> authConfig)
    {
        _googleAuthService = googleAuthService;
        _spotifyAuthService = spotifyAuthService;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _userAuthProviderTokenRepository = userAuthProviderTokenRepository;
        _twitchAuthService = twitchAuthService;
        _discordAuthService = discordAuthService;
        _socialAuthService = socialAuthService;
        _logger = logger;
        _authConfig = authConfig;
    }

    public override Task<InitializeLoginFlowResponse> InitializeLoginFlow(InitializeLoginFlowRequest request, ServerCallContext context)
    {
        var authorizationUrl = request.Provider switch
        {
            ESocialProvider.Google => _googleAuthService.GetAuthorizationUrl(request.RedirectUrl, request.UseExtendedScopes),
            ESocialProvider.Spotify => _spotifyAuthService.GetAuthorizationUrl(request.RedirectUrl, request.UseExtendedScopes),
            ESocialProvider.Twitch => _twitchAuthService.GetAuthorizationUrl(request.RedirectUrl, request.UseExtendedScopes),
            ESocialProvider.Discord => _discordAuthService.GetAuthorizationUrl(request.RedirectUrl, request.UseExtendedScopes),
            _ => throw new RpcException(new Status(StatusCode.FailedPrecondition, "Invalid auth provider"))
        };
        return Task.FromResult(new InitializeLoginFlowResponse
        {
            AuthUrl = authorizationUrl
        });
    }

    public override async Task<SocialLoginResponse> RefreshAccessToken(SocialUserRefreshAccessTokenRequest request, ServerCallContext context)
    {
        var userToken = await _refreshTokenRepository
            .Query(m => m.RefreshToken == request.RefreshToken || m.PreviousRefreshToken == request.RefreshToken)
            .Include(m => m.User)
            .FirstOrDefaultAsync(context.CancellationToken);
        
        if (userToken == default)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid refresh token"));

        if (userToken.HasExpired)
        {
            _refreshTokenRepository.Remove(userToken);
            await _refreshTokenRepository.SaveChangesAsync(context.CancellationToken);
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Expired refresh token"));
        }
        
        if (userToken.PreviousRefreshToken == request.RefreshToken)
        {
            _refreshTokenRepository.Remove(userToken);
            await _refreshTokenRepository.SaveChangesAsync(context.CancellationToken);
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Security breach detected"));
        }
        
        var authProviderAccessToken = await _userAuthProviderTokenRepository
            .FindAsync(context.CancellationToken, userToken.UserId, userToken.User!.AuthProvider);
                
        if (authProviderAccessToken == default)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Failed to find the auth provider access token"));
        
        JwtCredential jwtCredential;

        switch (authProviderAccessToken.AuthProvider)
        {
            case EAuthProvider.Google:
            {
                var userInfo = (GoogleUserInfo) await _googleAuthService.GetUserInfoAsync(authProviderAccessToken.AccessToken, context.CancellationToken);
                jwtCredential = _googleAuthService.CreateJwtCredential(userInfo);
                break;
            }
            case EAuthProvider.Spotify:
            {
                var userInfo = (SpotifyUserInfo) await _spotifyAuthService.GetUserInfoAsync(authProviderAccessToken.AccessToken, context.CancellationToken);
                jwtCredential = _spotifyAuthService.CreateJwtCredential(userInfo);
                break;
            }
            case EAuthProvider.Twitch:
            {
                var userInfo = (TwitchUserInfo) await _twitchAuthService.GetUserInfoAsync(authProviderAccessToken.AccessToken, context.CancellationToken);
                jwtCredential = _twitchAuthService.CreateJwtCredential(userInfo);
                break;
            }
            case EAuthProvider.Discord:
            {
                var userInfo = (DiscordUserInfo) await _discordAuthService.GetUserInfoAsync(authProviderAccessToken.AccessToken, context.CancellationToken);
                jwtCredential = _discordAuthService.CreateJwtCredential(userInfo);
                break;
            }
            default:
            {
                _logger.LogError("Failed to create jwt credential for auth provider: {AuthProvider}", authProviderAccessToken.AuthProvider);
                throw new RpcException(new Status(StatusCode.Internal, "Failed to create jwt credential"));
            }
        }

        userToken.PreviousRefreshToken = userToken.RefreshToken;
        userToken.RefreshToken = jwtCredential.RefreshToken;

        await _refreshTokenRepository.SaveChangesAsync(context.CancellationToken);

        return new SocialLoginResponse
        {
            AccessToken = jwtCredential.AccessToken,
            RefreshToken = jwtCredential.RefreshToken
        };
    }

    public override async Task<SocialLoginResponse> GoogleLogin(GoogleLoginRequest request, ServerCallContext context)
    {
        GoogleUserInfo userInfo;

        try
        {
            userInfo = (GoogleUserInfo) await _googleAuthService.GetUserInfoAsync(request.IdToken, context.CancellationToken);
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Failed to get user info"));
        }
        
        if (!_socialAuthService.ValidateRequest(request.State, userInfo.Nonce))
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Failed state/nonce validation"));
        
        var socialUser = await CreateSocialUserAsync(userInfo.EmailAddress, userInfo.Name, EAuthProvider.Google, userInfo.AuthProviderUserId, cancellationToken: context.CancellationToken);

        userInfo.Id = socialUser.Id;

        var jwtCredential = _googleAuthService.CreateJwtCredential(userInfo);

        await SaveRefreshTokenAsync(userInfo.Id, jwtCredential.RefreshToken, context.CancellationToken);
        
        return new SocialLoginResponse
        {
            AccessToken = jwtCredential.AccessToken,
            RefreshToken = jwtCredential.RefreshToken
        };
    }

    public override async Task<SocialLoginResponse> SpotifyLogin(SpotifyLoginRequest request, ServerCallContext context)
    {
        if (!_socialAuthService.ValidateRequest(request.State))
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Failed state/nonce validation"));
        
        var tokenExchangeResponse = await _spotifyAuthService.ExchangeAuthCodeForAccessTokenAsync(request.AuthCode, request.RedirectUrl, context.CancellationToken);

        SpotifyUserInfo userInfo;

        try
        {
            userInfo = (SpotifyUserInfo) await _spotifyAuthService.GetUserInfoAsync(tokenExchangeResponse.AccessToken, context.CancellationToken);
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Failed to get user info"));
        }
        
        var socialUser = await CreateSocialUserAsync(userInfo.EmailAddress, userInfo.Name, EAuthProvider.Spotify, userInfo.AuthProviderUserId, tokenExchangeResponse, context.CancellationToken);

        userInfo.Id = socialUser.Id;

        var jwtCredential = _spotifyAuthService.CreateJwtCredential(userInfo);

        await SaveRefreshTokenAsync(userInfo.Id, jwtCredential.RefreshToken, context.CancellationToken);
        
        return new SocialLoginResponse
        {
            AccessToken = jwtCredential.AccessToken,
            RefreshToken = jwtCredential.RefreshToken
        };
    }

    public override async Task<SocialLoginResponse> TwitchLogin(TwitchLoginRequest request, ServerCallContext context)
    {
        var tokenExchangeResponse = await _twitchAuthService.ExchangeAuthCodeForAccessTokenAsync(request.AuthCode, request.RedirectUrl, context.CancellationToken);
        var isValid = await _twitchAuthService.ValidateAccessTokenWithNonceClaimAsync(tokenExchangeResponse.AccessToken, request.State, context.CancellationToken);
        
        if (!isValid)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Failed state/nonce validation"));

        TwitchUserInfo userInfo;

        try
        {
            userInfo = (TwitchUserInfo) await _twitchAuthService.GetUserInfoAsync(tokenExchangeResponse.AccessToken, context.CancellationToken);
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Failed to get user info"));
        }

        var socialUser = await CreateSocialUserAsync(userInfo.EmailAddress, userInfo.Name, EAuthProvider.Twitch, userInfo.AuthProviderUserId, tokenExchangeResponse, context.CancellationToken);
        
        userInfo.Id = socialUser.Id;

        var jwtCredential = _twitchAuthService.CreateJwtCredential(userInfo);

        await SaveRefreshTokenAsync(socialUser.Id, jwtCredential.RefreshToken, context.CancellationToken);

        return new SocialLoginResponse
        {
            AccessToken = jwtCredential.AccessToken,
            RefreshToken = jwtCredential.RefreshToken
        };
    }

    private async Task SaveRefreshTokenAsync(Guid userId, string refreshToken, CancellationToken cancellationToken = default)
    {
        _refreshTokenRepository.Add(new SocialUserRefreshToken
        {
            UserId = userId,
            RefreshToken = refreshToken,
            AbsoluteExpirationTime = DateTime.UtcNow.Add(JwtAuthConfig.JwtMaxLifeSpan)
        });
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<ISocialUser> CreateSocialUserAsync(
        string emailAddress,
        string userName,
        EAuthProvider authProvider,
        string? authProviderUserId = default,
        OAuthAccessTokenExchangeResponse? tokenExchangeResponse = default,
        CancellationToken cancellationToken = default)
    {
        var (entity, _) = await _userRepository.FindOrCreateAsync(m => m.EmailAddress == emailAddress, () =>
            new SocialUser
            {
                AuthProviderUserId = authProviderUserId,
                Name = userName,
                EmailAddress = emailAddress,
                AuthProvider = authProvider
            }, cancellationToken);
        
        if (tokenExchangeResponse != default)
            await StoreAccessTokenAsync(entity.Id, authProvider, tokenExchangeResponse, cancellationToken);
        
        return entity;
    }

    private async Task StoreAccessTokenAsync(
        Guid socialUserId,
        EAuthProvider authProvider,
        OAuthAccessTokenExchangeResponse tokenExchangeResponse,
        CancellationToken cancellationToken = default)
    {
        var authProviderToken = await _userAuthProviderTokenRepository.FindAsync(cancellationToken, socialUserId, authProvider);
        if (authProviderToken == default)
            _userAuthProviderTokenRepository.Add(new SocialUserAuthProviderToken
            {
                UserId = socialUserId,
                AccessToken = tokenExchangeResponse.AccessToken,
                RefreshToken = tokenExchangeResponse.RefreshToken,
                Scopes = JsonConvert.SerializeObject(tokenExchangeResponse.Scope),
                AuthProvider = authProvider,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenExchangeResponse.ExpiresInSeconds)
            });
        authProviderToken?.UpdateFromTokenExchangeResponse(tokenExchangeResponse);
        await _userAuthProviderTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
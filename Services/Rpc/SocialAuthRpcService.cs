using AuthServer.Database.Enums;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories.Interfaces;
using AuthServer.Services.Interfaces;
using Google.Apis.Auth;
using Grpc.Core;

namespace AuthServer.Services.Rpc;

public sealed class SocialAuthRpcService : SocialAuthGrpcService.SocialAuthGrpcServiceBase
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly ISpotifyAuthService _spotifyAuthService;
    private readonly GoogleJsonWebSignature.ValidationSettings _googleValidationSettings;
    private readonly ISocialUserRepository _userRepository;
    private readonly ISocialUserRefreshTokenRepository _refreshTokenRepository;

    public SocialAuthRpcService(
        IGoogleAuthService googleAuthService,
        ISpotifyAuthService spotifyAuthService,
        GoogleJsonWebSignature.ValidationSettings googleValidationSettings,
        ISocialUserRepository userRepository,
        ISocialUserRefreshTokenRepository refreshTokenRepository)
    {
        _googleAuthService = googleAuthService;
        _spotifyAuthService = spotifyAuthService;
        _googleValidationSettings = googleValidationSettings;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public override async Task<SocialLoginResponse> GoogleLogin(GoogleLoginRequest request, ServerCallContext context)
    {
        GoogleJsonWebSignature.Payload validationResponse;

        try
        {
            validationResponse = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, _googleValidationSettings);
        }
        catch (InvalidJwtException)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid Google auth token"));
        }

        var (user, _) = await _userRepository.FindOrCreateAsync(m => m.EmailAddress == validationResponse.Email, () => new SocialUser
        {
            Name = validationResponse.Name,
            EmailAddress = validationResponse.Email,
            AuthProvider = EAuthProvider.Google
        }, context.CancellationToken);

        var jwtCredential = _googleAuthService.CreateJwtCredential(user);
        
        return new SocialLoginResponse
        {
            AccessToken = jwtCredential.AccessToken,
            RefreshToken = jwtCredential.RefreshToken
        };
    }

    public override async Task<SocialLoginResponse> SpotifyLogin(SpotifyLoginRequest request, ServerCallContext context)
    {
        var tokenExchangeResponse = await _spotifyAuthService.ExchangeAuthCodeForAccessTokenAsync(request.AuthCode, request.RedirectUrl, context.CancellationToken);

        var userInfo = await _spotifyAuthService.GetUserInfoAsync(tokenExchangeResponse.AccessToken, context.CancellationToken);

        var (user, _) = await _userRepository.FindOrCreateAsync(m => m.EmailAddress == userInfo, () => new SocialUser
        {
            Name = userInfo,
            EmailAddress = userInfo,
            AuthProvider = EAuthProvider.Spotify
        }, context.CancellationToken);

        // Note: improvement for this segment and similar ones would be to execute this db operation outside the scope of this controller action execution since it doesn't depend on the outcome.
        _refreshTokenRepository.Add(new SocialUserRefreshToken
        {
            AccessToken = tokenExchangeResponse.AccessToken,
            RefreshToken = tokenExchangeResponse.RefreshToken,
            AuthProvider = EAuthProvider.Spotify,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenExchangeResponse.ExpiresInSeconds)
        });

        await _refreshTokenRepository.SaveChangesAsync(context.CancellationToken);
        // Note: end segment

        var jwtCredential = _spotifyAuthService.CreateJwtCredential(user);

        return new SocialLoginResponse
        {
            AccessToken = jwtCredential.AccessToken,
            RefreshToken = jwtCredential.RefreshToken
        };
    }
}
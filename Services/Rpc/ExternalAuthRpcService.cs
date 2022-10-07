using AuthServer.Database.Enums;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.Services.Auth.External;
using Google.Apis.Auth;
using Grpc.Core;

namespace AuthServer.Services.Rpc;

public sealed class ExternalAuthRpcService : AuthServer.ExternalAuthRpcService.ExternalAuthRpcServiceBase
{
    private readonly GoogleAuthService _googleAuthService;
    private readonly SpotifyAuthService _spotifyAuthService;
    private readonly GoogleJsonWebSignature.ValidationSettings _googleValidationSettings;
    private readonly ExternalUserRepository _userRepository;
    private readonly ExternalUserRefreshTokenRepository _refreshTokenRepository;

    public ExternalAuthRpcService(
        GoogleAuthService googleAuthService,
        SpotifyAuthService spotifyAuthService,
        GoogleJsonWebSignature.ValidationSettings googleValidationSettings,
        ExternalUserRepository userRepository,
        ExternalUserRefreshTokenRepository refreshTokenRepository)
    {
        _googleAuthService = googleAuthService;
        _spotifyAuthService = spotifyAuthService;
        _googleValidationSettings = googleValidationSettings;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public override async Task<ExternalLoginResponse> GoogleLogin(GoogleLoginRequest request, ServerCallContext context)
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

        var (user, _) = await _userRepository.FindOrCreateAsync(m => m.EmailAddress == validationResponse.Email, () => new ExternalUser
        {
            Name = validationResponse.Name,
            EmailAddress = validationResponse.Email,
            AuthProvider = EAuthProvider.Google
        }, context.CancellationToken);

        var jwtCredential = _googleAuthService.CreateJwtCredential(user);
        
        return new ExternalLoginResponse
        {
            AccessToken = jwtCredential.AccessToken,
            RefreshToken = jwtCredential.RefreshToken
        };
    }

    public override async Task<ExternalLoginResponse> SpotifyLogin(SpotifyLoginRequest request, ServerCallContext context)
    {
        var tokenExchangeResponse = await _spotifyAuthService.ExchangeAuthCodeForAccessTokenAsync(request.AuthCode, request.RedirectUrl, context.CancellationToken);

        var userInfo = await _spotifyAuthService.GetUserInfoAsync(tokenExchangeResponse.AccessToken, context.CancellationToken);

        var (user, _) = await _userRepository.FindOrCreateAsync(m => m.EmailAddress == userInfo, () => new ExternalUser
        {
            Name = userInfo,
            EmailAddress = userInfo,
            AuthProvider = EAuthProvider.Spotify
        }, context.CancellationToken);

        // Note: improvement for this segment and similar ones would be to execute this db operation outside the scope of this controller action execution since it doesn't depend on the outcome.
        _refreshTokenRepository.Add(new ExternalUserRefreshToken
        {
            AccessToken = tokenExchangeResponse.AccessToken,
            RefreshToken = tokenExchangeResponse.RefreshToken,
            AuthProvider = EAuthProvider.Spotify,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenExchangeResponse.ExpiresInSeconds)
        });

        await _refreshTokenRepository.SaveChangesAsync(context.CancellationToken);
        // Note: end segment

        var jwtCredential = _spotifyAuthService.CreateJwtCredential(user);

        return new ExternalLoginResponse
        {
            AccessToken = jwtCredential.AccessToken,
            RefreshToken = jwtCredential.RefreshToken
        };
    }
}
using AuthServer.Configuration;
using AuthServer.Models.UserInfo;
using AuthServer.Models.UserInfo.Interfaces;
using AuthServer.Services.Abstract;
using AuthServer.Services.Interfaces;
using Google.Apis.Auth;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace AuthServer.Services;

public sealed class GoogleAuthService : AuthProviderService, IGoogleAuthService
{
    private readonly GoogleJsonWebSignature.ValidationSettings _googleValidationSettings;
    
    public GoogleAuthService(
        HttpClient httpClient,
        IOptions<GoogleAuthConfig> authProviderConfig,
        IOptions<JwtAuthConfig> authConfig,
        ISocialAuthService socialAuthService,
        GoogleJsonWebSignature.ValidationSettings googleValidationSettings) : base(httpClient, authProviderConfig, authConfig, socialAuthService)
    {
        _googleValidationSettings = googleValidationSettings;
    }

    public override async Task<IUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        GoogleJsonWebSignature.Payload validationResponse;

        try
        {
            validationResponse = await GoogleJsonWebSignature.ValidateAsync(accessToken, _googleValidationSettings);
        }
        catch (InvalidJwtException)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid Google auth token"));
        }

        return new GoogleUserInfo
        {
            EmailAddress = validationResponse.Email,
            Name = validationResponse.Name,
            ProfileImage = validationResponse.Picture,
            Nonce = validationResponse.Nonce
        };
    }
}
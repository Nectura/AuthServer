using System.Net;
using System.Security.Claims;
using AuthServer.Configuration;
using AuthServer.Exceptions;
using AuthServer.Extensions;
using AuthServer.Models.OAuth;
using AuthServer.Models.UserInfo;
using AuthServer.Models.UserInfo.Interfaces;
using AuthServer.Services.Abstract;
using AuthServer.Services.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using TwitchLib.Api.Interfaces;

namespace AuthServer.Services;

public sealed class TwitchAuthService : AuthProviderService, ITwitchAuthService
{
    private readonly ITwitchAPI _twitchApi;
    
    public TwitchAuthService(
        HttpClient httpClient,
        IOptions<TwitchAuthConfig> authProviderConfig,
        IOptions<JwtAuthConfig> authConfig,
        ISocialAuthService socialAuthService,
        ITwitchAPI twitchApi) : base(httpClient, authProviderConfig, authConfig, socialAuthService)
    {
        _twitchApi = twitchApi;
    }

    public override async Task<OAuthAccessTokenExchangeResponse> ExchangeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var requestModel = new OAuthAccessTokenRefreshRequest
        {
            ClientId = _authProviderConfig.AuthProviderClientId,
            ClientSecret = _authProviderConfig.AuthProviderClientSecret,
            RefreshToken = WebUtility.UrlEncode(refreshToken)
        };

        using var restClient = BuildRestClient(_authProviderConfig.AccessTokenExchangeEndpoint);

        var request = new RestRequest(requestModel.GetQueryParametersFromJsonProperties(), Method.Post);
        request.AddOrUpdateHeader("Content-Type", "application/x-www-form-urlencoded");

        var response = await restClient.ExecuteAsync(request, cancellationToken);

        if (response.IsSuccessful)
            return JsonConvert.DeserializeObject<OAuthAccessTokenExchangeResponse>(response.Content!)!;
        
        var failureResponse = JsonConvert.DeserializeObject<OAuthTwitchAccessTokenExchangeFailureResponse>(response.Content!)!;
            
        throw response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new OAuthAccessTokenExchangeException("Invalid refresh token"),
            HttpStatusCode.Unauthorized => new OAuthAccessTokenExchangeException("Invalid client id or client secret"),
            _ => new OAuthAccessTokenExchangeException("Unknown error: " + Environment.NewLine + JsonConvert.SerializeObject(failureResponse))
        };
    }

    public override async Task<OAuthAccessTokenExchangeResponse> ExchangeAuthCodeForAccessTokenAsync(string authCode, string redirectUrl, CancellationToken cancellationToken = default)
    {
        var requestModel = new OAuthAccessTokenExchangeRequest
        {
            ClientId = _authProviderConfig.AuthProviderClientId,
            ClientSecret = _authProviderConfig.AuthProviderClientSecret,
            Code = authCode,
            RedirectUri = redirectUrl
        };

        using var restClient = BuildRestClient(_authProviderConfig.AccessTokenExchangeEndpoint);

        var request = new RestRequest(requestModel.GetQueryParametersFromJsonProperties(), Method.Post);
        request.AddOrUpdateHeader("Content-Type", "application/x-www-form-urlencoded");

        var response = await restClient.ExecuteAsync(request, cancellationToken);

        if (response.IsSuccessful)
            return JsonConvert.DeserializeObject<OAuthAccessTokenExchangeResponse>(response.Content!)!;
        
        var failureResponse = JsonConvert.DeserializeObject<OAuthTwitchAccessTokenExchangeFailureResponse>(response.Content!)!;
            
        throw response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new OAuthAccessTokenExchangeException("Invalid refresh token"),
            HttpStatusCode.Unauthorized => new OAuthAccessTokenExchangeException("Invalid client id or client secret"),
            _ => new OAuthAccessTokenExchangeException("Unknown error: " + Environment.NewLine + JsonConvert.SerializeObject(failureResponse))
        };
    }

    public override async Task<IUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var userResponse = await _twitchApi.Helix.Users.GetUsersAsync(accessToken: accessToken);
        var userInfo = userResponse.Users.FirstOrDefault();
        
        if (userInfo == null)
            throw new ArgumentException("Invalid Twitch auth token");

        return new TwitchUserInfo
        {
            AuthProviderUserId = userInfo.Id,
            Name = userInfo.DisplayName,
            EmailAddress = userInfo.Email,
            ProfileImage = userInfo.ProfileImageUrl,
            BroadcasterType = userInfo.BroadcasterType
        };
    }

    public async Task<bool> ValidateAccessTokenWithNonceClaimAsync(string accessToken, string state, CancellationToken cancellationToken = default)
    {
        var request = new RestRequest();
        request.AddOrUpdateHeader("Authorization", $"Bearer {accessToken}");
        request.AddOrUpdateHeader("Content-Type", "application/json");

        using var restClient = BuildRestClient(_authProviderConfig.UserInfoEndpoint);
        var response = await restClient.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessful)
            throw new Exception($"[{GetType().Name}] Failed to validate access token.");

        return _socialAuthService.ValidateRequest(state, JsonConvert.DeserializeObject<dynamic>(response.Content!)!.nonce);
    }

    protected override HashSet<Claim> CreateUserClaims(IUserInfo userInfo)
    {
        var twitchUserInfo = (TwitchUserInfo) userInfo;
        var claims = base.CreateUserClaims(userInfo);
        if (!string.IsNullOrWhiteSpace(twitchUserInfo.ProfileImage))
            claims.Add(new Claim("profileImage", twitchUserInfo.ProfileImage));
        claims.Add(new Claim("broadcasterType", twitchUserInfo.BroadcasterType));
        return claims;
    }

    public override string GetAuthorizationUrl(string redirectUrl, bool useExtendedScopes)
    {
        var twitchConfig = (TwitchAuthConfig) _authProviderConfig;
        var authUrl = base.GetAuthorizationUrl(redirectUrl, useExtendedScopes);
        return $"{authUrl}&force_verify={(twitchConfig.ForceVerify ? "true" : "false")}";
    }
}
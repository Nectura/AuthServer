using System.Net;
using AuthServer.Configuration;
using AuthServer.Configuration.Abstract;
using AuthServer.Extensions;
using AuthServer.Models.OAuth;
using AuthServer.Models.UserInfo.Interfaces;
using AuthServer.Services.Abstract.Interfaces;
using AuthServer.Services.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace AuthServer.Services.Abstract;

public abstract class AuthProviderService : JwtAuthService, IAuthProviderService
{
    private readonly HttpClient _httpClient;
    protected readonly AuthProviderConfig _authProviderConfig;
    protected readonly ISocialAuthService _socialAuthService;

    protected AuthProviderService(
        HttpClient httpClient,
        IOptions<AuthProviderConfig> authProviderConfig,
        IOptions<JwtAuthConfig> jwtAuthConfig,
        ISocialAuthService socialAuthService) : base(jwtAuthConfig, authProviderConfig)
    {
        _httpClient = httpClient;
        _socialAuthService = socialAuthService;
        _authProviderConfig = authProviderConfig.Value;
    }

    public virtual async Task<OAuthAccessTokenExchangeResponse> ExchangeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var requestModel = new OAuthAccessTokenRefreshRequest
        {
            RefreshToken = refreshToken
        };

        var request = new RestRequest(requestModel.GetQueryParametersFromJsonProperties(), Method.Post);
        var encodedCredentials = $"{_authProviderConfig.AuthProviderClientId}:{_authProviderConfig.AuthProviderClientSecret}".Base64Encode();
        request.AddOrUpdateHeader("Authorization", $"Basic {encodedCredentials}");
        request.AddOrUpdateHeader("Content-Type", "application/x-www-form-urlencoded");

        using var restClient = BuildRestClient(_authProviderConfig.AccessTokenExchangeEndpoint);
        var response = await restClient.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessful)
            throw new Exception($"[{GetType().Name}] Failed to exchange a refresh token for a new one.");

        return JsonConvert.DeserializeObject<OAuthAccessTokenExchangeResponse>(response.Content!)!;
    }

    public virtual async Task<OAuthAccessTokenExchangeResponse> ExchangeAuthCodeForAccessTokenAsync(string authCode, string redirectUrl, CancellationToken cancellationToken = default)
    {
        var requestModel = new OAuthAccessTokenExchangeRequest
        {
            Code = authCode,
            RedirectUri = redirectUrl
        };

        var request = new RestRequest(requestModel.GetQueryParametersFromJsonProperties(), Method.Post);
        var encodedCredentials = $"{_authProviderConfig.AuthProviderClientId}:{_authProviderConfig.AuthProviderClientSecret}".Base64Encode();
        request.AddOrUpdateHeader("Authorization", $"Basic {encodedCredentials}");
        request.AddOrUpdateHeader("Content-Type", "application/x-www-form-urlencoded");

        using var restClient = BuildRestClient(_authProviderConfig.AccessTokenExchangeEndpoint);
        var response = await restClient.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessful)
            throw new Exception($"[{GetType().Name}] Failed to exchange an authorization code for an access token.");

        return JsonConvert.DeserializeObject<OAuthAccessTokenExchangeResponse>(response.Content!)!;
    }

    public abstract Task<IUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default);

    public virtual string GetAuthorizationUrl(string redirectUrl, bool useExtendedScopes)
    {
        if (!_socialAuthService.TryRegisterRequest(out var state, out var nonce))
            throw new Exception($"[{GetType().Name}] Failed to register an authorization request.");

        var encodedScopes = WebUtility.UrlEncode(string.Join("+", useExtendedScopes ? _authProviderConfig.ExtendedAuthScopes : _authProviderConfig.MinimalAuthScopes));
        
        var requestModel = new OAuthTwitchAuthorizationCodeRequest
        {
            ClientId = _authProviderConfig.AuthProviderClientId,
            RedirectUri = redirectUrl,
            Scope = encodedScopes,
            ResponseType = "code",
            State = state,
            Nonce = nonce
        };

        return _authProviderConfig.UserAuthorizationEndpoint + requestModel.GetQueryParametersFromJsonProperties();
    }

    protected RestClient BuildRestClient(string baseUrl)
    {
        return new RestClient(new RestClientOptions
        {
            BaseUrl = new Uri(baseUrl),
            MaxTimeout = _authProviderConfig.TimeoutInMilliseconds
        }).UseNewtonsoftJson();
    }
}
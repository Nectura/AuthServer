using AuthServer.Configuration;
using AuthServer.Configuration.Abstract;
using AuthServer.Extensions;
using AuthServer.Models.OAuth;
using AuthServer.Services.Abstract.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace AuthServer.Services.Abstract;

public abstract class AuthProviderService : JwtAuthService, IAuthProviderService
{
    private readonly HttpClient _httpClient;
    protected readonly AuthProviderConfig _authProviderConfig;

    protected AuthProviderService(
        HttpClient httpClient,
        IOptions<AuthProviderConfig> authProviderConfig,
        IOptions<JwtAuthConfig> jwtAuthConfig) : base(jwtAuthConfig, authProviderConfig)
    {
        _httpClient = httpClient;
        _authProviderConfig = authProviderConfig.Value;
    }

    public async Task<OAuthAccessTokenExchangeResponse> ExchangeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var requestModel = new OAuthAccessTokenRefreshRequest
        {
            RefreshToken = refreshToken
        };

        using var restClient = BuildRestClient(_authProviderConfig.AccessTokenExchangeEndpoint);

        var request = new RestRequest(requestModel.GetQueryParametersFromJsonProperties(), Method.Post);
        var encodedCredentials = $"{_authProviderConfig.AuthProviderClientId}:{_authProviderConfig.AuthProviderClientSecret}".Base64Encode();
        request.AddOrUpdateHeader("Authorization", $"Basic {encodedCredentials}");
        request.AddOrUpdateHeader("Content-Type", "application/x-www-form-urlencoded");

        var response = await restClient.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessful)
            throw new Exception($"[{GetType().Name}] Failed to exchange a refresh token for a new one.");

        return JsonConvert.DeserializeObject<OAuthAccessTokenExchangeResponse>(response.Content!)!;
    }

    public async Task<OAuthAccessTokenExchangeResponse> ExchangeAuthCodeForAccessTokenAsync(string authCode, string redirectUrl, CancellationToken cancellationToken = default)
    {
        var requestModel = new OAuthAccessTokenExchangeRequest
        {
            Code = authCode,
            RedirectUri = redirectUrl
        };

        using var restClient = BuildRestClient(_authProviderConfig.AccessTokenExchangeEndpoint);

        var request = new RestRequest(requestModel.GetQueryParametersFromJsonProperties(), Method.Post);
        var encodedCredentials = $"{_authProviderConfig.AuthProviderClientId}:{_authProviderConfig.AuthProviderClientSecret}".Base64Encode();
        request.AddOrUpdateHeader("Authorization", $"Basic {encodedCredentials}");
        request.AddOrUpdateHeader("Content-Type", "application/x-www-form-urlencoded");

        var response = await restClient.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessful)
            throw new Exception($"[{GetType().Name}] Failed to exchange an authorization code for an access token.");

        return JsonConvert.DeserializeObject<OAuthAccessTokenExchangeResponse>(response.Content!)!;
    }

    protected RestClient BuildRestClient(string baseUrl)
    {
        return new RestClient(_httpClient, new RestClientOptions
        {
            BaseUrl = new Uri(baseUrl),
            MaxTimeout = _authProviderConfig.TimeoutInMilliseconds
        }).UseNewtonsoftJson();
    }
}

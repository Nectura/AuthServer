﻿using AuthServer.Configuration;
using AuthServer.Services.Abstract;
using AuthServer.Services.Auth.SocialAuth.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;

namespace AuthServer.Services.Auth.SocialAuth;

public sealed class SpotifyAuthService : AuthProviderService, ISpotifyAuthService
{
    public SpotifyAuthService(
        HttpClient httpClient,
        IOptions<SpotifyAuthConfig> authProviderConfig,
        IOptions<JwtAuthConfig> authConfig) : base(httpClient, authProviderConfig, authConfig)
    {
    }

    public async Task<string> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var restClient = BuildRestClient(((SpotifyAuthConfig)_authProviderConfig).UserMeEndpoint);

        var request = new RestRequest(string.Empty);
        request.AddHeader("Authorization", $"Bearer {accessToken}");

        var response = await restClient.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessful)
            throw new Exception("[Spotify] Failed to exchange the access token for the user's email address.");

        return JsonConvert.DeserializeObject<dynamic>(response.Content!)!.id;
    }
}
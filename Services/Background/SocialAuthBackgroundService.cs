using System.Collections.Concurrent;
using AuthServer.Database.Enums;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories.Interfaces;
using AuthServer.Models.OAuth;
using AuthServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AuthServer.Services.Background;

public sealed class SocialAuthBackgroundService : BackgroundService
{
    private readonly PeriodicTimer _timer;
    
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISocialAuthService _socialAuthService;
    private readonly ILogger<SocialAuthBackgroundService> _logger;
    private readonly Dictionary<EAuthProvider, Func<string, CancellationToken, Task<OAuthAccessTokenExchangeResponse>>> _refreshTokenExchangeDictionary;

    public SocialAuthBackgroundService(
        IServiceScopeFactory scopeFactory,
        IGoogleAuthService googleAuthService,
        ISpotifyAuthService spotifyAuthService,
        ITwitchAuthService twitchAuthService,
        IDiscordAuthService discordAuthService,
        ISocialAuthService socialAuthService,
        ILogger<SocialAuthBackgroundService> logger)
    {
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        
        _scopeFactory = scopeFactory;
        _socialAuthService = socialAuthService;
        _logger = logger;
        _refreshTokenExchangeDictionary = new Dictionary<EAuthProvider, Func<string, CancellationToken, Task<OAuthAccessTokenExchangeResponse>>>
        {
            { EAuthProvider.Google, googleAuthService.ExchangeRefreshTokenAsync },
            { EAuthProvider.Spotify, spotifyAuthService.ExchangeRefreshTokenAsync },
            { EAuthProvider.Twitch, twitchAuthService.ExchangeRefreshTokenAsync },
            { EAuthProvider.Discord, discordAuthService.ExchangeRefreshTokenAsync }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && await _timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshSocialAuthProviderTokensAsync(stoppingToken);
            await ClearExpiredLocalUserRefreshTokensAsync(stoppingToken);
            await ClearExpiredSocialUserRefreshTokensAsync(stoppingToken);
            _socialAuthService.ClearExpiredRequests();
        }
        
        _timer.Dispose();
    }
    
    private async Task RefreshSocialAuthProviderTokensAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var authProviderTokenRepository = scope.ServiceProvider.GetRequiredService<ISocialUserAuthProviderTokenRepository>();

        var expiredAccessTokens = await authProviderTokenRepository
            .Query(m => DateTime.UtcNow >= m.ExpiresAt)
            .ToListAsync(cancellationToken: cancellationToken);

        var deadTokens = new ConcurrentBag<SocialUserAuthProviderToken>();
        
        var taskList = expiredAccessTokens.Select(m => RefreshAccessTokenAsync(m, deadTokens, cancellationToken)).ToList();

        await Task.WhenAll(taskList);
        
        // Note: Probably don't want to remove them no matter what
        // authProviderTokenRepository.RemoveRange(deadTokens);

        await authProviderTokenRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task RefreshAccessTokenAsync(SocialUserAuthProviderToken refreshToken, ConcurrentBag<SocialUserAuthProviderToken> deadTokens, CancellationToken cancellationToken = default)
    {
        var authProviderName = Enum.GetName(typeof(EAuthProvider), refreshToken.AuthProvider);
        
        if (!_refreshTokenExchangeDictionary.ContainsKey(refreshToken.AuthProvider))
        {
            _logger.LogWarning("Failed to refresh access token for {authProviderName} because that auth provider isn't handled.", authProviderName);
            return;
        }

        var tokenRefreshFunc = _refreshTokenExchangeDictionary[refreshToken.AuthProvider];
        
        OAuthAccessTokenExchangeResponse tokenExchangeResponse;
        try
        {
            tokenExchangeResponse = await tokenRefreshFunc.Invoke(refreshToken.RefreshToken, cancellationToken);
        }
        catch (Exception)
        {
            deadTokens.Add(refreshToken);
            return;
        }

        refreshToken.RefreshToken = tokenExchangeResponse.RefreshToken;
        refreshToken.AccessToken = tokenExchangeResponse.AccessToken;
        refreshToken.Scopes = JsonConvert.SerializeObject(tokenExchangeResponse.Scope);
        refreshToken.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenExchangeResponse.ExpiresInSeconds);
    }

    private async Task ClearExpiredLocalUserRefreshTokensAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var tokenRepository = scope.ServiceProvider.GetRequiredService<ILocalUserRefreshTokenRepository>();

        var expiredAccessTokens = await tokenRepository
            .Query(m => DateTime.UtcNow >= m.AbsoluteExpirationTime)
            .ToListAsync(cancellationToken: cancellationToken);

        tokenRepository.RemoveRange(expiredAccessTokens);

        await tokenRepository.SaveChangesAsync(cancellationToken);
    }
    
    private async Task ClearExpiredSocialUserRefreshTokensAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var tokenRepository = scope.ServiceProvider.GetRequiredService<ISocialUserRefreshTokenRepository>();

        var expiredAccessTokens = await tokenRepository
            .Query(m => DateTime.UtcNow >= m.AbsoluteExpirationTime)
            .ToListAsync(cancellationToken: cancellationToken);

        tokenRepository.RemoveRange(expiredAccessTokens);

        await tokenRepository.SaveChangesAsync(cancellationToken);
    }
}
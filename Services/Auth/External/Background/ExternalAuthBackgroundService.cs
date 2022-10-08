using AuthServer.Database.Enums;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.Models.OAuth;
using AuthServer.Utilities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AuthServer.Services.Auth.External.Background;

public sealed class ExternalAuthBackgroundService : IHostedService
{
    private readonly TimerAsync _timer;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<EAuthProvider, Func<string, CancellationToken, Task<OAuthAccessTokenExchangeResponse>>> _refreshTokenExchangeDictionary;

    public ExternalAuthBackgroundService(
        IServiceScopeFactory scopeFactory,
        GoogleAuthService googleAuthService,
        SpotifyAuthService spotifyAuthService)
    {
        _timer = new TimerAsync(TickAsync, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        _scopeFactory = scopeFactory;
        _refreshTokenExchangeDictionary = new Dictionary<EAuthProvider, Func<string, CancellationToken, Task<OAuthAccessTokenExchangeResponse>>>
        {
            { EAuthProvider.Google, googleAuthService.ExchangeRefreshTokenAsync },
            { EAuthProvider.Spotify, spotifyAuthService.ExchangeRefreshTokenAsync },
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Start();

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _timer.Stop();
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<ExternalUserRefreshTokenRepository>();

        var expiredAccessTokens = await refreshTokenRepository
            .Query(m => DateTime.UtcNow >= m.ExpiresAt)
            .ToListAsync(cancellationToken: cancellationToken);

        var taskList = expiredAccessTokens.Select(m => RefreshAccessTokenAsync(m, cancellationToken)).ToList();

        await Task.WhenAll(taskList);

        await refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task RefreshAccessTokenAsync(ExternalUserRefreshToken externalUserRefreshToken, CancellationToken cancellationToken = default)
    {
        if (!_refreshTokenExchangeDictionary.ContainsKey(externalUserRefreshToken.AuthProvider))
        {
            Log.Warning($"Failed to refresh access token for {Enum.GetName(typeof(EAuthProvider), externalUserRefreshToken.AuthProvider)} because that auth provider isn't handled.");
            return;
        }

        var tokenRefreshFunc = _refreshTokenExchangeDictionary[externalUserRefreshToken.AuthProvider];
        OAuthAccessTokenExchangeResponse tokenExchangeResponse;

        try
        {
            tokenExchangeResponse = await tokenRefreshFunc.Invoke(externalUserRefreshToken.RefreshToken, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to refresh access token for {Enum.GetName(typeof(EAuthProvider), externalUserRefreshToken.AuthProvider)}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            return;
        }

        //externalUserRefreshToken.PreviousRefreshToken = externalUserRefreshToken.RefreshToken; // Note: limits the user to only one location
        externalUserRefreshToken.RefreshToken = tokenExchangeResponse.RefreshToken;
        externalUserRefreshToken.AccessToken = tokenExchangeResponse.AccessToken;
        externalUserRefreshToken.Scopes = tokenExchangeResponse.Scope;
        externalUserRefreshToken.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenExchangeResponse.ExpiresInSeconds);
    }
}

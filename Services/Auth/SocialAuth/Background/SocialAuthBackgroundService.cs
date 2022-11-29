using AuthServer.Database.Enums;
using AuthServer.Database.Models.Interfaces;
using AuthServer.Database.Repositories.Interfaces;
using AuthServer.Models.OAuth;
using AuthServer.Services.Auth.SocialAuth.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AuthServer.Services.Auth.SocialAuth.Background;

public sealed class SocialAuthBackgroundService : IHostedService
{
    private readonly PeriodicTimer _timer;
    private CancellationTokenSource? _cancellationTokenSource;
    
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<EAuthProvider, Func<string, CancellationToken, Task<OAuthAccessTokenExchangeResponse>>> _refreshTokenExchangeDictionary;

    public SocialAuthBackgroundService(
        IServiceScopeFactory scopeFactory,
        IGoogleAuthService googleAuthService,
        ISpotifyAuthService spotifyAuthService)
    {
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        
        _scopeFactory = scopeFactory;
        _refreshTokenExchangeDictionary = new Dictionary<EAuthProvider, Func<string, CancellationToken, Task<OAuthAccessTokenExchangeResponse>>>
        {
            { EAuthProvider.Google, googleAuthService.ExchangeRefreshTokenAsync },
            { EAuthProvider.Spotify, spotifyAuthService.ExchangeRefreshTokenAsync },
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        Task.Run(async () =>
        {
            while (await _timer.WaitForNextTickAsync(_cancellationTokenSource.Token))
                await TickAsync(cancellationToken);
        }, cancellationToken);
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource?.Cancel();
        _timer.Dispose();
        return Task.CompletedTask;
    }

    private async Task TickAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<ISocialUserRefreshTokenRepository>();

        var expiredAccessTokens = await refreshTokenRepository
            .Query(m => DateTime.UtcNow >= m.ExpiresAt)
            .ToListAsync(cancellationToken: cancellationToken);

        var taskList = expiredAccessTokens.Select(m => RefreshAccessTokenAsync(m, cancellationToken)).ToList();

        await Task.WhenAll(taskList);

        await refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task RefreshAccessTokenAsync(IExternalUserRefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        if (!_refreshTokenExchangeDictionary.ContainsKey(refreshToken.AuthProvider))
        {
            Log.Warning($"Failed to refresh access token for {Enum.GetName(typeof(EAuthProvider), refreshToken.AuthProvider)} because that auth provider isn't handled.");
            return;
        }

        var tokenRefreshFunc = _refreshTokenExchangeDictionary[refreshToken.AuthProvider];
        OAuthAccessTokenExchangeResponse tokenExchangeResponse;

        try
        {
            tokenExchangeResponse = await tokenRefreshFunc.Invoke(refreshToken.RefreshToken, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to refresh access token for {Enum.GetName(typeof(EAuthProvider), refreshToken.AuthProvider)}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            return;
        }

        //externalUserRefreshToken.PreviousRefreshToken = externalUserRefreshToken.RefreshToken; // Note: limits the user to only one location
        refreshToken.RefreshToken = tokenExchangeResponse.RefreshToken;
        refreshToken.AccessToken = tokenExchangeResponse.AccessToken;
        refreshToken.Scopes = tokenExchangeResponse.Scope;
        refreshToken.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenExchangeResponse.ExpiresInSeconds);
    }
}

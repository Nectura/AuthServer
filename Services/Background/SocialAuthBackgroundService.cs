using AuthServer.Database.Enums;
using AuthServer.Database.Models.Interfaces;
using AuthServer.Database.Repositories.Interfaces;
using AuthServer.Models.OAuth;
using AuthServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Background;

public sealed class SocialAuthBackgroundService : BackgroundService
{
    private readonly PeriodicTimer _timer;
    
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SocialAuthBackgroundService> _logger;
    private readonly Dictionary<EAuthProvider, Func<string, CancellationToken, Task<OAuthAccessTokenExchangeResponse>>> _refreshTokenExchangeDictionary;

    public SocialAuthBackgroundService(
        IServiceScopeFactory scopeFactory,
        IGoogleAuthService googleAuthService,
        ISpotifyAuthService spotifyAuthService,
        ILogger<SocialAuthBackgroundService> logger)
    {
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        
        _scopeFactory = scopeFactory;
        _logger = logger;
        _refreshTokenExchangeDictionary = new Dictionary<EAuthProvider, Func<string, CancellationToken, Task<OAuthAccessTokenExchangeResponse>>>
        {
            { EAuthProvider.Google, googleAuthService.ExchangeRefreshTokenAsync },
            { EAuthProvider.Spotify, spotifyAuthService.ExchangeRefreshTokenAsync },
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && await _timer.WaitForNextTickAsync(stoppingToken))
            await TickAsync(stoppingToken);
        
        _timer.Dispose();
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
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to refresh access token for {errMsg}{newLine}{stackTrace}:", ex.Message, Environment.NewLine, ex.StackTrace);
            return;
        }

        //externalUserRefreshToken.PreviousRefreshToken = externalUserRefreshToken.RefreshToken; // Note: limits the user to only one location
        refreshToken.RefreshToken = tokenExchangeResponse.RefreshToken;
        refreshToken.AccessToken = tokenExchangeResponse.AccessToken;
        refreshToken.Scopes = tokenExchangeResponse.Scope;
        refreshToken.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenExchangeResponse.ExpiresInSeconds);
    }
}

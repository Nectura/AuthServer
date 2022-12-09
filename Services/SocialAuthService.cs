using System.Collections.Concurrent;
using AuthServer.Configuration;
using AuthServer.Models.Structs;
using AuthServer.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AuthServer.Services;

public sealed class SocialAuthService : ISocialAuthService
{
    private readonly IOptions<SocialAuthConfig> _authConfig;
    private readonly ConcurrentDictionary<string, SocialAuthRequest> _stateDictionary = new();

    public SocialAuthService(IOptions<SocialAuthConfig> authConfig)
    {
        _authConfig = authConfig;
    }
    
    public bool TryRegisterRequest(out string state, out string nonce)
    {
        state = Guid.NewGuid().ToString();
        nonce = Guid.NewGuid().ToString();
        return _stateDictionary.TryAdd(state, new SocialAuthRequest
        {
            Nonce = nonce,
            ExpiresAt = DateTime.UtcNow.AddSeconds(_authConfig.Value.SocialAuthRequestTimeoutInSeconds)
        });
    }

    public bool ValidateRequest(string state, string? nonce = default)
    {
        var validState = _stateDictionary.TryRemove(state, out var request);
        if (request.HasExpired) return false;
        return validState && nonce == default || validState && nonce == request.Nonce;
    }
    
    public void ClearExpiredRequests()
    {
        var expiredRequests = _stateDictionary.Where(x => x.Value.HasExpired).Select(x => x.Key).ToList();
        foreach (var expiredRequest in expiredRequests)
            _stateDictionary.TryRemove(expiredRequest, out _);
    }
}
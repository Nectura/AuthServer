using AuthServer.Services.Abstract.Interfaces;

namespace AuthServer.Services.Interfaces;

public interface ITwitchAuthService : IAuthProviderService
{    
    Task<bool> ValidateAccessTokenWithNonceClaimAsync(string accessToken, string state, CancellationToken cancellationToken = default);
}
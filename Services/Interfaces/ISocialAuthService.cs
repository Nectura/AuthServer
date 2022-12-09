namespace AuthServer.Services.Interfaces;

public interface ISocialAuthService
{
    bool TryRegisterRequest(out string state, out string nonce);
    bool ValidateRequest(string state, string? nonce = default);
    void ClearExpiredRequests();
}
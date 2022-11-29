namespace AuthServer.Services.Cryptography.Interfaces;

public interface IAuthService
{
    Task<(byte[] SaltHash, byte[] FinalizedOutput)> HashInputAsync(string input, CancellationToken cancellationToken = default);
    Task<bool> CompareHashesAsync(string input, byte[] saltHash, byte[] expectedOutput, CancellationToken cancellationToken = default);
}
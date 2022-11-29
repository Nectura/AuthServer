using System.Security.Cryptography;
using System.Text;
using AuthServer.Services.Cryptography.Interfaces;

namespace AuthServer.Services.Cryptography;

/*
 * Hashing Algorithm: SHA3
 * Hashing Method: hash(hash(pw)+salt)
 */
public sealed class Sha3AuthService : IAuthService
{
    private const int CryptoSafeStringGenerationLength = 32;

    public async Task<(byte[] SaltHash, byte[] FinalizedOutput)> HashInputAsync(string input, CancellationToken cancellationToken = default)
    {
        var cryptoSafeString = GenerateRandomCryptoSafeString();
        var saltHash = await ComputeHashAsync(cryptoSafeString, cancellationToken);
        var passwordHash = await ComputeHashAsync(input, cancellationToken);
        var finalizedOutput = new byte[saltHash.Length + passwordHash.Length];
        Array.Copy(passwordHash, 0, finalizedOutput, 0, passwordHash.Length);
        Array.Copy(saltHash, 0, finalizedOutput, passwordHash.Length, saltHash.Length);
        finalizedOutput = await ComputeHashAsync(finalizedOutput, cancellationToken);
        return (saltHash, finalizedOutput);
    }

    public async Task<bool> CompareHashesAsync(string input, byte[] saltHash, byte[] expectedOutput, CancellationToken cancellationToken = default)
    {
        // compute the hash of the input string
        var passwordHash = await ComputeHashAsync(input, cancellationToken);

        // define the finalized output byte array
        var finalizedOutput = new byte[saltHash.Length + passwordHash.Length];

        // append the password hash to the finalized output and then append the salt hash afterwards
        Array.Copy(passwordHash, 0, finalizedOutput, 0, passwordHash.Length);
        Array.Copy(saltHash, 0, finalizedOutput, passwordHash.Length, saltHash.Length);

        // compute the hash of the finalized inner hash
        finalizedOutput = await ComputeHashAsync(finalizedOutput, cancellationToken);

        return finalizedOutput.SequenceEqual(expectedOutput);
    }

    private static async Task<byte[]> ComputeHashAsync(string input, CancellationToken cancellationToken = default)
    {
        return await ComputeHashAsync(Encoding.ASCII.GetBytes(input), cancellationToken);
    }

    private static async Task<byte[]> ComputeHashAsync(byte[] input, CancellationToken cancellationToken = default)
    {
        using var hashProvider = SHA384.Create();
        var inputStream = new MemoryStream(input);
        return await hashProvider.ComputeHashAsync(inputStream, cancellationToken);
    }

    private static string GenerateRandomCryptoSafeString()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(CryptoSafeStringGenerationLength));
    }
}

using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.Services.Auth;
using AuthServer.Services.Auth.Local;
using AuthServer.Validators;
using Grpc.Core;

namespace AuthServer.Services.Rpc;

public sealed class LocalAuthRpcService : AuthServer.LocalAuthRpcService.LocalAuthRpcServiceBase
{
    private readonly LocalAuthService _localAuthService;
    private readonly CryptoAuthService _cryptoAuthService;
    private readonly LocalUserRepository _userRepository;

    public LocalAuthRpcService(
        LocalAuthService localAuthService,
        CryptoAuthService cryptoAuthService,
        LocalUserRepository userRepository)
    {
        _localAuthService = localAuthService;
        _cryptoAuthService = cryptoAuthService;
        _userRepository = userRepository;
    }

    public override async Task<LocalLoginResponse> Login(LocalLoginRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.EmailAddress) || string.IsNullOrWhiteSpace(request.Password))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Empty username or password"));

        var userInfoValidator = new UserInfoValidator();

        if (!userInfoValidator.TryValidateEmailAddress(request.EmailAddress, out _))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Invalid email address"));

        var user = await _userRepository.FindAsync(request.EmailAddress, context.CancellationToken);

        if (user == default)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid username or password"));

        var validCredentials = await _cryptoAuthService.CompareHashesAsync(request.Password, user.SaltHash, user.PasswordHash, context.CancellationToken);

        if (!validCredentials)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid username or password"));

        var jwtCredential = _localAuthService.CreateJwtCredential(user);

        return new LocalLoginResponse
        {
            AccessToken = jwtCredential.AccessToken,
            RefreshToken = jwtCredential.RefreshToken
        };
    }

    public override async Task<LocalRegistrationResponse> Register(LocalRegistrationRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.EmailAddress) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Name))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "One or more fields are empty"));

        var userInfoValidator = new UserInfoValidator();
        var passwordValidator = new PasswordStructureValidator();

        if (!userInfoValidator.TryValidateEmailAddress(request.EmailAddress, out _))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Invalid email address"));

        if (!passwordValidator.Validate(request.Password, out var validationError))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Invalid password structure: {validationError}"));

        var userExists = await _userRepository.AnyAsync(m => m.EmailAddress == request.EmailAddress, context.CancellationToken);

        if (userExists)
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Email address already in use"));

        var (saltHash, finalizedOutput) = await _cryptoAuthService.HashInputAsync(request.Password, context.CancellationToken);

        _userRepository.Add(new LocalUser
        {
            Name = request.Name,
            EmailAddress = request.EmailAddress,
            PasswordHash = finalizedOutput,
            SaltHash = saltHash
        });

        await _userRepository.SaveChangesAsync(context.CancellationToken);

        return new LocalRegistrationResponse();
    }
}
using AuthServer.Database.Models;
using AuthServer.Database.Repositories.Interfaces;
using AuthServer.Services.Auth.LocalAuth.Interfaces;
using AuthServer.Services.Cryptography.Interfaces;
using AuthServer.Validators;
using AuthServer.Validators.Exceptions;
using AuthServer.Validators.Requests;
using Grpc.Core;

namespace AuthServer.Services.Rpc;

public sealed class LocalAuthRpcService : LocalAuthGrpcService.LocalAuthGrpcServiceBase
{
    private readonly ILocalAuthService _localAuthService;
    private readonly IAuthService _authService;
    private readonly ILocalUserRepository _userRepository;

    public LocalAuthRpcService(
        ILocalAuthService localAuthService,
        IAuthService authService,
        ILocalUserRepository userRepository)
    {
        _localAuthService = localAuthService;
        _authService = authService;
        _userRepository = userRepository;
    }

    public override async Task<LocalLoginResponse> Login(LocalLoginRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.EmailAddress) || string.IsNullOrWhiteSpace(request.Password))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Empty username or password"));

        try
        {
            new UserInfoValidator().TryValidate(new UserInfoValidationRequest
            {
                Input = request.EmailAddress
            });
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }

        var user = await _userRepository.FindAsync(request.EmailAddress, context.CancellationToken);

        if (user == default)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid username or password"));

        var validCredentials = await _authService.CompareHashesAsync(request.Password, user.SaltHash, user.PasswordHash, context.CancellationToken);

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

        try
        {
            new UserInfoValidator().TryValidate(new UserInfoValidationRequest
            {
                Input = request.EmailAddress
            });
            new PasswordStructureValidator().TryValidate(new PasswordStructureValidationRequest(request.Password));
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        
        var userExists = await _userRepository.AnyAsync(m => m.EmailAddress == request.EmailAddress, context.CancellationToken);

        if (userExists)
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Email address already in use"));

        var (saltHash, finalizedOutput) = await _authService.HashInputAsync(request.Password, context.CancellationToken);

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
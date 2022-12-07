using AuthServer.Database.Models;
using AuthServer.Database.Repositories.Interfaces;
using AuthServer.Services.Cryptography.Interfaces;
using AuthServer.Services.Interfaces;
using AuthServer.Validators.Exceptions;
using AuthServer.Validators.Interfaces;
using AuthServer.Validators.Requests;
using Grpc.Core;

namespace AuthServer.Services.Rpc;

public sealed class LocalAuthRpcService : LocalAuthGrpcService.LocalAuthGrpcServiceBase
{
    private readonly ILocalAuthService _localAuthService;
    private readonly IAuthService _authService;
    private readonly ILocalUserRepository _userRepository;
    private readonly IUserInfoValidator _userInfoValidator;
    private readonly IPasswordStructureValidator _passwordStructureValidator;

    public LocalAuthRpcService(
        ILocalAuthService localAuthService,
        IAuthService authService,
        ILocalUserRepository userRepository,
        IUserInfoValidator userInfoValidator,
        IPasswordStructureValidator passwordStructureValidator)
    {
        _localAuthService = localAuthService;
        _authService = authService;
        _userRepository = userRepository;
        _userInfoValidator = userInfoValidator;
        _passwordStructureValidator = passwordStructureValidator;
    }

    public override async Task<LocalLoginResponse> Login(LocalLoginRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.EmailAddress) || string.IsNullOrWhiteSpace(request.Password))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Empty username or password"));

        try
        {
            _userInfoValidator.TryValidate(new UserInfoValidationRequest
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
            _userInfoValidator.TryValidate(new UserInfoValidationRequest
            {
                Input = request.EmailAddress
            });
            _passwordStructureValidator.TryValidate(new PasswordStructureValidationRequest(request.Password));
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        
        if (await _userRepository.AnyAsync(m => m.EmailAddress == request.EmailAddress, context.CancellationToken))
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
using AuthServer.Configuration;
using AuthServer.Database.Enums;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories.Interfaces;
using AuthServer.Models.UserInfo;
using AuthServer.Services.Cryptography.Interfaces;
using AuthServer.Services.Interfaces;
using AuthServer.Validators.Exceptions;
using AuthServer.Validators.Interfaces;
using AuthServer.Validators.Requests;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.Rpc;

public sealed class LocalAuthRpcService : LocalAuthGrpcService.LocalAuthGrpcServiceBase
{
    private readonly ILocalAuthService _localAuthService;
    private readonly IAuthService _authService;
    private readonly ILocalUserRepository _userRepository;
    private readonly IUserInfoValidator _userInfoValidator;
    private readonly ILocalUserRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordStructureValidator _passwordStructureValidator;
    private readonly IOptions<LocalAuthConfig> _authConfig;

    public LocalAuthRpcService(
        ILocalAuthService localAuthService,
        IAuthService authService,
        ILocalUserRepository userRepository,
        IUserInfoValidator userInfoValidator,
        IPasswordStructureValidator passwordStructureValidator,
        ILocalUserRefreshTokenRepository refreshTokenRepository,
        IOptions<LocalAuthConfig> authConfig)
    {
        _localAuthService = localAuthService;
        _authService = authService;
        _userRepository = userRepository;
        _userInfoValidator = userInfoValidator;
        _passwordStructureValidator = passwordStructureValidator;
        _refreshTokenRepository = refreshTokenRepository;
        _authConfig = authConfig;
    }
    
    public override async Task<LocalLoginResponse> RefreshAccessToken(LocalUserRefreshAccessTokenRequest request, ServerCallContext context)
    {
        var userToken = await _refreshTokenRepository
            .Query(m => m.RefreshToken == request.RefreshToken || m.PreviousRefreshToken == request.RefreshToken)
            .Include(m => m.User)
            .FirstOrDefaultAsync(context.CancellationToken);
        
        if (userToken == default)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid refresh token"));

        if (userToken.HasExpired)
        {
            _refreshTokenRepository.Remove(userToken);
            await _refreshTokenRepository.SaveChangesAsync(context.CancellationToken);
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Expired refresh token"));
        }
        
        if (userToken.PreviousRefreshToken == request.RefreshToken)
        {
            _refreshTokenRepository.Remove(userToken);
            await _refreshTokenRepository.SaveChangesAsync(context.CancellationToken);
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Security breach detected"));
        }
        
        var jwtCredential = _localAuthService.CreateJwtCredential(new LocalUserInfo
        {
            Id = userToken.UserId,
            Name = userToken.User!.Name,
            EmailAddress = userToken.User.EmailAddress,
            ProfileImage = userToken.User.ProfilePicture
        });
        
        userToken.PreviousRefreshToken = userToken.RefreshToken;
        userToken.RefreshToken = jwtCredential.RefreshToken;

        await _refreshTokenRepository.SaveChangesAsync(context.CancellationToken);

        return new LocalLoginResponse
        {
            AccessToken = jwtCredential.AccessToken,
            RefreshToken = jwtCredential.RefreshToken
        };
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

        var user = await _userRepository.FindAsync(context.CancellationToken, request.EmailAddress);

        if (user == default)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid username or password"));

        var validCredentials = await _authService.CompareHashesAsync(request.Password, user.SaltHash, user.PasswordHash, context.CancellationToken);

        if (!validCredentials)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid username or password"));

        var jwtCredential = _localAuthService.CreateJwtCredential(new LocalUserInfo
        {
            Id = user.Id,
            Name = user.Name,
            EmailAddress = user.EmailAddress,
            ProfileImage = user.ProfilePicture
        });
        
        await SaveRefreshTokenAsync(user.Id, jwtCredential.RefreshToken, cancellationToken: context.CancellationToken);
        
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
    
    private async Task SaveRefreshTokenAsync(Guid userId, string refreshToken, EUserAuthScope scopes = EUserAuthScope.Identity, CancellationToken cancellationToken = default)
    {
        _refreshTokenRepository.Add(new LocalUserRefreshToken
        {
            UserId = userId,
            RefreshToken = refreshToken,
            Scopes = scopes,
            AbsoluteExpirationTime = DateTime.UtcNow.Add(JwtAuthConfig.JwtMaxLifeSpan)
        });
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
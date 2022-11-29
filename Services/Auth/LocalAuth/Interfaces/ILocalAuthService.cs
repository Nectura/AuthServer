using AuthServer.Database.Models.Abstract;
using AuthServer.Models.Jwt;

namespace AuthServer.Services.Auth.LocalAuth.Interfaces;

public interface ILocalAuthService
{
    JwtCredential CreateJwtCredential(User user);
}
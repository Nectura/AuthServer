using AuthServer.Database.Models.Abstract;
using AuthServer.Models.Jwt;

namespace AuthServer.Services.Interfaces;

public interface ILocalAuthService
{
    JwtCredential CreateJwtCredential(User user);
}
using AuthServer.Database.Models.Abstract;
using AuthServer.Models.Jwt;

namespace AuthServer.Services.Abstract.Interfaces;

public interface IJwtAuthService
{
    public JwtCredential CreateJwtCredential(User user);
}
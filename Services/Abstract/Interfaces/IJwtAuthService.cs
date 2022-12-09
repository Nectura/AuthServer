using AuthServer.Models.Jwt;
using AuthServer.Models.UserInfo.Interfaces;

namespace AuthServer.Services.Abstract.Interfaces;

public interface IJwtAuthService
{
    public JwtCredential CreateJwtCredential(IUserInfo userInfo);
}
using AuthServer.Models.Jwt;
using AuthServer.Models.UserInfo.Interfaces;

namespace AuthServer.Services.Interfaces;

public interface ILocalAuthService
{
    JwtCredential CreateJwtCredential(IUserInfo userInfo);
}
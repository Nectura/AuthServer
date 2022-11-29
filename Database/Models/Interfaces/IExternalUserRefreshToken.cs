using AuthServer.Database.Enums;
using AuthServer.Database.Models.Abstract.Interfaces;

namespace AuthServer.Database.Models.Interfaces;

public interface IExternalUserRefreshToken : IUserRefreshToken
{
    SocialUser? User { get; set; }
    EAuthProvider AuthProvider { get; set; }
    string AccessToken { get; set; }
    string Scopes { get; set; }
}
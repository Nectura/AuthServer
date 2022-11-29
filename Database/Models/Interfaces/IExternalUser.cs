using AuthServer.Database.Enums;
using AuthServer.Database.Models.Abstract.Interfaces;

namespace AuthServer.Database.Models.Interfaces;

public interface IExternalUser : IUser
{
    EAuthProvider AuthProvider { get; set; }
    ICollection<SocialUserRefreshToken>? RefreshTokens { get; set; }
}
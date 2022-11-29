using AuthServer.Database.Enums;
using AuthServer.Database.Models.Abstract;
using AuthServer.Database.Models.Interfaces;

namespace AuthServer.Database.Models;

public sealed class SocialUser : User, IExternalUser
{
    public EAuthProvider AuthProvider { get; set; }

    public ICollection<SocialUserRefreshToken>? RefreshTokens { get; set; }
}
using AuthServer.Database.Enums;
using AuthServer.Database.Models.Abstract;

namespace AuthServer.Database.Models;

public sealed class ExternalUser : User
{
    public EAuthProvider AuthProvider { get; set; }

    public ICollection<ExternalUserRefreshToken>? RefreshTokens { get; set; }
}
using AuthServer.Database.Enums;
using AuthServer.Database.Models.Abstract;

namespace AuthServer.Database.Models;

public sealed class ExternalUserRefreshToken : UserRefreshToken
{
    public ExternalUser? User { get; set; }
    public EAuthProvider AuthProvider { get; set; }
    public string AccessToken { get; set; } = "";
    public string Scopes { get; set; } = "";
}
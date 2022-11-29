using AuthServer.Database.Enums;
using AuthServer.Database.Models.Abstract;
using AuthServer.Database.Models.Interfaces;

namespace AuthServer.Database.Models;

public sealed class SocialUserRefreshToken : UserRefreshToken, IExternalUserRefreshToken
{
    public SocialUser? User { get; set; }
    public EAuthProvider AuthProvider { get; set; }
    public string AccessToken { get; set; } = "";
    public string Scopes { get; set; } = "";
}
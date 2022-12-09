using AuthServer.Database.Models.Interfaces;

namespace AuthServer.Database.Models;

public sealed class SocialUserRefreshToken : UserRefreshToken, ISocialUserRefreshToken
{
    public SocialUser? User { get; set; }
}
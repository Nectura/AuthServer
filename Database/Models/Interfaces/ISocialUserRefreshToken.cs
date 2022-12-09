namespace AuthServer.Database.Models.Interfaces;

public interface ISocialUserRefreshToken : IServiceAppUserRefreshToken
{
    SocialUser? User { get; set; }
}
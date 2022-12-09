using AuthServer.Models.UserInfo.Interfaces;

namespace AuthServer.Models.UserInfo.Abstract;

public abstract class UserInfo : IUserInfo
{
    public Guid Id { get; set; }
    public string? AuthProviderUserId { get; set; }
    public string? ProfileImage { get; set; }
    public string Name { get; set; } = "";
    public string EmailAddress { get; set; } = "";
}
namespace AuthServer.Models.UserInfo.Interfaces;

public interface IUserInfo
{
    public Guid Id { get; set; }
    public string? AuthProviderUserId { get; set; }
    string Name { get; set; }
    string EmailAddress { get; set; }
}
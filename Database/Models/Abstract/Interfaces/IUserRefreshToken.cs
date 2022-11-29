namespace AuthServer.Database.Models.Abstract.Interfaces;

public interface IUserRefreshToken
{
    Guid Id { get; set; }
    Guid UserId { get; set; }
    string RefreshToken { get; set; }
    DateTime ExpiresAt { get; set; }
}
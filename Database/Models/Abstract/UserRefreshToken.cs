namespace AuthServer.Database.Models.Abstract;

public abstract class UserRefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string RefreshToken { get; set; } = "";
    //public string? PreviousRefreshToken { get; set; } // Note: limits the user to only one location
    public DateTime ExpiresAt { get; set; }
}
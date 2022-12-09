namespace AuthServer.Database.Models.Interfaces;

public interface IServiceAppUserRefreshToken
{
    Guid Id { get; set; }
    Guid UserId { get; set; }
    string RefreshToken { get; set; }
    string? PreviousRefreshToken { get; set; }
    DateTime AbsoluteExpirationTime { get; set; }
}
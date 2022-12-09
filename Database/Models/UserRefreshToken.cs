using System.ComponentModel.DataAnnotations.Schema;
using AuthServer.Database.Models.Interfaces;

namespace AuthServer.Database.Models;

public abstract class UserRefreshToken : IServiceAppUserRefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string RefreshToken { get; set; } = "";
    public string? PreviousRefreshToken { get; set; }
    public DateTime AbsoluteExpirationTime { get; set; }

    [NotMapped]
    public bool HasExpired => DateTime.UtcNow >= AbsoluteExpirationTime;
}
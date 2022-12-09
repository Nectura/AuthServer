using AuthServer.Database.Enums;

namespace AuthServer.Database.Models.Interfaces;

public interface ILocalUserRefreshToken : IServiceAppUserRefreshToken
{
    LocalUser? User { get; set; }
    EUserAuthScope Scopes { get; set; }
}
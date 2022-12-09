using AuthServer.Database.Enums;
using AuthServer.Database.Models.Abstract.Interfaces;

namespace AuthServer.Database.Models.Interfaces;

public interface ISocialUser : IUser
{
    string? AuthProviderUserId { get; set; }
    EAuthProvider AuthProvider { get; set; }
}
using AuthServer.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Database.Interfaces;

public interface IEntityContext
{
    DbSet<LocalUser> LocalUsers { get; set; }
    DbSet<SocialUser> SocialUsers { get; set; }
    DbSet<LocalUserRefreshToken> LocalUserRefreshTokens { get; set; }
    DbSet<SocialUserRefreshToken> SocialUserRefreshTokens { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
using AuthServer.Database.Interfaces;
using AuthServer.Database.Models;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8618

namespace AuthServer.Database;

public sealed class EntityContext : DbContext, IEntityContext
{
    public DbSet<LocalUser> LocalUsers { get; set; }
    public DbSet<SocialUser> SocialUsers { get; set; }
    public DbSet<SocialUserAuthProviderToken> SocialUserAuthProviderTokens { get; set; }
    public DbSet<LocalUserRefreshToken> LocalUserRefreshTokens { get; set; }
    public DbSet<SocialUserRefreshToken> SocialUserRefreshTokens { get; set; }

    public EntityContext(DbContextOptions<EntityContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<LocalUser>()
            .HasMany(m => m.RefreshTokens)
            .WithOne(m => m.User)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SocialUser>()
            .HasMany(m => m.RefreshTokens)
            .WithOne(m => m.User)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<SocialUser>()
            .HasMany(m => m.AuthProviderTokens)
            .WithOne(m => m.User)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<LocalUserRefreshToken>()
            .HasIndex(m => new { m.RefreshToken, m.PreviousRefreshToken });

        builder.Entity<SocialUserRefreshToken>()
            .HasIndex(m => new { m.RefreshToken, m.PreviousRefreshToken });

        builder.Entity<SocialUserAuthProviderToken>()
            .HasKey(m => new { m.UserId, m.AuthProvider });
    }
}
using AuthServer.Database.Interfaces;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories.Abstract;
using AuthServer.Database.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Database.Repositories;

public sealed class SocialUserRefreshTokenRepository : EntityRepository<SocialUserRefreshToken>, ISocialUserRefreshTokenRepository
{
    public SocialUserRefreshTokenRepository(IEntityContext context) : base((DbContext)context)
    {
    }
}
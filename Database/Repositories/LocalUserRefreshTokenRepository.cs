using AuthServer.Database.Models;
using AuthServer.Database.Repositories.Abstract;

namespace AuthServer.Database.Repositories;

public sealed class LocalUserRefreshTokenRepository : EntityRepository<LocalUserRefreshToken>
{
    public LocalUserRefreshTokenRepository(EntityContext context) : base(context)
    {
    }
}
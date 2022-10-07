using AuthServer.Database.Models;
using AuthServer.Database.Repositories.Abstract;

namespace AuthServer.Database.Repositories;

public sealed class ExternalUserRefreshTokenRepository : EntityRepository<ExternalUserRefreshToken>
{
    public ExternalUserRefreshTokenRepository(EntityContext context) : base(context)
    {
    }
}
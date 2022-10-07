using AuthServer.Database.Models;
using AuthServer.Database.Repositories.Abstract;

namespace AuthServer.Database.Repositories;

public sealed class ExternalUserRepository : EntityRepository<ExternalUser>
{
    public ExternalUserRepository(EntityContext context) : base(context)
    {
    }
}
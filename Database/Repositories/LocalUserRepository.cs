using AuthServer.Database.Models;
using AuthServer.Database.Repositories.Abstract;

namespace AuthServer.Database.Repositories;

public sealed class LocalUserRepository : EntityRepository<LocalUser>
{
    public LocalUserRepository(EntityContext context) : base(context)
    {
    }
}
using AuthServer.Database.Interfaces;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories.Abstract;
using AuthServer.Database.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Database.Repositories;

public sealed class LocalUserRepository : EntityRepository<LocalUser>, ILocalUserRepository
{
    public LocalUserRepository(IEntityContext context) : base((DbContext)context)
    {
    }
}
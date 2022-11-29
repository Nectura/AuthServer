using AuthServer.Database.Models;
using AuthServer.Database.Repositories.Abstract.Interfaces;

namespace AuthServer.Database.Repositories.Interfaces;

public interface ILocalUserRefreshTokenRepository : IEntityRepository<LocalUserRefreshToken>
{
}
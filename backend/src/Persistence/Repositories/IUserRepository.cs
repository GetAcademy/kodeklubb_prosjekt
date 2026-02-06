using Persistence.DbModels;

namespace Persistence.Repositories;

public interface IUserRepository
{
    Task<IEnumerable<UserEntity>> GetAllAsync();
    Task<UserEntity?> GetByIdAsync(long id);
    Task<UserEntity?> GetByDiscordIdAsync(string discordId);
    Task AddAsync(UserEntity entity);
    Task SaveChangesAsync();
}

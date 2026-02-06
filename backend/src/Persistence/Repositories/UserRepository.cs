using Microsoft.EntityFrameworkCore;
using Persistence.DbModels;

namespace Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserEntity>> GetAllAsync()
    {
        return await _context.Users.AsNoTracking().ToListAsync();
    }

    public async Task<UserEntity?> GetByIdAsync(long id)
    {
        return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<UserEntity?> GetByDiscordIdAsync(string discordId)
    {
        return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.DiscordId == discordId);
    }

    public async Task AddAsync(UserEntity entity)
    {
        await _context.Users.AddAsync(entity);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

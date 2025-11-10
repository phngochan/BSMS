using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace BSMS.DAL.Repositories.Implementations;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(BSMSDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> IsUsernameExistsAsync(string username)
    {
        return await _dbSet.AnyAsync(u => u.Username == username);
    }

    public async Task<bool> IsEmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(u => u.Role == role)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<User?> GetUserWithVehiclesAsync(int userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(u => u.Vehicles)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<User?> GetUserWithPackagesAsync(int userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(u => u.UserPackages)
                .ThenInclude(up => up.Package)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<User?> GetUserWithTransactionsAsync(int userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(u => u.SwapTransactions)
                .ThenInclude(st => st.Station)
            .Include(u => u.SwapTransactions)
                .ThenInclude(st => st.BatteryTaken)
            .Include(u => u.SwapTransactions)
                .ThenInclude(st => st.BatteryReturned)
            .Include(u => u.Payments)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }
}

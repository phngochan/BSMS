using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> IsUsernameExistsAsync(string username);
    Task<bool> IsEmailExistsAsync(string email);
    Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
    Task<User?> GetUserWithVehiclesAsync(int userId);
    Task<User?> GetUserWithPackagesAsync(int userId);
    Task<User?> GetUserWithTransactionsAsync(int userId);
}

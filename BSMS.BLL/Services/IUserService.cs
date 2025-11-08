using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface IUserService
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<bool> IsUsernameExistsAsync(string username);
    Task<bool> IsEmailExistsAsync(string email);
    Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
    Task<User> CreateUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task UpdatePasswordAsync(string email, string newPasswordHash);
    Task<int> CountUsersAsync();
}

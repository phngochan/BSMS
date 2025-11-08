using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userRepository.GetByEmailAsync(email);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _userRepository.GetByUsernameAsync(username);
    }

    public async Task<bool> IsUsernameExistsAsync(string username)
    {
        return await _userRepository.IsUsernameExistsAsync(username);
    }

    public async Task<bool> IsEmailExistsAsync(string email)
    {
        return await _userRepository.IsEmailExistsAsync(email);
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
    {
        return await _userRepository.GetUsersByRoleAsync(role);
    }

    public async Task<User> CreateUserAsync(User user)
    {
        return await _userRepository.CreateAsync(user);
    }

    public async Task<int> CountUsersAsync()
    {
        return await _userRepository.CountAsync();
    }

    public async Task UpdateUserAsync(User user)
    {
        await _userRepository.UpdateAsync(user);
    }

    public async Task UpdatePasswordAsync(string email, string newPasswordHash)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
            throw new InvalidOperationException("User not found");

        user.PasswordHash = newPasswordHash;
        await _userRepository.UpdateAsync(user);
    }
}

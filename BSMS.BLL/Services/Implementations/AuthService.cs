using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services.Implementations;
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);

        if (user == null)
        {
            return new AuthResult
            {
                Success = false,
                Message = "Invalid username or password"
            };
        }

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            return new AuthResult
            {
                Success = false,
                Message = "Invalid username or password"
            };
        }

        return new AuthResult
        {
            Success = true,
            Message = "Login successful",
            User = user
        };
    }

    public async Task<AuthResult> RegisterAsync(string username, string email, string fullName, string phone, string password)
    {
        if (await _userRepository.IsUsernameExistsAsync(username))
        {
            return new AuthResult
            {
                Success = false,
                Message = "Username already exists"
            };
        }

        if (await _userRepository.IsEmailExistsAsync(email))
        {
            return new AuthResult
            {
                Success = false,
                Message = "Email already exists"
            };
        }

        var user = new User
        {
            Username = username,
            PasswordHash = _passwordHasher.HashPassword(password),
            FullName = fullName,
            Phone = phone,
            Email = email,
            Role = UserRole.Driver,
            CreatedAt = DateTime.Now
        };

        var createdUser = await _userRepository.CreateAsync(user);

        return new AuthResult
        {
            Success = true,
            Message = "Registration successful",
            User = createdUser
        };
    }

    public async Task<AuthResult> ValidateTokenAsync(string token)
    {
        throw new NotImplementedException();
    }
}

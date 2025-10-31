using BSMS.BusinessObjects.DTOs.Auth;
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

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);

        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid username or password"
            };
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid username or password"
            };
        }

        return new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            User = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            }
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Validation
        if (request.Password != request.ConfirmPassword)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Passwords do not match"
            };
        }

        if (await _userRepository.IsUsernameExistsAsync(request.Username))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Username already exists"
            };
        }

        if (await _userRepository.IsEmailExistsAsync(request.Email))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Email already exists"
            };
        }

        // Create user
        var user = new User
        {
            Username = request.Username,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            Role = UserRole.Driver,
            CreatedAt = DateTime.Now
        };

        var createdUser = await _userRepository.CreateAsync(user);

        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            User = new UserDto
            {
                UserId = createdUser.UserId,
                Username = createdUser.Username,
                FullName = createdUser.FullName,
                Email = createdUser.Email,
                Role = createdUser.Role
            }
        };
    }

    public async Task<AuthResponse> ValidateTokenAsync(string token)
    {
        // Implement JWT validation nếu dùng JWT
        // Hoặc validate session nếu dùng Cookie Authentication
        throw new NotImplementedException();
    }
}

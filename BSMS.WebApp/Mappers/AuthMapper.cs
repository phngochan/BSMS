using BSMS.BLL.Services;
using BSMS.BusinessObjects.DTOs.Auth;
using BSMS.BusinessObjects.Models;
using BSMS.WebApp.ViewModels.Auth;

namespace BSMS.WebApp.Mappers;

public static class AuthMapper
{
    public static AuthResponse ToAuthResponse(this AuthResult result)
    {
        return new AuthResponse
        {
            Success = result.Success,
            Message = result.Message,
            User = result.User?.ToUserDto()
        };
    }

    public static UserDto ToUserDto(this User user)
    {
        return new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };
    }
}


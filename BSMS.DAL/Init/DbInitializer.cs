using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace BSMS.DAL.Init;
public static class DbInitializer
{
    public static async Task SeedAsync(BSMSDbContext context, IConfiguration config)
    {
        // Ensure DB is migrated
        await context.Database.MigrateAsync();

        var username = string.IsNullOrWhiteSpace(config["AdminAccount:Username"]) ? "admin" : config["AdminAccount:Username"]!;
        var email = string.IsNullOrWhiteSpace(config["AdminAccount:Email"]) ? "admin@example.com" : config["AdminAccount:Email"]!;
        var password = string.IsNullOrWhiteSpace(config["AdminAccount:Password"]) ? "123456" : config["AdminAccount:Password"]!;

        // Hash password first
        var hashedPassword = HashPassword(password);

        // Check by username first
        var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (existingAdmin != null)
        {
            var needUpdate = existingAdmin.PasswordHash != hashedPassword || existingAdmin.Role != UserRole.Admin || existingAdmin.Email != email;
            if (needUpdate)
            {
                existingAdmin.PasswordHash = hashedPassword;
                existingAdmin.Role = UserRole.Admin;
                existingAdmin.Email = email;
                context.Users.Update(existingAdmin);
                await context.SaveChangesAsync();
            }
        }
        else
        {
            // Create new admin with hashed password
            context.Users.Add(new User { Username = username, Email = email, PasswordHash = hashedPassword, Role = UserRole.Admin, FullName = "Administrator", CreatedAt = DateTime.Now });
            await context.SaveChangesAsync();
        }
    }

    private static string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

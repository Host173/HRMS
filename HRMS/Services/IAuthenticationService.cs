using HRMS.Models;

namespace HRMS.Services;

public interface IAccountService
{
    Task<bool> ValidateUserAsync(string username, string password);
    Task<User?> GetUserByUsernameAsync(string username);
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
}


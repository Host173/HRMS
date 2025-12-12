using HRMS.Models;

namespace HRMS.Services;

// Temporary mock service for testing - replace with database implementation later
public class MockAuthenticationService : IAccountService
{
    // Mock users for testing - remove when database is connected
    private readonly Dictionary<string, string> _mockUsers = new()
    {
        { "admin", "admin123" },
        { "user", "user123" }
    };

    public Task<bool> ValidateUserAsync(string username, string password)
    {
        // This is a mock implementation - replace with database query later
        if (_mockUsers.TryGetValue(username.ToLower(), out var storedPassword))
        {
            return Task.FromResult(storedPassword == password);
        }
        return Task.FromResult(false);
    }

    public Task<User?> GetUserByUsernameAsync(string username)
    {
        // This is a mock implementation - replace with database query later
        if (_mockUsers.ContainsKey(username.ToLower()))
        {
            return Task.FromResult<User?>(new User
            {
                Id = 1,
                Username = username,
                Email = $"{username}@example.com",
                FullName = $"{username} User"
            });
        }
        return Task.FromResult<User?>(null);
    }
}


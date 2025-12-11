namespace HRMS.Models;

public class Employee
{
    public int EmployeeId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public string? AccountStatus { get; set; }
    
    // Additional fields can be added as needed
    public string? NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
}


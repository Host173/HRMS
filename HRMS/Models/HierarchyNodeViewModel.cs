namespace HRMS.Models;

public class HierarchyNodeViewModel
{
    public string EmployeeName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public int Level { get; set; }
    public List<HierarchyNodeViewModel> Subordinates { get; set; } = new();
}


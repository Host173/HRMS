namespace HRMS.Models;

public class DepartmentStatisticsViewModel
{
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int InactiveEmployees { get; set; }
}


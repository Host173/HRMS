using System.Collections.Generic;

namespace HRMS.Models;

public class DiversityReportViewModel
{
    public int TotalEmployees { get; set; }
    public Dictionary<string, int> DepartmentDistribution { get; set; } = new();
    public Dictionary<string, int> PositionDistribution { get; set; } = new();
    public List<DepartmentStatisticsViewModel> DepartmentStatistics { get; set; } = new();
}


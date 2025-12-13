namespace HRMS.Models;

public class ComplianceReportViewModel
{
    public string ReportType { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int CompliantRecords { get; set; }
    public int NonCompliantRecords { get; set; }
    public double CompliancePercentage => TotalRecords > 0 
        ? (CompliantRecords * 100.0 / TotalRecords) 
        : 0;
}


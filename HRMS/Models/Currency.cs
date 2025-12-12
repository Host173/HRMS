using System;
using System.Collections.Generic;

namespace HRMS.Models;

public partial class Currency
{
    public string CurrencyCode { get; set; } = null!;

    public string CurrencyName { get; set; } = null!;

    public decimal? ExchangeRate { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual ICollection<AllowanceDeduction> AllowanceDeduction { get; set; } = new List<AllowanceDeduction>();

    public virtual ICollection<SalaryType> SalaryType { get; set; } = new List<SalaryType>();
}

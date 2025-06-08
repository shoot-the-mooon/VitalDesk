namespace VitalDesk.Core.Models;

public class Patient
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? InsuranceNo { get; set; }
    public DateTime? FirstVisit { get; set; }
    public DateTime? Admission { get; set; }
    public DateTime? Discharge { get; set; }
    
    public int? Age => BirthDate.HasValue ? DateTime.Now.Year - BirthDate.Value.Year : null;
} 
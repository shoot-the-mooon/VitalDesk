using System.Diagnostics.CodeAnalysis;

namespace VitalDesk.Core.Models;

// Ensure public properties remain available for reflection when the application
// is published with trimming (required by Dapper).
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
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
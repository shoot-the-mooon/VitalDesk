using System.Diagnostics.CodeAnalysis;

namespace VitalDesk.Core.Models;

// Preserve public properties for reflection when trimming the application.
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class Vital
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public DateTime MeasuredAt { get; set; }
    public double Temperature { get; set; }
    public int? Pulse { get; set; }
    public int? Systolic { get; set; }
    public int? Diastolic { get; set; }
    public double? Weight { get; set; }
    
    // Navigation property
    public Patient? Patient { get; set; }
} 
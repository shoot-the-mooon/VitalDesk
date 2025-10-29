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
    
    // 食事（朝・昼・夕）
    public string? Breakfast { get; set; } // "○" or "×"
    public string? Lunch { get; set; }     // "○" or "×"
    public string? Dinner { get; set; }    // "○" or "×"
    
    // 睡眠時間（時間）
    public int? Sleep { get; set; }
    
    // 便通（回数）
    public int? BowelMovement { get; set; }
    
    // 備考
    public string? Note { get; set; }
    
    // 食事を結合した文字列（表示用）
    public string MealSummary => $"{Breakfast ?? "－"}{Lunch ?? "－"}{Dinner ?? "－"}";
    
    // Navigation property
    public Patient? Patient { get; set; }
} 
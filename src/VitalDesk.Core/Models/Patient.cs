using System.Diagnostics.CodeAnalysis;

namespace VitalDesk.Core.Models;

// Ensure public properties remain available for reflection when the application
// is published with trimming (required by Dapper).
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class Patient
{
    public int Id { get; set; }
    public string NationalHealthInsurance { get; set; } = string.Empty; // 国保（旧：患者コード）
    public string Symbol { get; set; } = string.Empty; // 記号
    public string Number { get; set; } = string.Empty; // 番号
    public string InsurerName { get; set; } = string.Empty; // 保険者名
    public string Name { get; set; } = string.Empty;
    public string Furigana { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public DateTime? FirstVisit { get; set; }
    public DateTime? Admission { get; set; }
    public DateTime? Discharge { get; set; }
    public string Status { get; set; } = "Admitted"; // Admitted, Discharged, Transferred
    
    public int? Age => BirthDate.HasValue ? DateTime.Now.Year - BirthDate.Value.Year : null;
}

// 患者ステータスの定数
public static class PatientStatus
{
    public const string Admitted = "Admitted";     // 入院中
    public const string Discharged = "Discharged"; // 退院
    public const string Transferred = "Transferred"; // 転棟
}

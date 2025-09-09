using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using VitalDesk.Core.Models;

namespace VitalDesk.App.Services;

public class CsvExportService
{
    public async Task<bool> ExportPatientsAsync(IEnumerable<Patient> patients, string filePath)
    {
        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("国保,記号,番号,保険者名,患者名,フリガナ,生年月日,年齢,初診日,入院日,退院日");
            
            foreach (var patient in patients)
            {
                csv.AppendLine($"{EscapeCsv(patient.NationalHealthInsurance)},{EscapeCsv(patient.Symbol)},{EscapeCsv(patient.Number)},{EscapeCsv(patient.InsurerName)},{EscapeCsv(patient.Name)},{EscapeCsv(patient.Furigana)},{patient.BirthDate:yyyy-MM-dd},{patient.Age},{patient.FirstVisit:yyyy-MM-dd},{patient.Admission:yyyy-MM-dd},{patient.Discharge:yyyy-MM-dd}");
            }
            
            await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<bool> ExportVitalsAsync(IEnumerable<Vital> vitals, string filePath)
    {
        try
        {
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("PatientId,MeasuredAt,Temperature,Pulse,Systolic,Diastolic,Weight");
            
            // Data
            foreach (var vital in vitals)
            {
                csv.AppendLine($"{vital.PatientId}," +
                              $"{vital.MeasuredAt:yyyy-MM-dd HH:mm:ss}," +
                              $"{vital.Temperature:F1}," +
                              $"{vital.Pulse}," +
                              $"{vital.Systolic}," +
                              $"{vital.Diastolic}," +
                              $"{vital.Weight:F1}");
            }
            
            await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private static string EscapeCsv(string? field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;
            
        // If the field contains comma, quote, or newline, wrap it in quotes and escape internal quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        
        return field;
    }
} 
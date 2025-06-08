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
            
            // Header
            csv.AppendLine("Code,Name,BirthDate,Age,InsuranceNo,FirstVisit,Admission,Discharge");
            
            // Data
            foreach (var patient in patients)
            {
                csv.AppendLine($"{EscapeCsvField(patient.Code)}," +
                              $"{EscapeCsvField(patient.Name)}," +
                              $"{patient.BirthDate?.ToString("yyyy-MM-dd")}," +
                              $"{patient.Age}," +
                              $"{EscapeCsvField(patient.InsuranceNo)}," +
                              $"{patient.FirstVisit?.ToString("yyyy-MM-dd")}," +
                              $"{patient.Admission?.ToString("yyyy-MM-dd")}," +
                              $"{patient.Discharge?.ToString("yyyy-MM-dd")}");
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
    
    private string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;
            
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        
        return field;
    }
} 
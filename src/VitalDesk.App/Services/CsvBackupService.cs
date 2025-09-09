using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VitalDesk.Core.Models;
using VitalDesk.Core.Repositories;
using VitalDesk.Core.Migrations;

namespace VitalDesk.App.Services;

public class CsvBackupService
{
    private readonly IPatientRepository _patientRepository;
    private readonly IVitalRepository _vitalRepository;

    public CsvBackupService()
    {
        _patientRepository = new PatientRepository();
        _vitalRepository = new VitalRepository();
    }

    public async Task<string> CreateBackupAsync()
    {
        // デスクトップのVitalDeskフォルダにバックアップを作成
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var backupFolder = Path.Combine(desktopPath, "VitalDesk_Backup");
        Directory.CreateDirectory(backupFolder);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"VitalDesk_Complete_Backup_{timestamp}.csv";
        var backupPath = Path.Combine(backupFolder, backupFileName);

        // 全ての患者データとバイタルデータを取得
        var patients = await _patientRepository.GetAllAsync();
        var vitals = await _vitalRepository.GetAllAsync();

        var csv = new StringBuilder();
        
        // ヘッダー行
        csv.AppendLine("DataType,PatientId,NationalHealthInsurance,Symbol,Number,InsurerName,Name,Furigana,BirthDate,FirstVisit,Admission,Discharge,VitalId,MeasuredAt,Temperature,Pulse,Systolic,Diastolic,Weight");

        // 患者データを出力
        foreach (var patient in patients.OrderBy(p => p.Furigana))
        {
            csv.AppendLine($"Patient,{patient.Id},{EscapeCsvField(patient.NationalHealthInsurance)},{EscapeCsvField(patient.Symbol)},{EscapeCsvField(patient.Number)},{EscapeCsvField(patient.InsurerName)},{EscapeCsvField(patient.Name)},{EscapeCsvField(patient.Furigana)},{FormatDate(patient.BirthDate)},{FormatDate(patient.FirstVisit)},{FormatDate(patient.Admission)},{FormatDate(patient.Discharge)},,,,,,,,");
        }

        // バイタルデータを出力
        foreach (var vital in vitals.OrderBy(v => v.PatientId).ThenBy(v => v.MeasuredAt))
        {
            csv.AppendLine($"Vital,{vital.PatientId},,,,,,,,,,,,{vital.Id},{FormatDateTime(vital.MeasuredAt)},{vital.Temperature},{vital.Pulse},{vital.Systolic},{vital.Diastolic},{vital.Weight}");
        }

        await File.WriteAllTextAsync(backupPath, csv.ToString(), Encoding.UTF8);
        
        return backupPath;
    }

    public async Task<int> ImportBackupAsync(string csvFilePath)
    {
        if (!File.Exists(csvFilePath))
            throw new FileNotFoundException("バックアップファイルが見つかりません。");

        var lines = await File.ReadAllLinesAsync(csvFilePath, Encoding.UTF8);
        
        if (lines.Length < 2)
            throw new InvalidDataException("無効なバックアップファイルです。");

        // 現在のデータを全て削除
        await DatabaseInitializer.ClearAllDataAsync();

        var patients = new List<Patient>();
        var vitals = new List<Vital>();
        var patientIdMapping = new Dictionary<int, int>(); // 古いID -> 新しいID

        // CSVを解析
        for (int i = 1; i < lines.Length; i++) // ヘッダーをスキップ
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var fields = ParseCsvLine(line);
            if (fields.Length < 19) continue;

            var dataType = fields[0];

            if (dataType == "Patient")
            {
                var oldPatientId = int.Parse(fields[1]);
                var patient = new Patient
                {
                    NationalHealthInsurance = fields[2],
                    Symbol = fields[3],
                    Number = fields[4],
                    InsurerName = fields[5],
                    Name = fields[6],
                    Furigana = fields[7],
                    BirthDate = ParseDate(fields[8]),
                    FirstVisit = ParseDate(fields[9]),
                    Admission = ParseDate(fields[10]),
                    Discharge = ParseDate(fields[11])
                };

                var newPatientId = await _patientRepository.CreateAsync(patient);
                patientIdMapping[oldPatientId] = newPatientId;
            }
            else if (dataType == "Vital")
            {
                var oldPatientId = int.Parse(fields[1]);
                if (patientIdMapping.ContainsKey(oldPatientId))
                {
                    var vital = new Vital
                    {
                        PatientId = patientIdMapping[oldPatientId],
                        MeasuredAt = ParseDateTime(fields[14]),
                        Temperature = ParseDouble(fields[15]),
                        Pulse = ParseInt(fields[16]),
                        Systolic = ParseInt(fields[17]),
                        Diastolic = ParseInt(fields[18]),
                        Weight = ParseDouble(fields[19])
                    };

                    await _vitalRepository.CreateAsync(vital);
                }
            }
        }

        return patientIdMapping.Count;
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // カンマ、改行、ダブルクォートが含まれている場合はダブルクォートで囲む
        if (field.Contains(',') || field.Contains('\n') || field.Contains('\r') || field.Contains('"'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        
        return field;
    }

    private static string FormatDate(DateTime? date)
    {
        return date?.ToString("yyyy-MM-dd") ?? "";
    }

    private static string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private static DateTime? ParseDate(string dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
            return null;

        if (DateTime.TryParse(dateStr, out var date))
            return date;

        return null;
    }

    private static DateTime ParseDateTime(string dateTimeStr)
    {
        if (DateTime.TryParse(dateTimeStr, out var dateTime))
            return dateTime;

        return DateTime.Now;
    }

    private static double ParseDouble(string str)
    {
        if (double.TryParse(str, out var value))
            return value;

        return 0;
    }

    private static int? ParseInt(string str)
    {
        if (string.IsNullOrEmpty(str))
            return null;

        if (int.TryParse(str, out var value))
            return value;

        return null;
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // エスケープされたダブルクォート
                    current.Append('"');
                    i++; // 次の文字をスキップ
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
} 
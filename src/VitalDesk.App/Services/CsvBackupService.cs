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
        var backupFolder = Path.Combine(desktopPath, "体温記録_バックアップ");
        Directory.CreateDirectory(backupFolder);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"{timestamp}_BackupFile.csv";
        var backupPath = Path.Combine(backupFolder, backupFileName);

        // 全ての患者データとバイタルデータを取得（全ステータス含む）
        var patients = await _patientRepository.GetAllPatientsIncludingAllStatusAsync();
        var vitals = await _vitalRepository.GetAllAsync();

        var csv = new StringBuilder();
        
        // ヘッダー行（26フィールド: 0-25）
        csv.AppendLine("DataType,PatientId,NationalHealthInsurance,Symbol,Number,InsurerName,Name,Furigana,BirthDate,FirstVisit,Admission,Discharge,Status,VitalId,MeasuredAt,Temperature,Pulse,Systolic,Diastolic,Weight,Breakfast,Lunch,Dinner,Sleep,BowelMovement,Note");

        // 患者データを出力（13フィールド + 空13フィールド = 26フィールド）
        foreach (var patient in patients.OrderBy(p => p.Furigana))
        {
            csv.AppendLine($"Patient,{patient.Id},{EscapeCsvField(patient.NationalHealthInsurance)},{EscapeCsvField(patient.Symbol)},{EscapeCsvField(patient.Number)},{EscapeCsvField(patient.InsurerName)},{EscapeCsvField(patient.Name)},{EscapeCsvField(patient.Furigana)},{FormatDate(patient.BirthDate)},{FormatDate(patient.FirstVisit)},{FormatDate(patient.Admission)},{FormatDate(patient.Discharge)},{EscapeCsvField(patient.Status)},,,,,,,,,,,,,");
        }

        // バイタルデータを出力（2フィールド + 空11フィールド + 13フィールド = 26フィールド）
        foreach (var vital in vitals.OrderBy(v => v.PatientId).ThenBy(v => v.MeasuredAt))
        {
            csv.AppendLine($"Vital,{vital.PatientId},,,,,,,,,,,,{vital.Id},{FormatDateTime(vital.MeasuredAt)},{vital.Temperature},{vital.Pulse},{vital.Systolic},{vital.Diastolic},{vital.Weight},{EscapeCsvField(vital.Breakfast)},{EscapeCsvField(vital.Lunch)},{EscapeCsvField(vital.Dinner)},{vital.Sleep},{vital.BowelMovement},{EscapeCsvField(vital.Note)}");
        }

        await File.WriteAllTextAsync(backupPath, csv.ToString(), Encoding.UTF8);
        
        return backupPath;
    }

    public async Task<int> ImportBackupAsync(string csvFilePath)
    {
        // 復元ログファイルを作成
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var backupFolder = Path.Combine(desktopPath, "体温記録_バックアップ");
        Directory.CreateDirectory(backupFolder);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logPath = Path.Combine(backupFolder, $"{timestamp}_restore_log.txt");
        var log = new StringBuilder();
        
        try
        {
            if (!File.Exists(csvFilePath))
                throw new FileNotFoundException("バックアップファイルが見つかりません。");

            var lines = await File.ReadAllLinesAsync(csvFilePath, Encoding.UTF8);
            
            if (lines.Length < 2)
                throw new InvalidDataException("無効なバックアップファイルです。");

            log.AppendLine($"復元開始: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            log.AppendLine($"ファイル: {Path.GetFileName(csvFilePath)}");
            log.AppendLine($"総行数: {lines.Length}");
            log.AppendLine();

            // 現在のデータを全て削除
            await DatabaseInitializer.ClearAllDataAsync();
            log.AppendLine("既存データを削除しました");
            log.AppendLine();

            var patients = new List<Patient>();
            var vitals = new List<Vital>();
            var patientIdMapping = new Dictionary<int, int>(); // 古いID -> 新しいID
            
            int patientCount = 0;
            int vitalCount = 0;
            int skippedLines = 0;

            // CSVを解析
            for (int i = 1; i < lines.Length; i++) // ヘッダーをスキップ
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

            var fields = ParseCsvLine(line);
            
            // 25フィールドの場合は古いフォーマット（Noteなし）として扱う
            if (fields.Length == 25)
            {
                // Noteフィールドを空文字列として追加
                var expandedFields = new string[26];
                Array.Copy(fields, expandedFields, 25);
                expandedFields[25] = "";
                fields = expandedFields;
            }
            else if (fields.Length < 25)
            {
                skippedLines++;
                continue;
            }

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
                        Discharge = ParseDate(fields[11]),
                        Status = !string.IsNullOrEmpty(fields[12]) ? fields[12] : PatientStatus.Admitted
                    };

                    var newPatientId = await _patientRepository.CreateAsync(patient);
                    patientIdMapping[oldPatientId] = newPatientId;
                    patientCount++;
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
                            Weight = ParseDouble(fields[19]),
                            Breakfast = fields[20],
                            Lunch = fields[21],
                            Dinner = fields[22],
                            Sleep = ParseInt(fields[23]),
                            BowelMovement = ParseInt(fields[24]),
                            Note = fields[25]
                        };

                        var vitalId = await _vitalRepository.CreateAsync(vital);
                        if (vitalId > 0)
                        {
                            vitalCount++;
                        }
                    }
                }
            }
            
            log.AppendLine();
            log.AppendLine("=== 復元結果 ===");
            log.AppendLine($"復元された患者数: {patientCount}人");
            log.AppendLine($"復元されたバイタル数: {vitalCount}件");
            log.AppendLine($"スキップされた行数: {skippedLines}行");
            log.AppendLine($"復元完了: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            // ログファイルを保存
            await File.WriteAllTextAsync(logPath, log.ToString(), Encoding.UTF8);

            return patientIdMapping.Count;
        }
        catch (Exception ex)
        {
            log.AppendLine();
            log.AppendLine("=== エラー発生 ===");
            log.AppendLine($"エラー: {ex.Message}");
            log.AppendLine($"スタックトレース: {ex.StackTrace}");
            
            // エラーが発生してもログを保存
            await File.WriteAllTextAsync(logPath, log.ToString(), Encoding.UTF8);
            
            throw;
        }
    }

    private static string EscapeCsvField(string? field)
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

        // 最後のフィールドを追加
        fields.Add(current.ToString());
        
        return fields.ToArray();
    }
} 
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using VitalDesk.Core.Migrations;

namespace VitalDesk.App.Services;

public class BackupService
{
    private const string BackupFolderName = "Backup";
    private const string LogsFolderName = "Logs";
    
    public Task<bool> CreateBackupAsync()
    {
        try
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var backupDirectory = Path.Combine(baseDirectory, BackupFolderName);
            
            // Create backup directory if it doesn't exist
            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }
            
            // Get database file path
            var connectionString = DatabaseInitializer.GetConnectionString();
            var dbPath = ExtractDbPathFromConnectionString(connectionString);
            
            if (!File.Exists(dbPath))
            {
                LogError("Database file not found for backup");
                return Task.FromResult(false);
            }
            
            // Create backup filename with timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            var backupFileName = $"{timestamp}.zip";
            var backupFilePath = Path.Combine(backupDirectory, backupFileName);
            
            // Create ZIP archive
            using (var archive = ZipFile.Open(backupFilePath, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(dbPath, "Temperatures.db");
            }
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            LogError($"Backup failed: {ex}");
            return Task.FromResult(false);
        }
    }
    
    private string ExtractDbPathFromConnectionString(string connectionString)
    {
        // Extract the Data Source path from the connection string
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Trim().Substring("Data Source=".Length);
            }
        }
        throw new InvalidOperationException("Could not extract database path from connection string");
    }
    
    private void LogError(string message)
    {
        try
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var logsDirectory = Path.Combine(baseDirectory, LogsFolderName);
            
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
            
            var logFileName = $"{DateTime.Now:yyyyMM}.log";
            var logFilePath = Path.Combine(logsDirectory, logFileName);
            
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
            File.AppendAllText(logFilePath, logEntry);
        }
        catch
        {
            // Ignore logging errors to prevent infinite loops
        }
    }
} 
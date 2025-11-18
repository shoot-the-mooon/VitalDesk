using Microsoft.Data.Sqlite;
using Dapper;

namespace VitalDesk.Core.Migrations;

public static class DatabaseInitializer
{
    private const string DatabaseFileName = "Temperatures.db";
    
    public static string GetConnectionString()
    {
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DatabaseFileName);
        return $"Data Source={dbPath}";
    }
    
    public static async Task InitializeAsync()
    {
        var connectionString = GetConnectionString();
        
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        
        // Enable WAL mode
        await connection.ExecuteAsync("PRAGMA journal_mode=WAL;");
        
        // Create Patient table
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Patient (
                Id                      INTEGER PRIMARY KEY AUTOINCREMENT,
                Number                  TEXT    NOT NULL DEFAULT '',
                Name                    TEXT    NOT NULL DEFAULT '',
                Furigana               TEXT    NOT NULL DEFAULT '',
                BirthDate              DATE,
                Admission              DATE,
                Status                 TEXT    NOT NULL DEFAULT 'Admitted'
            );");
        
        // Migration: Add new columns if they don't exist
        try
        {
            await connection.ExecuteAsync("ALTER TABLE Patient ADD COLUMN Number TEXT NOT NULL DEFAULT '';");
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column name"))
        {
            // Column already exists, ignore
        }
        
        // Add Furigana column if it doesn't exist (migration for existing databases)
        try
        {
            await connection.ExecuteAsync("ALTER TABLE Patient ADD COLUMN Furigana TEXT NOT NULL DEFAULT '';");
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column name"))
        {
            // Column already exists, ignore
        }
        
        // Add Status column if it doesn't exist
        try
        {
            await connection.ExecuteAsync("ALTER TABLE Patient ADD COLUMN Status TEXT NOT NULL DEFAULT 'Admitted';");
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column name"))
        {
            // Column already exists, ignore
        }
        
        // Ensure all NOT NULL columns have proper default values
        try
        {
            await connection.ExecuteAsync("UPDATE Patient SET Name = '' WHERE Name IS NULL");
            await connection.ExecuteAsync("UPDATE Patient SET Furigana = '' WHERE Furigana IS NULL");
            await connection.ExecuteAsync("UPDATE Patient SET Number = '' WHERE Number IS NULL");
            await connection.ExecuteAsync("UPDATE Patient SET Status = 'Admitted' WHERE Status IS NULL OR Status = ''");
        }
        catch (Exception)
        {
            // Ignore migration errors
        }
        
        // Create Vital table
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Vital (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                PatientId     INTEGER NOT NULL,
                MeasuredAt    DATETIME NOT NULL,
                Temperature   REAL     NOT NULL,
                Pulse         INTEGER,
                Systolic      INTEGER,
                Diastolic     INTEGER,
                Weight        REAL,
                Breakfast     TEXT,
                Lunch         TEXT,
                Dinner        TEXT,
                Sleep         INTEGER,
                BowelMovement INTEGER,
                Note          TEXT,
                FOREIGN KEY(PatientId) REFERENCES Patient(Id)
            );");
        
        // Create index
        await connection.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS IX_Vital_Patient_Time 
            ON Vital(PatientId, MeasuredAt);");
    }
    
    public static async Task ClearAllDataAsync()
    {
        var connectionString = GetConnectionString();
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        
        // Delete all data from tables
        await connection.ExecuteAsync("DELETE FROM Vital");
        await connection.ExecuteAsync("DELETE FROM Patient");
        
        // Reset auto-increment counters
        await connection.ExecuteAsync("DELETE FROM sqlite_sequence WHERE name IN ('Patient', 'Vital')");
    }
}

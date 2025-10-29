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
                NationalHealthInsurance TEXT    NOT NULL DEFAULT '',
                Symbol                  TEXT    NOT NULL DEFAULT '',
                Number                  TEXT    NOT NULL DEFAULT '',
                InsurerName            TEXT    NOT NULL DEFAULT '',
                Name                    TEXT    NOT NULL DEFAULT '',
                Furigana               TEXT    NOT NULL DEFAULT '',
                BirthDate              DATE,
                FirstVisit             DATE,
                Admission              DATE,
                Discharge              DATE,
                Status                 TEXT    NOT NULL DEFAULT 'Admitted'
            );");
        
        // Migration: Add new columns if they don't exist
        try
        {
            await connection.ExecuteAsync("ALTER TABLE Patient ADD COLUMN NationalHealthInsurance TEXT NOT NULL DEFAULT '';");
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column name"))
        {
            // Column already exists, ignore
        }
        
        try
        {
            await connection.ExecuteAsync("ALTER TABLE Patient ADD COLUMN Symbol TEXT NOT NULL DEFAULT '';");
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column name"))
        {
            // Column already exists, ignore
        }
        
        try
        {
            await connection.ExecuteAsync("ALTER TABLE Patient ADD COLUMN Number TEXT NOT NULL DEFAULT '';");
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column name"))
        {
            // Column already exists, ignore
        }
        
        try
        {
            await connection.ExecuteAsync("ALTER TABLE Patient ADD COLUMN InsurerName TEXT NOT NULL DEFAULT '';");
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
        
        // Migrate existing Code column to NationalHealthInsurance if Code exists
        try
        {
            var hasCodeColumn = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM pragma_table_info('Patient') WHERE name='Code'");
            
            if (hasCodeColumn > 0)
            {
                // First, update NationalHealthInsurance from Code where it's empty
                await connection.ExecuteAsync(
                    "UPDATE Patient SET NationalHealthInsurance = Code WHERE NationalHealthInsurance = '' AND Code IS NOT NULL");
                
                // Then set default value for Code column to avoid NOT NULL constraint errors
                await connection.ExecuteAsync(
                    "UPDATE Patient SET Code = '' WHERE Code IS NULL");
            }
        }
        catch (Exception)
        {
            // Ignore migration errors
        }
        
        // Handle old InsuranceNo column if it exists
        try
        {
            var hasInsuranceNoColumn = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM pragma_table_info('Patient') WHERE name='InsuranceNo'");
            
            if (hasInsuranceNoColumn > 0)
            {
                // Set default value for InsuranceNo column to avoid NOT NULL constraint errors
                await connection.ExecuteAsync(
                    "UPDATE Patient SET InsuranceNo = '' WHERE InsuranceNo IS NULL");
            }
        }
        catch (Exception)
        {
            // Ignore migration errors
        }
        
        // Ensure all NOT NULL columns have proper default values
        try
        {
            await connection.ExecuteAsync("UPDATE Patient SET Name = '' WHERE Name IS NULL");
            await connection.ExecuteAsync("UPDATE Patient SET Furigana = '' WHERE Furigana IS NULL");
            await connection.ExecuteAsync("UPDATE Patient SET NationalHealthInsurance = '' WHERE NationalHealthInsurance IS NULL");
            await connection.ExecuteAsync("UPDATE Patient SET Symbol = '' WHERE Symbol IS NULL");
            await connection.ExecuteAsync("UPDATE Patient SET Number = '' WHERE Number IS NULL");
            await connection.ExecuteAsync("UPDATE Patient SET InsurerName = '' WHERE InsurerName IS NULL");
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

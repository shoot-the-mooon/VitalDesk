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
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Code        TEXT    NOT NULL UNIQUE,
                Name        TEXT    NOT NULL,
                BirthDate   DATE,
                InsuranceNo TEXT,
                FirstVisit  DATE,
                Admission   DATE,
                Discharge   DATE
            );");
        
        // Create Vital table
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Vital (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                PatientId   INTEGER NOT NULL,
                MeasuredAt  DATETIME NOT NULL,
                Temperature REAL     NOT NULL,
                Pulse       INTEGER,
                Systolic    INTEGER,
                Diastolic   INTEGER,
                Weight      REAL,
                FOREIGN KEY(PatientId) REFERENCES Patient(Id)
            );");
        
        // Create index
        await connection.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS IX_Vital_Patient_Time 
            ON Vital(PatientId, MeasuredAt);");
    }
} 
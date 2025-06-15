using Microsoft.Data.Sqlite;
using Dapper;
using VitalDesk.Core.Models;
using VitalDesk.Core.Migrations;

namespace VitalDesk.Core.Repositories;

public class VitalRepository : IVitalRepository
{
    private readonly string _connectionString;
    
    public VitalRepository()
    {
        _connectionString = DatabaseInitializer.GetConnectionString();
    }
    
    public async Task<IEnumerable<Vital>> GetAllAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryAsync<Vital>("SELECT * FROM Vital ORDER BY MeasuredAt DESC");
    }
    
    public async Task<IEnumerable<Vital>> GetByPatientIdAsync(int patientId)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryAsync<Vital>(
            "SELECT * FROM Vital WHERE PatientId = @PatientId ORDER BY MeasuredAt DESC",
            new { PatientId = patientId });
    }
    
    public async Task<IEnumerable<Vital>> GetByPatientIdAndDateRangeAsync(int patientId, DateTime startDate, DateTime endDate)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryAsync<Vital>(
            @"SELECT * FROM Vital 
              WHERE PatientId = @PatientId 
                AND MeasuredAt >= @StartDate 
                AND MeasuredAt <= @EndDate 
              ORDER BY MeasuredAt ASC",
            new { PatientId = patientId, StartDate = startDate, EndDate = endDate });
    }
    
    public async Task<Vital?> GetByIdAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Vital>(
            "SELECT * FROM Vital WHERE Id = @Id", new { Id = id });
    }
    
    public async Task<Vital?> GetByPatientIdAndDateAsync(int patientId, DateTime measuredAt)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Vital>(
            @"SELECT * FROM Vital 
              WHERE PatientId = @PatientId 
                AND date(MeasuredAt) = date(@MeasuredAt)", 
            new { PatientId = patientId, MeasuredAt = measuredAt });
    }
    
    public async Task<int> CreateAsync(Vital vital)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = @"
            INSERT INTO Vital (PatientId, MeasuredAt, Temperature, Pulse, Systolic, Diastolic, Weight)
            VALUES (@PatientId, @MeasuredAt, @Temperature, @Pulse, @Systolic, @Diastolic, @Weight);
            SELECT last_insert_rowid();";
        
        var parameters = new DynamicParameters();
        parameters.Add("@PatientId", vital.PatientId);
        parameters.Add("@MeasuredAt", vital.MeasuredAt);
        parameters.Add("@Temperature", vital.Temperature);
        parameters.Add("@Pulse", vital.Pulse);
        parameters.Add("@Systolic", vital.Systolic);
        parameters.Add("@Diastolic", vital.Diastolic);
        parameters.Add("@Weight", vital.Weight);
        
        return await connection.QuerySingleAsync<int>(sql, parameters);
    }
    
    public async Task<bool> UpdateAsync(Vital vital)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = @"
            UPDATE Vital 
            SET PatientId = @PatientId, MeasuredAt = @MeasuredAt, Temperature = @Temperature,
                Pulse = @Pulse, Systolic = @Systolic, Diastolic = @Diastolic, Weight = @Weight
            WHERE Id = @Id";
        
        var parameters = new DynamicParameters();
        parameters.Add("@Id", vital.Id);
        parameters.Add("@PatientId", vital.PatientId);
        parameters.Add("@MeasuredAt", vital.MeasuredAt);
        parameters.Add("@Temperature", vital.Temperature);
        parameters.Add("@Pulse", vital.Pulse);
        parameters.Add("@Systolic", vital.Systolic);
        parameters.Add("@Diastolic", vital.Diastolic);
        parameters.Add("@Weight", vital.Weight);
        
        var rowsAffected = await connection.ExecuteAsync(sql, parameters);
        return rowsAffected > 0;
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        var rowsAffected = await connection.ExecuteAsync(
            "DELETE FROM Vital WHERE Id = @Id", new { Id = id });
        return rowsAffected > 0;
    }
} 
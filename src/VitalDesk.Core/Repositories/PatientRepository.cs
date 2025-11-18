using Microsoft.Data.Sqlite;
using Dapper;
using VitalDesk.Core.Models;
using VitalDesk.Core.Migrations;

namespace VitalDesk.Core.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly string _connectionString;
    
    public PatientRepository()
    {
        _connectionString = DatabaseInitializer.GetConnectionString();
    }
    
    public async Task<IEnumerable<Patient>> GetAllAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryAsync<Patient>(
            "SELECT * FROM Patient WHERE Status = @Status ORDER BY Furigana, Name",
            new { Status = PatientStatus.Admitted });
    }
    
    public async Task<IEnumerable<Patient>> GetAllPatientsIncludingAllStatusAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryAsync<Patient>(
            "SELECT * FROM Patient ORDER BY Furigana, Name");
    }
    
    public async Task<Patient?> GetByIdAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Patient>(
            "SELECT * FROM Patient WHERE Id = @Id", new { Id = id });
    }
    
    public async Task<IEnumerable<Patient>> SearchAsync(string searchTerm)
    {
        using var connection = new SqliteConnection(_connectionString);
        var searchPattern = $"%{searchTerm}%";
        return await connection.QueryAsync<Patient>(
            "SELECT * FROM Patient WHERE Status = @Status AND (Name LIKE @SearchPattern OR Number LIKE @SearchPattern OR Furigana LIKE @SearchPattern) ORDER BY Furigana, Name",
            new { Status = PatientStatus.Admitted, SearchPattern = searchPattern });
    }
    
    public async Task<int> CreateAsync(Patient patient)
    {
        using var connection = new SqliteConnection(_connectionString);
        
        var sql = @"
            INSERT INTO Patient (Number, Name, Furigana, BirthDate, Admission, Status)
            VALUES (@Number, @Name, @Furigana, @BirthDate, @Admission, @Status);
            SELECT last_insert_rowid();";
        
        var parameters = new DynamicParameters();
        parameters.Add("@Number", patient.Number ?? string.Empty);
        parameters.Add("@Name", patient.Name ?? string.Empty);
        parameters.Add("@Furigana", patient.Furigana ?? string.Empty);
        parameters.Add("@BirthDate", patient.BirthDate);
        parameters.Add("@Admission", patient.Admission);
        parameters.Add("@Status", patient.Status ?? PatientStatus.Admitted);
        
        return await connection.QuerySingleAsync<int>(sql, parameters);
    }
    
    public async Task<bool> UpdateAsync(Patient patient)
    {
        using var connection = new SqliteConnection(_connectionString);
        
        var sql = @"
            UPDATE Patient 
            SET Number = @Number, Name = @Name, Furigana = @Furigana, 
                BirthDate = @BirthDate, Admission = @Admission, Status = @Status
            WHERE Id = @Id";
        
        var parameters = new DynamicParameters();
        parameters.Add("@Id", patient.Id);
        parameters.Add("@Number", patient.Number ?? string.Empty);
        parameters.Add("@Name", patient.Name ?? string.Empty);
        parameters.Add("@Furigana", patient.Furigana ?? string.Empty);
        parameters.Add("@BirthDate", patient.BirthDate);
        parameters.Add("@Admission", patient.Admission);
        parameters.Add("@Status", patient.Status ?? PatientStatus.Admitted);
        
        var rowsAffected = await connection.ExecuteAsync(sql, parameters);
        return rowsAffected > 0;
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        
        // First delete related vital records to avoid foreign key constraint
        await connection.ExecuteAsync("DELETE FROM Vital WHERE PatientId = @Id", new { Id = id });
        
        // Then delete the patient
        var rowsAffected = await connection.ExecuteAsync(
            "DELETE FROM Patient WHERE Id = @Id", new { Id = id });
        return rowsAffected > 0;
    }
    
    
    public async Task<IEnumerable<Patient>> GetDischargedPatientsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryAsync<Patient>(
            "SELECT * FROM Patient WHERE Status = @Status ORDER BY Furigana, Name",
            new { Status = PatientStatus.Discharged });
    }
    
    public async Task<IEnumerable<Patient>> GetTransferredPatientsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryAsync<Patient>(
            "SELECT * FROM Patient WHERE Status = @Status ORDER BY Furigana, Name",
            new { Status = PatientStatus.Transferred });
    }
}

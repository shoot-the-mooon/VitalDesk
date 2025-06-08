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
        return await connection.QueryAsync<Patient>("SELECT * FROM Patient ORDER BY Name");
    }
    
    public async Task<Patient?> GetByIdAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Patient>(
            "SELECT * FROM Patient WHERE Id = @Id", new { Id = id });
    }
    
    public async Task<Patient?> GetByCodeAsync(string code)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Patient>(
            "SELECT * FROM Patient WHERE Code = @Code", new { Code = code });
    }
    
    public async Task<IEnumerable<Patient>> SearchAsync(string searchTerm)
    {
        using var connection = new SqliteConnection(_connectionString);
        var searchPattern = $"%{searchTerm}%";
        return await connection.QueryAsync<Patient>(
            "SELECT * FROM Patient WHERE Name LIKE @SearchPattern OR Code LIKE @SearchPattern ORDER BY Name",
            new { SearchPattern = searchPattern });
    }
    
    public async Task<int> CreateAsync(Patient patient)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = @"
            INSERT INTO Patient (Code, Name, BirthDate, InsuranceNo, FirstVisit, Admission, Discharge)
            VALUES (@Code, @Name, @BirthDate, @InsuranceNo, @FirstVisit, @Admission, @Discharge);
            SELECT last_insert_rowid();";
        
        return await connection.QuerySingleAsync<int>(sql, patient);
    }
    
    public async Task<bool> UpdateAsync(Patient patient)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = @"
            UPDATE Patient 
            SET Code = @Code, Name = @Name, BirthDate = @BirthDate, 
                InsuranceNo = @InsuranceNo, FirstVisit = @FirstVisit, 
                Admission = @Admission, Discharge = @Discharge
            WHERE Id = @Id";
        
        var rowsAffected = await connection.ExecuteAsync(sql, patient);
        return rowsAffected > 0;
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        var rowsAffected = await connection.ExecuteAsync(
            "DELETE FROM Patient WHERE Id = @Id", new { Id = id });
        return rowsAffected > 0;
    }
    
    public async Task<bool> ExistsAsync(string code)
    {
        using var connection = new SqliteConnection(_connectionString);
        var count = await connection.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM Patient WHERE Code = @Code", new { Code = code });
        return count > 0;
    }
} 
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
    
    public async Task<Patient?> GetByCodeAsync(string nationalHealthInsurance)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Patient>(
            "SELECT * FROM Patient WHERE NationalHealthInsurance = @NationalHealthInsurance", 
            new { NationalHealthInsurance = nationalHealthInsurance });
    }
    
    public async Task<IEnumerable<Patient>> SearchAsync(string searchTerm)
    {
        using var connection = new SqliteConnection(_connectionString);
        var searchPattern = $"%{searchTerm}%";
        return await connection.QueryAsync<Patient>(
            "SELECT * FROM Patient WHERE Status = @Status AND (Name LIKE @SearchPattern OR NationalHealthInsurance LIKE @SearchPattern OR Furigana LIKE @SearchPattern) ORDER BY Furigana, Name",
            new { Status = PatientStatus.Admitted, SearchPattern = searchPattern });
    }
    
    public async Task<int> CreateAsync(Patient patient)
    {
        using var connection = new SqliteConnection(_connectionString);
        
        // Check if old Code column exists
        var hasCodeColumn = false;
        var hasInsuranceNoColumn = false;
        try
        {
            hasCodeColumn = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM pragma_table_info('Patient') WHERE name='Code'") > 0;
            hasInsuranceNoColumn = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM pragma_table_info('Patient') WHERE name='InsuranceNo'") > 0;
        }
        catch
        {
            // Ignore errors, use default behavior
        }
        
        var sql = @"
            INSERT INTO Patient (NationalHealthInsurance, Symbol, Number, InsurerName, Name, Furigana, BirthDate, FirstVisit, Admission, Discharge, Status";
        
        if (hasCodeColumn)
        {
            sql += ", Code";
        }
        if (hasInsuranceNoColumn)
        {
            sql += ", InsuranceNo";
        }
        
        sql += @")
            VALUES (@NationalHealthInsurance, @Symbol, @Number, @InsurerName, @Name, @Furigana, @BirthDate, @FirstVisit, @Admission, @Discharge, @Status";
        
        if (hasCodeColumn)
        {
            sql += ", @Code";
        }
        if (hasInsuranceNoColumn)
        {
            sql += ", @InsuranceNo";
        }
        
        sql += @");
            SELECT last_insert_rowid();";
        
        var parameters = new DynamicParameters();
        parameters.Add("@NationalHealthInsurance", patient.NationalHealthInsurance ?? string.Empty);
        parameters.Add("@Symbol", patient.Symbol ?? string.Empty);
        parameters.Add("@Number", patient.Number ?? string.Empty);
        parameters.Add("@InsurerName", patient.InsurerName ?? string.Empty);
        parameters.Add("@Name", patient.Name ?? string.Empty);
        parameters.Add("@Furigana", patient.Furigana ?? string.Empty);
        parameters.Add("@BirthDate", patient.BirthDate);
        parameters.Add("@FirstVisit", patient.FirstVisit);
        parameters.Add("@Admission", patient.Admission);
        parameters.Add("@Discharge", patient.Discharge);
        parameters.Add("@Status", patient.Status ?? PatientStatus.Admitted);
        
        if (hasCodeColumn)
        {
            parameters.Add("@Code", patient.NationalHealthInsurance ?? string.Empty);
        }
        if (hasInsuranceNoColumn)
        {
            parameters.Add("@InsuranceNo", string.Empty);
        }
        
        return await connection.QuerySingleAsync<int>(sql, parameters);
    }
    
    public async Task<bool> UpdateAsync(Patient patient)
    {
        using var connection = new SqliteConnection(_connectionString);
        
        // Check if old Code column exists
        var hasCodeColumn = false;
        var hasInsuranceNoColumn = false;
        try
        {
            hasCodeColumn = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM pragma_table_info('Patient') WHERE name='Code'") > 0;
            hasInsuranceNoColumn = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM pragma_table_info('Patient') WHERE name='InsuranceNo'") > 0;
        }
        catch
        {
            // Ignore errors, use default behavior
        }
        
        var sql = @"
            UPDATE Patient 
            SET NationalHealthInsurance = @NationalHealthInsurance, Symbol = @Symbol, Number = @Number, InsurerName = @InsurerName,
                Name = @Name, Furigana = @Furigana, BirthDate = @BirthDate, 
                FirstVisit = @FirstVisit, Admission = @Admission, Discharge = @Discharge, Status = @Status";
        
        if (hasCodeColumn)
        {
            sql += ", Code = @Code";
        }
        if (hasInsuranceNoColumn)
        {
            sql += ", InsuranceNo = @InsuranceNo";
        }
        
        sql += " WHERE Id = @Id";
        
        var parameters = new DynamicParameters();
        parameters.Add("@Id", patient.Id);
        parameters.Add("@NationalHealthInsurance", patient.NationalHealthInsurance ?? string.Empty);
        parameters.Add("@Symbol", patient.Symbol ?? string.Empty);
        parameters.Add("@Number", patient.Number ?? string.Empty);
        parameters.Add("@InsurerName", patient.InsurerName ?? string.Empty);
        parameters.Add("@Name", patient.Name ?? string.Empty);
        parameters.Add("@Furigana", patient.Furigana ?? string.Empty);
        parameters.Add("@BirthDate", patient.BirthDate);
        parameters.Add("@FirstVisit", patient.FirstVisit);
        parameters.Add("@Admission", patient.Admission);
        parameters.Add("@Discharge", patient.Discharge);
        parameters.Add("@Status", patient.Status ?? PatientStatus.Admitted);
        
        if (hasCodeColumn)
        {
            parameters.Add("@Code", patient.NationalHealthInsurance ?? string.Empty);
        }
        if (hasInsuranceNoColumn)
        {
            parameters.Add("@InsuranceNo", string.Empty);
        }
        
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
    
    public async Task<bool> ExistsAsync(string nationalHealthInsurance)
    {
        using var connection = new SqliteConnection(_connectionString);
        var count = await connection.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM Patient WHERE NationalHealthInsurance = @NationalHealthInsurance", 
            new { NationalHealthInsurance = nationalHealthInsurance });
        return count > 0;
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

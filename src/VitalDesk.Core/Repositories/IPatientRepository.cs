using VitalDesk.Core.Models;

namespace VitalDesk.Core.Repositories;

public interface IPatientRepository
{
    Task<IEnumerable<Patient>> GetAllAsync();
    Task<IEnumerable<Patient>> GetAllPatientsIncludingAllStatusAsync(); // バックアップ用：全ステータスの患者を取得
    Task<Patient?> GetByIdAsync(int id);
    Task<IEnumerable<Patient>> SearchAsync(string searchTerm);
    Task<int> CreateAsync(Patient patient);
    Task<bool> UpdateAsync(Patient patient);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Patient>> GetDischargedPatientsAsync();
    Task<IEnumerable<Patient>> GetTransferredPatientsAsync();
} 
using VitalDesk.Core.Models;

namespace VitalDesk.Core.Repositories;

public interface IPatientRepository
{
    Task<IEnumerable<Patient>> GetAllAsync();
    Task<Patient?> GetByIdAsync(int id);
    Task<Patient?> GetByCodeAsync(string nationalHealthInsurance);
    Task<IEnumerable<Patient>> SearchAsync(string searchTerm);
    Task<int> CreateAsync(Patient patient);
    Task<bool> UpdateAsync(Patient patient);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(string nationalHealthInsurance);
    Task<IEnumerable<Patient>> GetDischargedPatientsAsync();
    Task<IEnumerable<Patient>> GetTransferredPatientsAsync();
} 
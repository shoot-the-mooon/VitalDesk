using VitalDesk.Core.Models;

namespace VitalDesk.Core.Repositories;

public interface IVitalRepository
{
    Task<IEnumerable<Vital>> GetAllAsync();
    Task<IEnumerable<Vital>> GetByPatientIdAsync(int patientId);
    Task<IEnumerable<Vital>> GetByPatientIdAndDateRangeAsync(int patientId, DateTime startDate, DateTime endDate);
    Task<Vital?> GetByIdAsync(int id);
    Task<Vital?> GetByPatientIdAndDateAsync(int patientId, DateTime measuredAt);
    Task<int> CreateAsync(Vital vital);
    Task<bool> UpdateAsync(Vital vital);
    Task<bool> DeleteAsync(int id);
    Task<int> DeleteByPatientIdAndDateAsync(int patientId, DateTime measuredAt);
} 
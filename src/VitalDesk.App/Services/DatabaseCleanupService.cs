using System;
using System.Threading.Tasks;
using VitalDesk.Core.Repositories;
using VitalDesk.Core.Models;
using System.Linq;

namespace VitalDesk.App.Services;

public class DatabaseCleanupService
{
    private readonly IPatientRepository _patientRepository;

    public DatabaseCleanupService()
    {
        _patientRepository = new PatientRepository();
    }

    public async Task CleanupInvalidDatesAsync()
    {
        try
        {
            var patients = await _patientRepository.GetAllAsync();
            
            foreach (var patient in patients)
            {
                bool needsUpdate = false;
                
                // 無効な日付をチェックして修正
                if (IsInvalidDate(patient.BirthDate))
                {
                    patient.BirthDate = null;
                    needsUpdate = true;
                }
                
                if (IsInvalidDate(patient.FirstVisit))
                {
                    patient.FirstVisit = DateTime.Now;
                    needsUpdate = true;
                }
                
                if (IsInvalidDate(patient.Admission))
                {
                    patient.Admission = null;
                    needsUpdate = true;
                }
                
                if (IsInvalidDate(patient.Discharge))
                {
                    patient.Discharge = null;
                    needsUpdate = true;
                }
                
                if (needsUpdate)
                {
                    await _patientRepository.UpdateAsync(patient);
                    System.Diagnostics.Debug.WriteLine($"Cleaned up patient {patient.Code} dates");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during database cleanup: {ex.Message}");
        }
    }

    private static bool IsInvalidDate(DateTime? dateTime)
    {
        return dateTime.HasValue && (dateTime.Value.Year < 1 || dateTime.Value.Year > 9999);
    }
} 
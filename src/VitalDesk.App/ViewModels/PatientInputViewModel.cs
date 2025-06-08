using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalDesk.Core.Models;
using VitalDesk.Core.Repositories;
using VitalDesk.App.Extensions;

namespace VitalDesk.App.ViewModels;

public partial class PatientInputViewModel : ObservableValidator
{
    private readonly IPatientRepository _patientRepository;
    
    [ObservableProperty]
    [Required(ErrorMessage = "Patient code is required")]
    [MinLength(1, ErrorMessage = "Patient code cannot be empty")]
    private string _code = string.Empty;
    
    [ObservableProperty]
    [Required(ErrorMessage = "Patient name is required")]
    [MinLength(1, ErrorMessage = "Patient name cannot be empty")]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private DateTimeOffset? _birthDate;
    
    [ObservableProperty]
    private string _insuranceNo = string.Empty;
    
    [ObservableProperty]
    private DateTimeOffset? _firstVisit = DateTimeOffset.Now;
    
    [ObservableProperty]
    private DateTimeOffset? _admission;
    
    [ObservableProperty]
    private DateTimeOffset? _discharge;
    
    [ObservableProperty]
    private bool _isValid = true;
    
    [ObservableProperty]
    private string _validationErrors = string.Empty;
    
    [ObservableProperty]
    private bool _isSaving;
    
    public bool IsEditMode { get; private set; }
    public int? PatientId { get; private set; }
    
    public event EventHandler<bool>? RequestClose;
    
    public PatientInputViewModel()
    {
        _patientRepository = new PatientRepository();
        ValidateAllProperties();
    }
    
    public void SetEditMode(Patient patient)
    {
        IsEditMode = true;
        PatientId = patient.Id;
        Code = patient.Code;
        Name = patient.Name;
        BirthDate = patient.BirthDate?.ToDateTimeOffset();
        InsuranceNo = patient.InsuranceNo ?? string.Empty;
        FirstVisit = patient.FirstVisit?.ToDateTimeOffset();
        Admission = patient.Admission?.ToDateTimeOffset();
        Discharge = patient.Discharge?.ToDateTimeOffset();
        
        ValidateAllProperties();
    }
    
    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidateAllProperties();
        
        if (HasErrors)
        {
            IsValid = false;
            ValidationErrors = string.Join("\n", GetErrors().Select(error => error.ErrorMessage));
            return;
        }
        
        try
        {
            IsSaving = true;
            
            var patient = new Patient
            {
                Id = PatientId ?? 0,
                Code = Code.Trim(),
                Name = Name.Trim(),
                BirthDate = BirthDate?.DateTime,
                InsuranceNo = string.IsNullOrWhiteSpace(InsuranceNo) ? null : InsuranceNo.Trim(),
                FirstVisit = FirstVisit?.DateTime,
                Admission = Admission?.DateTime,
                Discharge = Discharge?.DateTime
            };
            
            bool success;
            if (IsEditMode)
            {
                success = await _patientRepository.UpdateAsync(patient);
            }
            else
            {
                // Check if patient code already exists
                var existingPatient = await _patientRepository.GetByCodeAsync(patient.Code);
                if (existingPatient != null)
                {
                    ValidationErrors = "Patient code already exists. Please use a different code.";
                    IsValid = false;
                    return;
                }
                
                var id = await _patientRepository.CreateAsync(patient);
                success = id > 0;
            }
            
            if (success)
            {
                RequestClose?.Invoke(this, true);
            }
            else
            {
                ValidationErrors = "Failed to save patient data.";
                IsValid = false;
            }
        }
        catch (Exception ex)
        {
            ValidationErrors = $"Error saving data: {ex.Message}";
            IsValid = false;
        }
        finally
        {
            IsSaving = false;
        }
    }
    
    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, false);
    }
    
    partial void OnCodeChanged(string value)
    {
        ValidateProperty(value, nameof(Code));
        UpdateValidationState();
    }
    
    partial void OnNameChanged(string value)
    {
        ValidateProperty(value, nameof(Name));
        UpdateValidationState();
    }
    
    private void UpdateValidationState()
    {
        IsValid = !HasErrors;
        if (HasErrors)
        {
            ValidationErrors = string.Join("\n", GetErrors().Select(error => error.ErrorMessage));
        }
        else
        {
            ValidationErrors = string.Empty;
        }
    }
} 
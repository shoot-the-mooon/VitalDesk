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
    private string _number = string.Empty;
    
    [ObservableProperty]
    [Required(ErrorMessage = "Patient name is required")]
    [MinLength(1, ErrorMessage = "Patient name cannot be empty")]
    private string _name = string.Empty;
    
    [ObservableProperty]
    [Required(ErrorMessage = "Furigana is required")]
    [MinLength(1, ErrorMessage = "Furigana cannot be empty")]
    private string _furigana = string.Empty;
    
    [ObservableProperty]
    private DateTimeOffset? _birthDate;
    
    [ObservableProperty]
    private DateTimeOffset? _admission;
    
    [ObservableProperty]
    private bool _isValid = true;
    
    [ObservableProperty]
    private string _validationErrors = string.Empty;
    
    [ObservableProperty]
    private bool _isSaving;
    
    public bool IsEditMode { get; private set; }
    public int? PatientId { get; private set; }
    
    public event EventHandler<Patient?>? RequestClose;
    
    public PatientInputViewModel()
    {
        _patientRepository = new PatientRepository();
        ValidateAllProperties();
    }
    
    public void SetEditMode(Patient patient)
    {
        IsEditMode = true;
        PatientId = patient.Id;
        Number = patient.Number ?? string.Empty;
        Name = patient.Name;
        Furigana = patient.Furigana ?? string.Empty;
        BirthDate = patient.BirthDate?.ToDateTimeOffset();
        Admission = patient.Admission?.ToDateTimeOffset();
        
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
                Number = Number.Trim(),
                Name = Name.Trim(),
                Furigana = Furigana.Trim(),
                BirthDate = BirthDate?.DateTime,
                Admission = Admission?.DateTime,
                Status = PatientStatus.Admitted // デフォルトは入院中（退院・転棟はボタンから変更）
            };
            
            bool success;
            Patient? savedPatient = null;
            if (IsEditMode)
            {
                success = await _patientRepository.UpdateAsync(patient);
                if (success)
                {
                    savedPatient = patient;
                }
            }
            else
            {
                var id = await _patientRepository.CreateAsync(patient);
                success = id > 0;
                if (success)
                {
                    patient.Id = id;
                    savedPatient = patient;
                }
            }
            
            if (success)
            {
                RequestClose?.Invoke(this, savedPatient);
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
        RequestClose?.Invoke(this, null);
    }
    
    partial void OnNumberChanged(string value)
    {
        ValidateProperty(value, nameof(Number));
        UpdateValidationState();
    }
    
    partial void OnNameChanged(string value)
    {
        ValidateProperty(value, nameof(Name));
        UpdateValidationState();
    }
    
    partial void OnFuriganaChanged(string value)
    {
        ValidateProperty(value, nameof(Furigana));
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
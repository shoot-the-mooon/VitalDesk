using System;
using System.Collections.Generic;
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
        // 初期状態でのバリデーションは無効化（警告文を表示しないため）
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
        
        // 編集時も初期バリデーションは無効化
    }
    
    [RelayCommand]
    private async Task SaveAsync()
    {
        // 保存時にのみバリデーションを実行し、警告を表示
        ValidateAllProperties();
        
        // 必須フィールドが空の場合は警告を表示して保存を停止
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Furigana))
        {
            IsValid = false;
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Name))
            {
                errors.Add("患者名を入力してください。");
            }
            if (string.IsNullOrWhiteSpace(Furigana))
            {
                errors.Add("フリガナを入力してください。");
            }
            ValidationErrors = string.Join("\n", errors);
            return;
        }
        
        // バリデーション成功時は警告を非表示
        IsValid = true;
        ValidationErrors = string.Empty;
        
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
            // 保存失敗時も警告は表示しない
        }
        catch (Exception ex)
        {
            // エラー時も警告は表示しない
            System.Diagnostics.Debug.WriteLine($"Error saving patient: {ex.Message}");
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
        // 入力変更時のバリデーションは無効化（警告文を表示しないため）
    }
    
    partial void OnNameChanged(string value)
    {
        // 入力変更時に警告が表示されている場合、入力があれば警告を非表示にする
        if (!IsValid && !string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(Furigana))
        {
            IsValid = true;
            ValidationErrors = string.Empty;
        }
    }
    
    partial void OnFuriganaChanged(string value)
    {
        // 入力変更時に警告が表示されている場合、入力があれば警告を非表示にする
        if (!IsValid && !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(value))
        {
            IsValid = true;
            ValidationErrors = string.Empty;
        }
    }
    
    private void UpdateValidationState()
    {
        // 警告メッセージの表示を無効化
        IsValid = true;
        ValidationErrors = string.Empty;
    }
} 
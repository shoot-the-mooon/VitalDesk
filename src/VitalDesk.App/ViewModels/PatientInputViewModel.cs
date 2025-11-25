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
    
    // 循環更新を防ぐフラグ
    private bool _isUpdatingBirthDateFromComponents = false;
    private bool _isUpdatingAdmissionDateFromComponents = false;
    
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
    
    // 生年月日用の補助プロパティ（ComboBox用）
    [ObservableProperty]
    private int? _birthYear;
    
    [ObservableProperty]
    private int? _birthMonth;
    
    [ObservableProperty]
    private int? _birthDay;
    
    // 入院日用の補助プロパティ（ComboBox用）
    [ObservableProperty]
    private int? _admissionYear;
    
    [ObservableProperty]
    private int? _admissionMonth;
    
    [ObservableProperty]
    private int? _admissionDay;
    
    // 選択肢のリスト（年：現在年から1900年まで降順、月：1-12、日：1-31固定）
    public List<int> BirthYears { get; } = Enumerable.Range(1900, DateTime.Now.Year - 1900 + 1).Reverse().ToList();
    public List<int> BirthMonths { get; } = Enumerable.Range(1, 12).ToList();
    public List<int> BirthDays { get; } = Enumerable.Range(1, 31).ToList();
    
    public List<int> AdmissionYears { get; } = Enumerable.Range(1900, DateTime.Now.Year - 1900 + 2).Reverse().ToList();
    public List<int> AdmissionMonths { get; } = Enumerable.Range(1, 12).ToList();
    public List<int> AdmissionDays { get; } = Enumerable.Range(1, 31).ToList();
    
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
        // 補助プロパティの初期化はOnBirthDateChanged/OnAdmissionChangedで自動的に行われる
    }
    
    [RelayCommand]
    private async Task SaveAsync()
    {
        // 保存時にのみバリデーションを実行し、警告を表示
        ValidateAllProperties();
        
        var errors = new List<string>();
        
        // 必須フィールドが空の場合は警告を表示して保存を停止
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("患者名を入力してください。");
        }
        if (string.IsNullOrWhiteSpace(Furigana))
        {
            errors.Add("フリガナを入力してください。");
        }
        
        // 生年月日の有効性チェック（年・月・日が全て選択されている場合のみ）
        var birthDateError = GetDateValidationError(BirthYear, BirthMonth, BirthDay, "生年月日");
        if (birthDateError != null)
        {
            errors.Add(birthDateError);
        }
        
        // 入院日の有効性チェック（年・月・日が全て選択されている場合のみ）
        var admissionDateError = GetDateValidationError(AdmissionYear, AdmissionMonth, AdmissionDay, "入院日");
        if (admissionDateError != null)
        {
            errors.Add(admissionDateError);
        }
        
        // エラーがある場合は警告を表示して保存を停止
        if (errors.Count > 0)
        {
            IsValid = false;
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
    
    // BirthDate変更時に年・月・日を分解して補助プロパティに設定
    partial void OnBirthDateChanged(DateTimeOffset? value)
    {
        if (_isUpdatingBirthDateFromComponents) return;
        
        _isUpdatingBirthDateFromComponents = true;
        try
        {
            if (value.HasValue)
            {
                BirthYear = value.Value.Year;
                BirthMonth = value.Value.Month;
                BirthDay = value.Value.Day;
            }
            else
            {
                BirthYear = null;
                BirthMonth = null;
                BirthDay = null;
            }
        }
        finally
        {
            _isUpdatingBirthDateFromComponents = false;
        }
    }
    
    // Admission変更時に年・月・日を分解して補助プロパティに設定
    partial void OnAdmissionChanged(DateTimeOffset? value)
    {
        if (_isUpdatingAdmissionDateFromComponents) return;
        
        _isUpdatingAdmissionDateFromComponents = true;
        try
        {
            if (value.HasValue)
            {
                AdmissionYear = value.Value.Year;
                AdmissionMonth = value.Value.Month;
                AdmissionDay = value.Value.Day;
            }
            else
            {
                AdmissionYear = null;
                AdmissionMonth = null;
                AdmissionDay = null;
            }
        }
        finally
        {
            _isUpdatingAdmissionDateFromComponents = false;
        }
    }
    
    // 生年月日の年・月・日変更時にBirthDateを合成
    partial void OnBirthYearChanged(int? value)
    {
        if (!_isUpdatingBirthDateFromComponents) UpdateBirthDate();
    }
    
    partial void OnBirthMonthChanged(int? value)
    {
        if (!_isUpdatingBirthDateFromComponents) UpdateBirthDate();
    }
    
    partial void OnBirthDayChanged(int? value)
    {
        if (!_isUpdatingBirthDateFromComponents) UpdateBirthDate();
    }
    
    // 入院日の年・月・日変更時にAdmissionを合成
    partial void OnAdmissionYearChanged(int? value)
    {
        if (!_isUpdatingAdmissionDateFromComponents) UpdateAdmissionDate();
    }
    
    partial void OnAdmissionMonthChanged(int? value)
    {
        if (!_isUpdatingAdmissionDateFromComponents) UpdateAdmissionDate();
    }
    
    partial void OnAdmissionDayChanged(int? value)
    {
        if (!_isUpdatingAdmissionDateFromComponents) UpdateAdmissionDate();
    }
    
    // 生年月日を年・月・日から合成
    private void UpdateBirthDate()
    {
        _isUpdatingBirthDateFromComponents = true;
        try
        {
            if (BirthYear.HasValue && BirthMonth.HasValue && BirthDay.HasValue)
            {
                try
                {
                    BirthDate = new DateTimeOffset(BirthYear.Value, BirthMonth.Value, BirthDay.Value, 0, 0, 0, TimeSpan.Zero);
                }
                catch (ArgumentOutOfRangeException)
                {
                    BirthDate = null;
                }
            }
            else
            {
                BirthDate = null;
            }
        }
        finally
        {
            _isUpdatingBirthDateFromComponents = false;
        }
    }
    
    // 入院日を年・月・日から合成
    private void UpdateAdmissionDate()
    {
        _isUpdatingAdmissionDateFromComponents = true;
        try
        {
            if (AdmissionYear.HasValue && AdmissionMonth.HasValue && AdmissionDay.HasValue)
            {
                try
                {
                    Admission = new DateTimeOffset(AdmissionYear.Value, AdmissionMonth.Value, AdmissionDay.Value, 0, 0, 0, TimeSpan.Zero);
                }
                catch (ArgumentOutOfRangeException)
                {
                    Admission = null;
                }
            }
            else
            {
                Admission = null;
            }
        }
        finally
        {
            _isUpdatingAdmissionDateFromComponents = false;
        }
    }
    
    // 年・月・日の組み合わせが有効な日付かチェック
    private string? GetDateValidationError(int? year, int? month, int? day, string fieldName)
    {
        // 全て未選択の場合はOK（任意項目のため）
        if (!year.HasValue && !month.HasValue && !day.HasValue)
            return null;
        
        // 一部だけ選択されている場合もOK（未完成状態として許可）
        if (!year.HasValue || !month.HasValue || !day.HasValue)
            return null;
        
        try
        {
            // その月の最大日数を取得（うるう年も考慮）
            int daysInMonth = DateTime.DaysInMonth(year.Value, month.Value);
            
            // 選択された日がその月の範囲内かチェック
            if (day.Value < 1 || day.Value > daysInMonth)
            {
                return $"{fieldName}が無効です。{year}年{month}月は{daysInMonth}日までです。";
            }
            
            // DateTimeとして作成できるか確認
            var testDate = new DateTime(year.Value, month.Value, day.Value);
            return null; // 有効な日付
        }
        catch (ArgumentOutOfRangeException)
        {
            return $"{fieldName}が無効です。正しい日付を選択してください。";
        }
    }
} 
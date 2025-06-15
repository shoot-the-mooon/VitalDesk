using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VitalDesk.Core.Models;
using VitalDesk.Core.Repositories;

namespace VitalDesk.App.ViewModels;

public partial class VitalInputViewModel : ObservableValidator
{
    private readonly IVitalRepository _vitalRepository;
    private readonly int _patientId;
    
    [ObservableProperty]
    [Required(ErrorMessage = "体温は必須です")]
    [Range(30.0, 45.0, ErrorMessage = "体温は30.0～45.0°Cの範囲で入力してください")]
    private double? _temperature = 36.5; // デフォルト値: 平熱
    
    [ObservableProperty]
    [Range(30, 200, ErrorMessage = "脈拍は30～200bpmの範囲で入力してください")]
    private int? _pulse = 72; // デフォルト値: 正常脈拍
    
    [ObservableProperty]
    [Range(50, 250, ErrorMessage = "収縮期血圧は50～250mmHgの範囲で入力してください")]
    private int? _systolic = 120; // デフォルト値: 正常収縮期血圧
    
    [ObservableProperty]
    [Range(30, 150, ErrorMessage = "拡張期血圧は30～150mmHgの範囲で入力してください")]
    private int? _diastolic = 80; // デフォルト値: 正常拡張期血圧
    
    [ObservableProperty]
    [Range(1.0, 300.0, ErrorMessage = "体重は1.0～300.0kgの範囲で入力してください")]
    private double? _weight = 60.0; // デフォルト値: 平均体重
    
    [ObservableProperty]
    private DateTimeOffset _measuredAt = DateTimeOffset.Now;
    
    // カレンダー用のDateTime?プロパティ
    public DateTime? MeasuredAtDate
    {
        get => MeasuredAt.DateTime;
        set 
        {
            if (value.HasValue)
            {
                MeasuredAt = new DateTimeOffset(value.Value);
                OnPropertyChanged();
            }
        }
    }
    
    partial void OnMeasuredAtChanged(DateTimeOffset value)
    {
        OnPropertyChanged(nameof(MeasuredAtDate));
        OnPropertyChanged(nameof(MeasuredAtFullWidthString));
    }
    
    // 全角文字での日付表示
    public string MeasuredAtFullWidthString
    {
        get
        {
            var date = MeasuredAt.DateTime;
            var dayOfWeek = date.ToString("dddd", new System.Globalization.CultureInfo("ja-JP"));
            
            // 半角数字を全角数字に変換
            var year = ToFullWidth(date.Year.ToString());
            var month = ToFullWidth(date.Month.ToString());
            var day = ToFullWidth(date.Day.ToString());
            
            return $"{year}年{month}月{day}日　（{dayOfWeek}）";
        }
    }
    
    // 半角数字を全角数字に変換するヘルパーメソッド
    private static string ToFullWidth(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
        return input.Replace('0', '０')
                   .Replace('1', '１')
                   .Replace('2', '２')
                   .Replace('3', '３')
                   .Replace('4', '４')
                   .Replace('5', '５')
                   .Replace('6', '６')
                   .Replace('7', '７')
                   .Replace('8', '８')
                   .Replace('9', '９');
    }
    
    [ObservableProperty]
    private bool _isSaving;
    
    [ObservableProperty]
    private bool _isEditMode;
    
    [ObservableProperty]
    private string _saveButtonText = "保存";
    
    private int? _editingVitalId;
    
    public event EventHandler<bool>? RequestClose;

    public VitalInputViewModel(int patientId)
    {
        _patientId = patientId;
        _vitalRepository = new VitalRepository();
    }
    
    public void SetEditMode(Vital vital)
    {
        IsEditMode = true;
        SaveButtonText = "更新";
        _editingVitalId = vital.Id;
        
        Temperature = vital.Temperature;
        Pulse = vital.Pulse;
        Systolic = vital.Systolic;
        Diastolic = vital.Diastolic;
        Weight = vital.Weight;
        MeasuredAt = new DateTimeOffset(vital.MeasuredAt);
    }
    
    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidateAllProperties();
        if (HasErrors) return;
        
        try
        {
            IsSaving = true;
            
            // 同じ日時のデータが既に存在するかチェック（編集モード以外）
            if (!IsEditMode)
            {
                var existingVital = await _vitalRepository.GetByPatientIdAndDateAsync(_patientId, MeasuredAt.DateTime);
                if (existingVital != null)
                {
                    // 既存データがある場合は編集モードに切り替え
                    SetEditMode(existingVital);
                    return;
                }
            }
            
            var vital = new Vital
            {
                Id = _editingVitalId ?? 0,
                PatientId = _patientId,
                Temperature = Temperature!.Value,
                Pulse = Pulse,
                Systolic = Systolic,
                Diastolic = Diastolic,
                Weight = Weight,
                MeasuredAt = MeasuredAt.DateTime
            };
            
            bool success;
            if (IsEditMode && _editingVitalId.HasValue)
            {
                success = await _vitalRepository.UpdateAsync(vital);
            }
            else
            {
                var newId = await _vitalRepository.CreateAsync(vital);
                success = newId > 0;
            }
            
            if (success)
            {
                RequestClose?.Invoke(this, true);
            }
            else
            {
                // TODO: Show error message
                System.Diagnostics.Debug.WriteLine("Failed to save vital data");
            }
        }
        catch (Exception ex)
        {
            // TODO: Show error message
            System.Diagnostics.Debug.WriteLine($"Error saving vital: {ex.Message}");
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
} 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using VitalDesk.Core.Models;
using VitalDesk.Core.Repositories;

namespace VitalDesk.App.ViewModels;

public enum PeriodFilter
{
    Week,
    Month,
    All
}

public partial class VitalChartsViewModel : ViewModelBase
{
    private readonly IVitalRepository _vitalRepository;
    private readonly Patient _patient;
    private List<Vital> _allVitals = new();
    private List<Vital> _currentVitals = new();
    
    [ObservableProperty]
    private ObservableCollection<ISeries> _combinedSeries = new();
    
    [ObservableProperty]
    private ObservableCollection<Axis> _combinedXAxes = new();
    
    [ObservableProperty]
    private ObservableCollection<Axis> _combinedYAxes = new();
    
    [ObservableProperty]
    private PeriodFilter _selectedPeriod = PeriodFilter.Week;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private string _noDataMessage = string.Empty;

    public VitalChartsViewModel(Patient patient)
    {
        _patient = patient;
        _vitalRepository = new VitalRepository();
        
        InitializeAxes();
        _ = LoadVitalDataAsync();
    }
    
    private void InitializeAxes()
    {
        // X軸（測定日時）- カスタムラベル軸
        var xAxis = new Axis
        {
            Name = "測定日時",
            NamePaint = new SolidColorPaint(SKColors.Black),
            LabelsPaint = new SolidColorPaint(SKColors.Gray),
            Labeler = index => GetDateLabel((int)index),
            SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
        };
        
        // 統一軸設定: 34°C=40bpm=0、40°C=160bpm=100の正規化スケール
        // 体温軸（左）- 正規化された値で表示
        var temperatureAxis = new Axis
        {
            Name = "体温 (°C)",
            NamePaint = new SolidColorPaint(SKColors.Red),
            LabelsPaint = new SolidColorPaint(SKColors.Red),
            Position = LiveChartsCore.Measure.AxisPosition.Start,
            MinLimit = 0,
            MaxLimit = 100,
            SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 },
            Labeler = value => (34 + (value / 100.0) * 6).ToString("F1") // 0-100 → 34-40°C
        };
        
        // 脈拍軸（右）- 正規化された値で表示  
        var pulseAxis = new Axis
        {
            Name = "脈拍 (bpm)",
            NamePaint = new SolidColorPaint(SKColors.Blue),
            LabelsPaint = new SolidColorPaint(SKColors.Blue),
            Position = LiveChartsCore.Measure.AxisPosition.End,
            MinLimit = 0,
            MaxLimit = 100,
            SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 },
            Labeler = value => (40 + (value / 100.0) * 120).ToString("F0") // 0-100 → 40-160bpm
        };
        
        CombinedXAxes.Add(xAxis);
        CombinedYAxes.Add(temperatureAxis);
        CombinedYAxes.Add(pulseAxis);
    }
    
    [RelayCommand]
    private async Task LoadVitalDataAsync()
    {
        try
        {
            IsLoading = true;
            _allVitals = (await _vitalRepository.GetByPatientIdAsync(_patient.Id)).ToList();
            
            System.Diagnostics.Debug.WriteLine($"Loaded {_allVitals.Count} vitals for patient {_patient.Id}");
            
            if (!_allVitals.Any())
            {
                NoDataMessage = "バイタルサインのデータがありません";
                ClearAllSeries();
                return;
            }
            
            NoDataMessage = string.Empty;
            UpdateCharts();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading vital data: {ex.Message}");
            NoDataMessage = "データの読み込みに失敗しました";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private void ChangePeriod(PeriodFilter period)
    {
        SelectedPeriod = period;
        UpdateCharts();
    }
    
    [RelayCommand]
    private void GenerateTestData()
    {
        // テスト用のダミーデータを生成
        _allVitals.Clear();
        var random = new Random();
        var baseDate = DateTime.Now.AddDays(-10);
        
        for (int i = 0; i < 10; i++)
        {
            _allVitals.Add(new Vital
            {
                Id = i + 1,
                PatientId = _patient.Id,
                MeasuredAt = baseDate.AddDays(i),
                Temperature = 36.0 + random.NextDouble() * 2.0, // 36.0-38.0
                Pulse = 60 + random.Next(40), // 60-100
                Systolic = 110 + random.Next(40), // 110-150
                Diastolic = 70 + random.Next(20), // 70-90
                Weight = 60.0 + random.NextDouble() * 20.0 // 60-80
            });
        }
        
        System.Diagnostics.Debug.WriteLine($"Generated {_allVitals.Count} test vitals");
        UpdateCharts();
    }
    
    private void UpdateCharts()
    {
        var filteredVitals = GetFilteredVitals();
        
        if (!filteredVitals.Any())
        {
            NoDataMessage = "選択された期間にデータがありません";
            ClearAllSeries();
            return;
        }
        
        NoDataMessage = string.Empty;
        
        UpdateCombinedChart(filteredVitals);
    }
    
    private List<Vital> GetFilteredVitals()
    {
        var cutoffDate = SelectedPeriod switch
        {
            PeriodFilter.Week => DateTime.Now.AddDays(-7),
            PeriodFilter.Month => DateTime.Now.AddDays(-30),
            PeriodFilter.All => DateTime.MinValue,
            _ => DateTime.Now.AddDays(-7)
        };
        
        return _allVitals
            .Where(v => v.MeasuredAt >= cutoffDate)
            .OrderBy(v => v.MeasuredAt)
            .ToList();
    }
    
    private void UpdateCombinedChart(List<Vital> vitals)
    {
        // 現在のバイタルデータを保存（ラベル生成用）
        _currentVitals = vitals;
        
        // 正規化されたデータポイントを作成（0-100スケール）
        var temperatureData = vitals
            .Select(v => new ObservableValue(v.Temperature > 0 ? NormalizeTemperature(v.Temperature) : double.NaN))
            .ToArray();
        
        var pulseData = vitals
            .Select(v => new ObservableValue(v.Pulse.HasValue ? NormalizePulse(v.Pulse.Value) : double.NaN))
            .ToArray();
        
        System.Diagnostics.Debug.WriteLine($"Total vitals: {vitals.Count}");
        System.Diagnostics.Debug.WriteLine($"Temperature data points: {temperatureData.Length}");
        System.Diagnostics.Debug.WriteLine($"Pulse data points: {pulseData.Length}");
        
        CombinedSeries.Clear();
        
        // 体温系列（左軸）- 常に追加
        var tempSeries = new LineSeries<ObservableValue>
        {
            Values = temperatureData,
            Name = "体温",
            Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 },
            Fill = null,
            GeometryStroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 },
            GeometryFill = new SolidColorPaint(SKColors.Red),
            GeometrySize = 6,
            ScalesYAt = 0 // 左軸（体温）
        };
        CombinedSeries.Add(tempSeries);
        System.Diagnostics.Debug.WriteLine("Added temperature series");
        
        // 脈拍系列（右軸）- 常に追加
        var pulseSeries = new LineSeries<ObservableValue>
        {
            Values = pulseData,
            Name = "脈拍",
            Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 2 },
            Fill = null,
            GeometryStroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 2 },
            GeometryFill = new SolidColorPaint(SKColors.Blue),
            GeometrySize = 6,
            ScalesYAt = 1 // 右軸（脈拍）
        };
        CombinedSeries.Add(pulseSeries);
        System.Diagnostics.Debug.WriteLine("Added pulse series");
        
        System.Diagnostics.Debug.WriteLine($"Total series count: {CombinedSeries.Count}");
    }
    
    private void ClearAllSeries()
    {
        CombinedSeries.Clear();
    }
    
    private static string SafeDateTimeLabeler(double value)
    {
        try
        {
            // 値が有効な範囲内かチェック
            var ticks = (long)value;
            if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
            {
                return "---"; // 無効な値の場合
            }
            
            var dateTime = new DateTime(ticks);
            
            // 年が有効な範囲内かチェック
            if (dateTime.Year < 1 || dateTime.Year > 9999)
            {
                return "---"; // 無効な年の場合
            }
            
            return dateTime.ToString("MM/dd");
        }
        catch (Exception)
        {
            return "---"; // 変換失敗時
        }
    }
    
    private string GetDateLabel(int index)
    {
        if (index < 0 || index >= _currentVitals.Count)
            return "";
            
        var vital = _currentVitals[index];
        return vital.MeasuredAt.ToString("MM/dd");
    }
    
    // 体温を0-100スケールに正規化（34°C=0, 40°C=100）
    private static double NormalizeTemperature(double temperature)
    {
        const double minTemp = 34.0;
        const double maxTemp = 40.0;
        return Math.Max(0, Math.Min(100, (temperature - minTemp) / (maxTemp - minTemp) * 100));
    }
    
    // 脈拍を0-100スケールに正規化（40bpm=0, 160bpm=100）
    private static double NormalizePulse(int pulse)
    {
        const double minPulse = 40.0;
        const double maxPulse = 160.0;
        return Math.Max(0, Math.Min(100, (pulse - minPulse) / (maxPulse - minPulse) * 100));
    }
} 
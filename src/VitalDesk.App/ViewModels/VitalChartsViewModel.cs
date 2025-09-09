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

public partial class VitalChartsViewModel : ViewModelBase
{   
    private static readonly SKTypeface JpTypeface = SKTypeface.FromFamilyName(
        OperatingSystem.IsWindows() ? "Yu Gothic UI" :
        OperatingSystem.IsMacOS()   ? "Hiragino Sans" :
                                      "Noto Sans CJK JP") 
        ?? SKTypeface.Default;
        
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
    private bool _isLoading;
    
    [ObservableProperty]
    private string _noDataMessage = string.Empty;
    
    [ObservableProperty]
    private int _currentWeekOffset = 0; // 0 = 今週、1 = 先週、-1 = 来週
    
    [ObservableProperty]
    private string _currentPeriodText = string.Empty;
    
    [ObservableProperty]
    private bool _canGoToPrevious = true;
    
    [ObservableProperty]
    private bool _canGoToNext = false;

    public VitalChartsViewModel(Patient patient)
    {
        _patient = patient;
        _vitalRepository = new VitalRepository();
        
        InitializeAxes();
        _ = LoadVitalDataAsync();
    }
    
    private void InitializeAxes()
    {
        // X軸（測定日）- 全てのデータポイントに日付を表示
        var xAxis = new Axis
        {
            Name = "測定日",
            NamePaint = new SolidColorPaint(SKColors.Black) { SKTypeface = JpTypeface },
            LabelsPaint = new SolidColorPaint(SKColors.Gray) { SKTypeface = JpTypeface },
            Labeler = index => GetDateLabel((int)index),
            MinStep = 1, // 各データポイントにラベルを表示
            UnitWidth = 1 // データポイント間の間隔を1に設定
        };
        
        // 統一軸設定: 34°C=40bpm=0、40°C=160bpm=100の正規化スケール
        // 体温軸（左）- 正規化された値で表示
        var temperatureAxis = new Axis
        {
            Name = "体温 (°C)",
            NamePaint = new SolidColorPaint(SKColors.Red) { SKTypeface = JpTypeface },
            LabelsPaint = new SolidColorPaint(SKColors.Red) { SKTypeface = JpTypeface },
            Position = LiveChartsCore.Measure.AxisPosition.Start,
            MinLimit = 0,
            MaxLimit = 100,
            Labeler = value => (30 + (value/100 ) * 15).ToString("F1"), // 0-100 → 30-45°C
            UnitWidth = 10, // グリッド線を適度に（10単位ごと）
            
        };
        
        // 脈拍軸（右）- 正規化された値で表示  
        var pulseAxis = new Axis
        {
            Name = "脈拍 (bpm)",
            NamePaint = new SolidColorPaint(SKColors.Blue) { SKTypeface = JpTypeface },
            LabelsPaint = new SolidColorPaint(SKColors.Blue) { SKTypeface = JpTypeface },
            Position = LiveChartsCore.Measure.AxisPosition.End,
            MinLimit = 0,
            MaxLimit = 100,
            Labeler = value => (40 + (value / 100.0) * 120).ToString("F0"), // 0-100 → 40-160bpm
            UnitWidth = 10, // グリッド線を適度に（10単位ごと）
            
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
    private void GoToPreviousWeek()
    {
        CurrentWeekOffset++;
        UpdateCharts();
        UpdateNavigationButtons();
    }
    
    [RelayCommand]
    private void GoToNextWeek()
    {
        CurrentWeekOffset--;
        UpdateCharts();
        UpdateNavigationButtons();
    }
    
    [RelayCommand]
    private void GenerateTestData()
    {
        // テスト用のダミーデータを生成
        _allVitals.Clear();
        var random = new Random();
        var baseDate = DateTime.Now.AddDays(-21); // 3週間前から開始
        
        for (int i = 0; i < 21; i++)
        {
            _allVitals.Add(new Vital
            {
                Id = i + 1,
                PatientId = _patient.Id,
                MeasuredAt = baseDate.AddDays(i).AddHours(random.Next(6, 22)),
                Temperature = 36.0 + random.NextDouble() * 2.0, // 36.0-38.0
                Pulse = 60 + random.Next(40), // 60-100
                Systolic = 110 + random.Next(40), // 110-150
                Diastolic = 70 + random.Next(20), // 70-90
                Weight = 60.0 + random.NextDouble() * 20.0 // 60-80
            });
        }
        
        System.Diagnostics.Debug.WriteLine($"Generated {_allVitals.Count} test vitals");
        CurrentWeekOffset = 0; // 今週に戻す
        UpdateCharts();
        UpdateNavigationButtons();
    }
    
    private void UpdateCharts()
    {
        var filteredVitals = GetCurrentWeekVitals();
        
        if (!filteredVitals.Any())
        {
            NoDataMessage = "この期間にデータがありません";
            ClearAllSeries();
            UpdatePeriodText();
            return;
        }
        
        NoDataMessage = string.Empty;
        UpdatePeriodText();
        UpdateCombinedChart(filteredVitals);
    }
    
    private List<Vital> GetCurrentWeekVitals()
    {
        var now = DateTime.Now;
        var startOfWeek = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday).Date; // 月曜日を週の開始とする
        
        // オフセットを適用
        var targetStartOfWeek = startOfWeek.AddDays(-7 * CurrentWeekOffset);
        var targetEndOfWeek = targetStartOfWeek.AddDays(7);
        
        return _allVitals
            .Where(v => v.MeasuredAt >= targetStartOfWeek && v.MeasuredAt < targetEndOfWeek)
            .OrderBy(v => v.MeasuredAt)
            .ToList();
    }
    
    private void UpdatePeriodText()
    {
        var now = DateTime.Now;
        var startOfWeek = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday).Date;
        var targetStartOfWeek = startOfWeek.AddDays(-7 * CurrentWeekOffset);
        var targetEndOfWeek = targetStartOfWeek.AddDays(6);
        
        if (CurrentWeekOffset == 0)
        {
            CurrentPeriodText = "今週";
        }
        else if (CurrentWeekOffset == 1)
        {
            CurrentPeriodText = "先週";
        }
        else if (CurrentWeekOffset > 1)
        {
            CurrentPeriodText = $"{CurrentWeekOffset}週間前";
        }
        else
        {
            CurrentPeriodText = $"{Math.Abs(CurrentWeekOffset)}週間後";
        }
        
        CurrentPeriodText += $" ({targetStartOfWeek:MM/dd} - {targetEndOfWeek:MM/dd})";
    }
    
    private void UpdateNavigationButtons()
    {
        // 未来の週には移動できない
        CanGoToNext = CurrentWeekOffset > 0;
        
        // 過去の週は常に移動可能（データがある限り）
        CanGoToPrevious = true;
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
        
        // 体温系列（左軸）- 直線で接続
        var tempSeries = new LineSeries<ObservableValue>
        {
            Values = temperatureData,
            Name = "Temperature",
            Stroke = new SolidColorPaint(SKColors.Red) { SKTypeface = JpTypeface, StrokeThickness = 2 },
            Fill = null,
            GeometryStroke = new SolidColorPaint(SKColors.Red) { SKTypeface = JpTypeface, StrokeThickness = 2 },
            GeometryFill = new SolidColorPaint(SKColors.Red) { SKTypeface = JpTypeface },
            GeometrySize = 6,
            ScalesYAt = 0, // 左軸（体温）
            LineSmoothness = 0 // 直線にする
        };
        CombinedSeries.Add(tempSeries);
        System.Diagnostics.Debug.WriteLine("Added temperature series");
        
        // 脈拍系列（右軸）- 直線で接続
        var pulseSeries = new LineSeries<ObservableValue>
        {
            Values = pulseData,
            Name = "Pulse",
            Stroke = new SolidColorPaint(SKColors.Blue) { SKTypeface = JpTypeface, StrokeThickness = 2 },
            Fill = null,
            GeometryStroke = new SolidColorPaint(SKColors.Blue) { SKTypeface = JpTypeface, StrokeThickness = 2 },
            GeometryFill = new SolidColorPaint(SKColors.Blue) { SKTypeface = JpTypeface },
            GeometrySize = 6,
            ScalesYAt = 1, // 右軸（脈拍）
            LineSmoothness = 0 // 直線にする
        };
        CombinedSeries.Add(pulseSeries);
        System.Diagnostics.Debug.WriteLine("Added pulse series");
        
        System.Diagnostics.Debug.WriteLine($"Total series count: {CombinedSeries.Count}");
    }
    
    private void ClearAllSeries()
    {
        CombinedSeries.Clear();
    }
    
    private string GetDateLabel(int index)
    {
        if (index < 0 || index >= _currentVitals.Count)
            return "";
            
        var vital = _currentVitals[index];
        return vital.MeasuredAt.ToString("MM/dd");
    }
    
    // 体温を0-100スケールに正規化（30°C=0, 45°C=100）
    private static double NormalizeTemperature(double temperature)
    {
        const double minTemp = 30.0;
        const double maxTemp = 45.0;
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using VitalDesk.Core.Models;
using VitalDesk.Core.Repositories;

namespace VitalDesk.App.ViewModels;

public partial class PatientDetailViewModel : ViewModelBase
{
    private readonly IVitalRepository _vitalRepository;
    
    [ObservableProperty]
    private Patient? _patient;
    
    [ObservableProperty]
    private ObservableCollection<Vital> _vitals = new();
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private string _selectedPeriod = "week";
    
    [ObservableProperty]
    private ISeries[] _temperatureSeries = Array.Empty<ISeries>();
    
    [ObservableProperty]
    private ISeries[] _pulseSeries = Array.Empty<ISeries>();
    
    [ObservableProperty]
    private Axis[] _xAxes = Array.Empty<Axis>();
    
    [ObservableProperty]
    private Axis[] _yAxes = Array.Empty<Axis>();
    
    public string[] PeriodOptions { get; } = { "week", "month", "all" };
    
    public PatientDetailViewModel()
    {
        _vitalRepository = new VitalRepository();
        InitializeChart();
    }
    
    public async Task LoadPatientAsync(Patient patient)
    {
        Patient = patient;
        await LoadVitalsAsync();
    }
    
    [RelayCommand]
    private async Task LoadVitalsAsync()
    {
        if (Patient == null) return;
        
        try
        {
            IsLoading = true;
            
            var (startDate, endDate) = GetDateRange();
            var vitals = await _vitalRepository.GetByPatientIdAndDateRangeAsync(
                Patient.Id, startDate, endDate);
            
            Vitals.Clear();
            foreach (var vital in vitals.OrderByDescending(v => v.MeasuredAt))
            {
                Vitals.Add(vital);
            }
            
            UpdateChart(vitals);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading vitals: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task AddVitalAsync()
    {
        // TODO: Open vital input dialog
        await Task.CompletedTask;
    }
    
    [RelayCommand]
    private async Task EditVitalAsync(Vital? vital)
    {
        if (vital == null) return;
        
        // TODO: Open vital input dialog for editing
        await Task.CompletedTask;
    }
    
    [RelayCommand]
    private async Task DeleteVitalAsync(Vital? vital)
    {
        if (vital == null) return;
        
        try
        {
            // TODO: Show confirmation dialog
            var success = await _vitalRepository.DeleteAsync(vital.Id);
            if (success)
            {
                Vitals.Remove(vital);
                await LoadVitalsAsync(); // Refresh chart
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting vital: {ex.Message}");
        }
    }
    
    partial void OnSelectedPeriodChanged(string value)
    {
        _ = LoadVitalsAsync();
    }
    
    private (DateTime startDate, DateTime endDate) GetDateRange()
    {
        var endDate = DateTime.Now;
        var startDate = SelectedPeriod switch
        {
            "week" => endDate.AddDays(-7),
            "month" => endDate.AddMonths(-1),
            "all" => DateTime.MinValue,
            _ => endDate.AddDays(-7)
        };
        
        return (startDate, endDate);
    }
    
    private void InitializeChart()
    {
        XAxes = new Axis[]
        {
            new Axis
            {
                Name = "Time",
                NamePaint = new SolidColorPaint(SKColors.Black),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 12
            }
        };
        
        YAxes = new Axis[]
        {
            new Axis
            {
                Name = "Temperature (°C) / Pulse (bpm)",
                NamePaint = new SolidColorPaint(SKColors.Black),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 12
            }
        };
    }
    
    private void UpdateChart(IEnumerable<Vital> vitals)
    {
        var vitalsList = vitals.OrderBy(v => v.MeasuredAt).ToList();
        
        if (!vitalsList.Any())
        {
            TemperatureSeries = Array.Empty<ISeries>();
            PulseSeries = Array.Empty<ISeries>();
            return;
        }
        
        // Temperature series
        var temperatureValues = vitalsList.Select((v, index) => new
        {
            X = index,
            Y = v.Temperature
        }).ToArray();
        
        // Pulse series
        var pulseValues = vitalsList.Where(v => v.Pulse.HasValue).Select((v, index) => new
        {
            X = vitalsList.IndexOf(v),
            Y = v.Pulse!.Value
        }).ToArray();
        
        TemperatureSeries = new ISeries[]
        {
            new LineSeries<dynamic>
            {
                Values = temperatureValues,
                Name = "Temperature",
                Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 },
                Fill = null,
                GeometryStroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColors.Red),
                GeometrySize = 4
            }
        };
        
        PulseSeries = new ISeries[]
        {
            new LineSeries<dynamic>
            {
                Values = pulseValues,
                Name = "Pulse",
                Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 2 },
                Fill = null,
                GeometryStroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColors.Blue),
                GeometrySize = 4
            }
        };
    }
} 
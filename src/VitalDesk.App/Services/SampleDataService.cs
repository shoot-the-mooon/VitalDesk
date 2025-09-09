using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VitalDesk.Core.Models;
using VitalDesk.Core.Repositories;
using VitalDesk.Core.Migrations;

namespace VitalDesk.App.Services;

public class SampleDataService
{
    private readonly IPatientRepository _patientRepository;
    private readonly IVitalRepository _vitalRepository;

    public SampleDataService()
    {
        _patientRepository = new PatientRepository();
        _vitalRepository = new VitalRepository();
    }
    
    public async Task<bool> HasSampleDataAsync()
    {
        var patients = await _patientRepository.GetAllAsync();
        return patients.Count() > 50; // Consider it has sample data if more than 50 patients
    }

    public async Task GenerateSamplePatientsAsync(int count = 100)
    {
        // まず既存のデータを全て削除
        await DatabaseInitializer.ClearAllDataAsync();
        
        var random = new Random();
        var firstNames = new[] { "太郎", "花子", "次郎", "美咲", "健太", "由美", "雄介", "恵子", "拓也", "智子", 
            "一郎", "真由美", "浩二", "久美子", "直樹", "裕子", "修平", "彩", "翔太", "麻衣" };
        var lastNames = new[] { "田中", "佐藤", "鈴木", "高橋", "渡辺", "伊藤", "山田", "中村", "小林", "加藤",
            "吉田", "山口", "斎藤", "松本", "井上", "木村", "林", "清水", "山本", "中野" };
        var insurerNames = new[] { 
            "東京都国民健康保険", "大阪市国民健康保険", "横浜市国民健康保険", "名古屋市国民健康保険", "福岡市国民健康保険",
            "神戸市国民健康保険", "札幌市国民健康保険", "京都市国民健康保険", "広島市国民健康保険", "仙台市国民健康保険" };

        for (int i = 1; i <= count; i++)
        {
            var lastName = lastNames[random.Next(lastNames.Length)];
            var firstName = firstNames[random.Next(firstNames.Length)];
            var name = $"{lastName} {firstName}";
            var furigana = GenerateFurigana(lastName, firstName);
            var insurerName = insurerNames[random.Next(insurerNames.Length)];
            
            // 国保番号は市区町村番号（6桁）+ 個人番号（4桁）
            var cityCode = random.Next(100000, 999999).ToString();
            var personalCode = random.Next(1000, 9999).ToString();
            var nationalHealthInsurance = $"{cityCode}{personalCode}";
            
            // 記号は2-4文字のアルファベット + 3-5桁の数字
            var symbolChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var symbolLength = random.Next(2, 5);
            var symbolLetters = new string(Enumerable.Range(0, symbolLength)
                .Select(_ => symbolChars[random.Next(symbolChars.Length)])
                .ToArray());
            var symbolNumbers = random.Next(100, 99999).ToString();
            var symbol = $"{symbolLetters}{symbolNumbers}";
            
            // 番号は6-8桁の数字
            var number = random.Next(100000, 99999999).ToString();

            var patient = new Patient
            {
                NationalHealthInsurance = nationalHealthInsurance,
                Symbol = symbol,
                Number = number,
                InsurerName = insurerName,
                Name = name,
                Furigana = furigana,
                BirthDate = DateTime.Now.AddYears(-random.Next(20, 90)).AddDays(-random.Next(0, 365)),
                FirstVisit = DateTime.Now.AddDays(-random.Next(30, 365)),
                Admission = DateTime.Now.AddDays(-random.Next(1, 30))
            };
            
            // 20%の確率で退院済み
            if (random.NextDouble() < 0.2)
            {
                patient.Discharge = patient.Admission?.AddDays(random.Next(1, 14));
            }
            
            var id = await _patientRepository.CreateAsync(patient);
            
            // バイタルサインを生成
            await GenerateVitalSignsForPatient(id, random);
        }
    }
    
    private string GenerateFurigana(string lastName, string firstName)
    {
        var lastNameFurigana = lastName switch
        {
            "田中" => "タナカ",
            "佐藤" => "サトウ",
            "鈴木" => "スズキ",
            "高橋" => "タカハシ",
            "渡辺" => "ワタナベ",
            "伊藤" => "イトウ",
            "山田" => "ヤマダ",
            "中村" => "ナカムラ",
            "小林" => "コバヤシ",
            "加藤" => "カトウ",
            "吉田" => "ヨシダ",
            "山口" => "ヤマグチ",
            "斎藤" => "サイトウ",
            "松本" => "マツモト",
            "井上" => "イノウエ",
            "木村" => "キムラ",
            "林" => "ハヤシ",
            "清水" => "シミズ",
            "山本" => "ヤマモト",
            "中野" => "ナカノ",
            _ => "タナカ"
        };
        
        var firstNameFurigana = firstName switch
        {
            "太郎" => "タロウ",
            "花子" => "ハナコ",
            "次郎" => "ジロウ",
            "美咲" => "ミサキ",
            "健太" => "ケンタ",
            "由美" => "ユミ",
            "雄介" => "ユウスケ",
            "恵子" => "ケイコ",
            "拓也" => "タクヤ",
            "智子" => "トモコ",
            "一郎" => "イチロウ",
            "真由美" => "マユミ",
            "浩二" => "コウジ",
            "久美子" => "クミコ",
            "直樹" => "ナオキ",
            "裕子" => "ユウコ",
            "修平" => "シュウヘイ",
            "彩" => "アヤ",
            "翔太" => "ショウタ",
            "麻衣" => "マイ",
            _ => "タロウ"
        };
        
        return $"{lastNameFurigana} {firstNameFurigana}";
    }

    private async Task GenerateVitalSignsForPatient(int patientId, Random random)
    {
        var daysBack = random.Next(1, 30);
        
        for (int day = 0; day < daysBack; day++)
        {
            var measurementDate = DateTime.Now.AddDays(-day);
            
            // 1日2-3回の測定
            var measurementsPerDay = random.Next(2, 4);
            
            for (int measurement = 0; measurement < measurementsPerDay; measurement++)
            {
                // 測定時間は6時、14時、20時付近
                var hour = measurement switch
                {
                    0 => random.Next(5, 8), // 朝
                    1 => random.Next(13, 15), // 昼
                    _ => random.Next(19, 22) // 夜
                };
                
                var vital = new Vital
                {
                    PatientId = patientId,
                    MeasuredAt = measurementDate.AddHours(hour).AddMinutes(random.Next(0, 60)),
                    Temperature = Math.Round(36.2 + random.NextDouble() * 2.0, 1), // 36.2-38.2°C
                    Pulse = random.Next(60, 100), // より現実的な範囲
                    Systolic = random.Next(90, 140), // より現実的な範囲
                    Diastolic = random.Next(60, 90), // より現実的な範囲
                    Weight = Math.Round(45.0 + random.NextDouble() * 45.0, 1) // 45-90kg
                };

                await _vitalRepository.CreateAsync(vital);
            }
        }
    }
} 
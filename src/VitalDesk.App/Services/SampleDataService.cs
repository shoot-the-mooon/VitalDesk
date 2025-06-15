using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VitalDesk.Core.Models;
using VitalDesk.Core.Repositories;

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

    public async Task GenerateSamplePatientsAsync(int count = 100)
    {
        var random = new Random();
        var firstNames = new[]
        {
            "太郎", "花子", "次郎", "美咲", "三郎", "さくら", "健一", "由美", "大輔", "恵子",
            "雄一", "真理", "和也", "智子", "博之", "裕子", "隆", "明美", "正", "久美子",
            "誠", "直美", "修", "洋子", "勇", "典子", "清", "幸子", "実", "文子",
            "進", "節子", "豊", "悦子", "茂", "和子", "稔", "敏子", "昇", "良子",
            "光", "春子", "武", "秋子", "勝", "冬子", "力", "夏子", "優", "桜子"
        };

        var lastNames = new[]
        {
            "田中", "佐藤", "鈴木", "高橋", "渡辺", "伊藤", "山本", "中村", "小林", "加藤",
            "吉田", "山田", "佐々木", "山口", "松本", "井上", "木村", "林", "清水", "山崎",
            "森", "池田", "橋本", "阿部", "石川", "斎藤", "前田", "藤田", "後藤", "小川",
            "岡田", "長谷川", "村上", "近藤", "石田", "原田", "中島", "金子", "藤井", "西村",
            "福田", "太田", "三浦", "藤原", "岡本", "松田", "中川", "中野", "原", "小野"
        };

        var departments = new[]
        {
            "内科", "外科", "小児科", "産婦人科", "整形外科", "皮膚科", "眼科", "耳鼻咽喉科",
            "精神科", "泌尿器科", "循環器科", "呼吸器科", "消化器科", "神経内科", "リハビリテーション科"
        };

        for (int i = 1; i <= count; i++)
        {
            var lastName = lastNames[random.Next(lastNames.Length)];
            var firstName = firstNames[random.Next(firstNames.Length)];
            var department = departments[random.Next(departments.Length)];
            
            var birthDate = DateTime.Now.AddYears(-random.Next(20, 90)).AddDays(-random.Next(0, 365));
            var firstVisit = DateTime.Now.AddDays(-random.Next(1, 365));
            
            DateTime? admission = null;
            DateTime? discharge = null;
            
            // 30%の確率で入院患者
            if (random.NextDouble() < 0.3)
            {
                admission = firstVisit.AddDays(random.Next(0, 30));
                
                // 入院患者の70%は退院済み
                if (random.NextDouble() < 0.7)
                {
                    discharge = admission.Value.AddDays(random.Next(1, 60));
                }
            }

            var patient = new Patient
            {
                Code = $"{department.Substring(0, 1)}{i:D4}",
                Name = $"{lastName} {firstName}",
                BirthDate = birthDate,
                InsuranceNo = $"{random.Next(10000000, 99999999)}",
                FirstVisit = firstVisit,
                Admission = admission,
                Discharge = discharge
            };

            try
            {
                var patientId = await _patientRepository.CreateAsync(patient);
                
                // 各患者に1-10個のバイタルサインデータを生成
                await GenerateVitalSignsForPatient(patientId, random.Next(1, 11));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating sample patient {i}: {ex.Message}");
            }
        }
    }

    private async Task GenerateVitalSignsForPatient(int patientId, int count)
    {
        var random = new Random();
        var baseDate = DateTime.Now.AddDays(-30);

        for (int i = 0; i < count; i++)
        {
            var measuredAt = baseDate.AddDays(random.Next(0, 30)).AddHours(random.Next(6, 22));
            
            var vital = new Vital
            {
                PatientId = patientId,
                MeasuredAt = measuredAt,
                Temperature = 36.0 + random.NextDouble() * 2.5, // 36.0-38.5°C
                Pulse = random.Next(60, 120), // 60-120 bpm
                Systolic = random.Next(100, 160), // 100-160 mmHg
                Diastolic = random.Next(60, 100), // 60-100 mmHg
                Weight = 45.0 + random.NextDouble() * 40.0 // 45-85 kg
            };

            try
            {
                await _vitalRepository.CreateAsync(vital);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating vital sign for patient {patientId}: {ex.Message}");
            }
        }
    }

    public async Task<bool> HasSampleDataAsync()
    {
        var patients = await _patientRepository.GetAllAsync();
        return patients.Count() >= 50; // 50人以上いればサンプルデータありと判定
    }
} 
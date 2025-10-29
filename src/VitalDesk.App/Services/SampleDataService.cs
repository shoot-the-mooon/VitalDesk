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
        return patients.Count() > 40; // 40人以上いればサンプルデータありと判定
    }

    /// <summary>
    /// 50人の患者と3ヶ月分のバイタルデータを生成
    /// </summary>
    public async Task GenerateSamplePatientsAsync(int patientCount = 50, int daysOfVitals = 90)
    {
        // データベースを初期化
        await DatabaseInitializer.InitializeAsync();
        
        // 既存のデータを全て削除
        await DatabaseInitializer.ClearAllDataAsync();
        
        var random = new Random();
        
        // 名前のサンプル
        var firstNames = new[] { 
            "太郎", "花子", "次郎", "美咲", "健太", "由美", "雄介", "恵子", "拓也", "智子",
            "一郎", "真由美", "浩二", "久美子", "直樹", "裕子", "修平", "彩", "翔太", "麻衣",
            "大輔", "明美", "隆", "愛", "誠", "里奈", "剛", "桜", "博", "美穂"
        };
        
        var lastNames = new[] { 
            "田中", "佐藤", "鈴木", "高橋", "渡辺", "伊藤", "山田", "中村", "小林", "加藤",
            "吉田", "山口", "斎藤", "松本", "井上", "木村", "林", "清水", "山本", "中野",
            "阿部", "橋本", "石川", "前田", "藤田", "後藤", "岡田", "長谷川", "村上", "近藤"
        };
        
        // 保険者名のサンプル
        var insurerNames = new[] { 
            "東京都国民健康保険", "大阪市国民健康保険", "横浜市国民健康保険", 
            "名古屋市国民健康保険", "福岡市国民健康保険", "神戸市国民健康保険", 
            "札幌市国民健康保険", "京都市国民健康保険", "広島市国民健康保険", 
            "仙台市国民健康保険", "川崎市国民健康保険", "さいたま市国民健康保険",
            "千葉市国民健康保険", "北九州市国民健康保険", "新潟市国民健康保険"
        };

        Console.WriteLine($"サンプルデータ生成開始: {patientCount}人の患者、{daysOfVitals}日分のバイタル");

        for (int i = 1; i <= patientCount; i++)
        {
            var lastName = lastNames[random.Next(lastNames.Length)];
            var firstName = firstNames[random.Next(firstNames.Length)];
            var name = $"{lastName} {firstName}";
            var furigana = GenerateFurigana(lastName, firstName);
            var insurerName = insurerNames[random.Next(insurerNames.Length)];
            
            // 国保番号: 6桁の市区町村コード + 4桁の個人番号
            var nationalHealthInsurance = $"{random.Next(100000, 999999)}{random.Next(1000, 9999)}";
            
            // 記号: 2-4文字のアルファベット + 3-5桁の数字
            var symbolChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var symbolLength = random.Next(2, 5);
            var symbolLetters = new string(Enumerable.Range(0, symbolLength)
                .Select(_ => symbolChars[random.Next(symbolChars.Length)])
                .ToArray());
            var symbolNumbers = random.Next(100, 99999).ToString();
            var symbol = $"{symbolLetters}{symbolNumbers}";
            
            // 番号: 6-8桁の数字（文字列）
            var number = random.Next(100000, 99999999).ToString();

            // 年齢は20-90歳
            var birthDate = DateTime.Now.AddYears(-random.Next(20, 91)).AddDays(-random.Next(0, 365));
            var firstVisit = DateTime.Now.AddDays(-random.Next(90, 730)); // 3ヶ月〜2年前
            var admission = DateTime.Now.AddDays(-random.Next(0, daysOfVitals)); // バイタル期間内に入院

            // ステータスの決定
            var statusRandom = random.NextDouble();
            string status;
            DateTime? discharge = null;
            
            if (statusRandom < 0.70) // 70%が入院中
            {
                status = PatientStatus.Admitted;
            }
            else if (statusRandom < 0.85) // 15%が退院
            {
                status = PatientStatus.Discharged;
                discharge = admission.AddDays(random.Next(1, daysOfVitals / 2));
            }
            else // 15%が転棟
            {
                status = PatientStatus.Transferred;
                discharge = admission.AddDays(random.Next(1, daysOfVitals / 2));
            }

            var patient = new Patient
            {
                NationalHealthInsurance = nationalHealthInsurance,
                Symbol = symbol,
                Number = number,
                InsurerName = insurerName,
                Name = name,
                Furigana = furigana,
                BirthDate = birthDate,
                FirstVisit = firstVisit,
                Admission = admission,
                Discharge = discharge,
                Status = status
            };
            
            var patientId = await _patientRepository.CreateAsync(patient);
            
            // バイタルサインを生成
            await GenerateVitalSignsForPatient(patientId, admission, daysOfVitals, random);
            
            if (i % 10 == 0)
            {
                Console.WriteLine($"  {i}/{patientCount}人の患者データを生成完了");
            }
        }
        
        Console.WriteLine($"サンプルデータ生成完了: {patientCount}人");
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
            "阿部" => "アベ",
            "橋本" => "ハシモト",
            "石川" => "イシカワ",
            "前田" => "マエダ",
            "藤田" => "フジタ",
            "後藤" => "ゴトウ",
            "岡田" => "オカダ",
            "長谷川" => "ハセガワ",
            "村上" => "ムラカミ",
            "近藤" => "コンドウ",
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
            "大輔" => "ダイスケ",
            "明美" => "アケミ",
            "隆" => "タカシ",
            "愛" => "アイ",
            "誠" => "マコト",
            "里奈" => "リナ",
            "剛" => "ツヨシ",
            "桜" => "サクラ",
            "博" => "ヒロシ",
            "美穂" => "ミホ",
            _ => "タロウ"
        };
        
        return $"{lastNameFurigana} {firstNameFurigana}";
    }

    /// <summary>
    /// 指定された患者に対して、指定日数分のバイタルサインを生成（1日1件）
    /// </summary>
    private async Task GenerateVitalSignsForPatient(int patientId, DateTime admissionDate, int days, Random random)
    {
        // 1日1件の測定
        for (int day = 0; day < days; day++)
        {
            var measurementDate = admissionDate.AddDays(day);
            
            // 今日より未来の日付はスキップ
            if (measurementDate > DateTime.Now)
                break;
            
            // 測定時間: 午前中（8-11時）にランダム
            var hour = random.Next(8, 12);
            var minute = random.Next(0, 60);
            var measuredAt = measurementDate.Date.AddHours(hour).AddMinutes(minute);
            
            // バイタルサインの生成（現実的な値）
            var vital = new Vital
            {
                PatientId = patientId,
                MeasuredAt = measuredAt,
                Temperature = Math.Round(35.8 + random.NextDouble() * 2.4, 1), // 35.8-38.2°C
                Pulse = random.Next(55, 105),        // 55-104 bpm
                Systolic = random.Next(95, 155),     // 95-154 mmHg
                Diastolic = random.Next(55, 95),     // 55-94 mmHg
                Weight = Math.Round(45.0 + random.NextDouble() * 50.0, 1), // 45.0-95.0 kg
                Breakfast = random.NextDouble() > 0.1 ? "○" : "×", // 90%の確率で○
                Lunch = random.NextDouble() > 0.1 ? "○" : "×",     // 90%の確率で○
                Dinner = random.NextDouble() > 0.1 ? "○" : "×",    // 90%の確率で○
                Sleep = random.Next(4, 11),           // 4-10時間
                BowelMovement = random.Next(0, 4),    // 0-3回
                Note = random.NextDouble() > 0.8 ? GenerateRandomNote(random) : null // 20%の確率で備考あり
            };

            await _vitalRepository.CreateAsync(vital);
        }
    }
    
    private string GenerateRandomNote(Random random)
    {
        var notes = new[]
        {
            "良好",
            "特に問題なし",
            "やや疲労感あり",
            "元気",
            "食欲良好",
            "安静にしている",
            "よく眠れた",
            "少し倦怠感あり"
        };
        return notes[random.Next(notes.Length)];
    }
}

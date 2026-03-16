using System.Diagnostics;
using System.IO;
using System.Text;
using GleemLet.Models;
using Newtonsoft.Json;

using GleemLet;
namespace GleemLet.Services;

public class DataService
{
    // !! KRİTİK: Static field'lar KAYNAK KOD SIRASIYLA init edilir.
    // Instance = new() constructor tetikler → Load() → AllBadges ve _http kullanılır.
    // Bu yüzden AllBadges ve _http, Instance'dan ÖNCE tanımlanmak ZORUNDA. !!

    public static readonly List<Badge> AllBadges =
    [
        new(){Id="first_word",        Name="First Step",        Icon="Sprout", Description="Add your first word"},
        new(){Id="ten_words",          Name="Word Collector",    Icon="BookOpenPageVariant", Description="Add 10 words"},
        new(){Id="fifty_words",        Name="Bookworm",          Icon="BookOpen", Description="Add 50 words"},
        new(){Id="hundred_words",      Name="Vocab Pro",         Icon="Trophy", Description="Add 100 words"},
        new(){Id="two_hundred_words",  Name="Vocabulary King",   Icon="Crown", Description="Add 200 words"},
        new(){Id="first_study",        Name="Student",           Icon="School", Description="Complete first study session"},
        new(){Id="ten_sessions",       Name="Dedicated",         Icon="Target", Description="Complete 10 study sessions"},
        new(){Id="fifty_sessions",     Name="Expert",            Icon="Medal", Description="Complete 50 study sessions"},
        new(){Id="streak_3",           Name="Consistent",        Icon="Fire", Description="3 day streak"},
        new(){Id="streak_7",           Name="On Fire",           Icon="Flash", Description="7 day streak"},
        new(){Id="streak_14",          Name="Iron Will",         Icon="Shield", Description="14 day streak"},
        new(){Id="streak_30",          Name="Legend",            Icon="MoonStar", Description="30 day streak"},
        new(){Id="perfect",            Name="Perfectionist",     Icon="Star", Description="100% accuracy in a session"},
        new(){Id="three_sets",         Name="Collector",         Icon="Folder", Description="Create 3 sets"},
        new(){Id="five_sets",          Name="Set Master",        Icon="FolderMultiple", Description="Create 5 sets"},
        new(){Id="all_learned",        Name="Mastered",          Icon="Shimmer", Description="100% progress on any set"},
        new(){Id="level_5",            Name="Rising Star",       Icon="StarFace", Description="Reach Level 5"},
        new(){Id="level_10",           Name="Scholar",           Icon="School", Description="Reach Level 10"},
        new(){Id="night_owl",          Name="Night Owl",         Icon="WeatherNight", Description="Study after 22:00"},
        new(){Id="speed_demon",        Name="Speed Demon",       Icon="Flash", Description="Finish timed mode under 60s"},
        new(){Id="multilingual",       Name="Multilingual",      Icon="Earth", Description="Switch to Turkish"},
    ];

    // Instance en sonda — _http ve AllBadges hazır olduktan sonra init edilir
    public static DataService Instance { get; } = new();

    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GleemLet", "data.json");

    public AppData Data { get; private set; } = new();

    private DataService()
    {
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                Data = JsonConvert.DeserializeObject<AppData>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GleemLet] Load failed: {ex.Message}");
            // Bozuk JSON dosyasını sil ki bir sonraki açılışta da crash vermesin
            try { File.Delete(_path); } catch { }
            Data = new();
        }

        // FIX: Newtonsoft, JSON'da eksik/null olan collection field'larını null bırakabilir.
        // Eski data.json şemasından yükleme yapılırken bu crash'e yol açıyor.
        // Tüm collection'ları burada güvenli şekilde initialize ediyoruz.
        Data          ??= new();
        Data.Profile  ??= new();
        Data.Sets     ??= [];
        Data.Badges   ??= [];
        Data.Sessions ??= [];

        // Sets içindeki Words listelerini de kontrol et — eski JSON'dan gelen null field'lar için
        foreach (var set in Data.Sets)
        {
            set.Words ??= [];
            // Her kelimenin string alanlarını güvenli hale getir
            foreach (var w in set.Words)
            {
                w.En      ??= "";
                w.Tr      ??= "";
                w.Example ??= "";
                w.Level   ??= "";   // CEFR seviyesi — eski JSON'dan null gelebilir
                w.Id      ??= Guid.NewGuid().ToString();
            }
            // Migration: Ensure sample data or early words get levels if missing
            foreach (var w in set.Words)
            {
                if (string.IsNullOrEmpty(w.Level))
                {
                    // Random assignment for variety if it's sample data, or default to A1
                    if (set.Category == "Academic") w.Level = "B2";
                    else if (set.Category == "Business") w.Level = "B1";
                    else w.Level = "A1";
                }
            }
            set.Id          ??= Guid.NewGuid().ToString();
            set.Name        ??= "";
            set.Description ??= "";
            set.Category    ??= "General";
        }

        foreach (var b in AllBadges)
            if (!Data.Badges.Any(x => x.Id == b.Id))
            {
                var localName = L.BadgeName(b.Id);
                var localDesc = L.BadgeDesc(b.Id);
                Data.Badges.Add(new()
                {
                    Id          = b.Id,
                    Name        = b.Name,
                    Icon        = b.Icon,
                    Description = b.Description,
                    LocalizedName = localName,
                    LocalizedDescription = localDesc,
                });
            }

        if (Data.Sets.Count == 0) AddSampleData();

        // Dil tercihini yükle
        L.Lang = (Data.Profile.Language ?? "en") == "tr" ? AppLanguage.Turkish : AppLanguage.English;

        Save();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonConvert.SerializeObject(Data, Formatting.Indented));
        }
        catch (Exception ex)
        {
            // FIX: Sessiz catch yerine loglama
            Debug.WriteLine($"[GleemLet] Save failed: {ex.Message}");
        }
    }

    public void AddXP(int amount)
    {
        Data.Profile.XP    += amount;
        Data.Profile.Level  = Data.Profile.XP / 200 + 1;
        Save();
    }

    public void UpdateStreak()
    {
        var today = DateTime.Today;
        if (Data.Profile.LastStudyDate?.Date == today) return;
        Data.Profile.Streak = Data.Profile.LastStudyDate?.Date == today.AddDays(-1)
            ? Data.Profile.Streak + 1
            : 1;
        Data.Profile.LastStudyDate = today;
        Save();
    }

    public List<string> CheckAndAwardBadges()
    {
        var earned     = new List<string>();
        int totalWords = Data.Sets.Sum(s => s.Words.Count);

        void Award(string id)
        {
            var b = Data.Badges.FirstOrDefault(x => x.Id == id);
            if (b != null && !b.Earned)
            {
                b.Earned    = true;
                b.EarnedDate = DateTime.Now;
                earned.Add($"{b.Icon} {b.Name}");
            }
        }

        if (totalWords >= 1)   Award("first_word");
        if (totalWords >= 10)  Award("ten_words");
        if (totalWords >= 50)  Award("fifty_words");
        if (totalWords >= 100) Award("hundred_words");
        if (totalWords >= 200) Award("two_hundred_words");
        if (Data.Profile.TotalStudySessions >= 1)  Award("first_study");
        if (Data.Profile.TotalStudySessions >= 10) Award("ten_sessions");
        if (Data.Profile.TotalStudySessions >= 50) Award("fifty_sessions");
        if (Data.Profile.Streak >= 3)  Award("streak_3");
        if (Data.Profile.Streak >= 7)  Award("streak_7");
        if (Data.Profile.Streak >= 14) Award("streak_14");
        if (Data.Profile.Streak >= 30) Award("streak_30");
        if (Data.Sets.Count >= 3) Award("three_sets");
        if (Data.Sets.Count >= 5) Award("five_sets");
        if (Data.Profile.Level >= 5)   Award("level_5");
        if (Data.Profile.Level >= 10)  Award("level_10");
        if (L.Lang == AppLanguage.Turkish) Award("multilingual");
        // Night owl: any session recorded after 22:00
        if (Data.Sessions.Any(s => s.Date.Hour >= 22)) Award("night_owl");
        // Speed demon: timed session finished under 60 seconds
        if (Data.Sessions.Any(s => s.Mode == "timed" && s.DurationSeconds > 0 && s.DurationSeconds < 60)) Award("speed_demon");
        // All learned: any set with 100% progress
        if (Data.Sets.Any(s => s.Words.Count > 0 && s.Words.All(w => w.Learned))) Award("all_learned");

        // FIX: "perfect" badge artık burada — inline set edilmiyordu, kullanıcıya gösterilmiyordu
        if (Data.Sessions.Any(s => s.Total > 0 && s.Correct == s.Total))
            Award("perfect");

        if (earned.Count > 0) Save();
        return earned;
    }

    // FIX: RecordSession artık kazanılan badge listesini döndürüyor.
    // MainWindow'un ayrıca CheckAndAwardBadges() çağırmasına gerek kalmadı.
    public List<string> RecordSession(StudySession s)
    {
        Data.Sessions.Add(s);
        Data.Profile.TotalStudySessions++;
        Data.Profile.TotalStudySeconds += s.DurationSeconds;

        var set = Data.Sets.FirstOrDefault(x => x.Id == s.SetId);
        if (set != null)
        {
            set.StudyCount++;
            set.LastStudied = DateTime.Now;
        }

        AddXP(s.Correct * 10 + (s.Total > 0 && s.Correct == s.Total ? 50 : 0));
        UpdateStreak();

        // FIX: Tek çağrı — badge listesi caller'a dönüyor
        return CheckAndAwardBadges();
    }

    private void AddSampleData()
    {
        Data.Sets.Add(new()
        {
            Name = "IELTS Essentials", Description = "Common IELTS vocabulary", Category = "Academic",
            Words =
            [
                // FIX: Tr alanları artık gerçek Türkçe karşılıklar ve Seviyeler eklendi
                new(){En="ephemeral",  Tr="kısa ömürlü",      Example="Fame is ephemeral.", Level="C1"},
                new(){En="resilient",  Tr="dirençli",          Example="A resilient leader.", Level="B2"},
                new(){En="eloquent",   Tr="belagatli",         Example="An eloquent speech.", Level="B1"},
                new(){En="ubiquitous", Tr="her yerde bulunan", Example="Smartphones are ubiquitous.", Level="C2"},
                new(){En="ambiguous",  Tr="belirsiz",          Example="An ambiguous statement.", Level="B2"},
                new(){En="meticulous", Tr="titiz",             Example="Meticulous planning.", Level="C1"},
                new(){En="pragmatic",  Tr="pratik",            Example="A pragmatic approach.", Level="B1"},
                new(){En="tenacious",  Tr="azimli",            Example="A tenacious athlete.", Level="C1"},
            ]
        });
        Data.Sets.Add(new()
        {
            Name = "Business English", Description = "Professional vocabulary", Category = "Business",
            Words =
            [
                new(){En="negotiate",   Tr="müzakere etmek", Example="We need to negotiate.", Level="B2"},
                new(){En="stakeholder", Tr="paydaş",         Example="All stakeholders agree.", Level="C1"},
                new(){En="leverage",    Tr="avantaj sağlamak", Example="Leverage your strengths.", Level="B2"},
                new(){En="synergy",     Tr="sinerji",         Example="Team synergy matters.", Level="B1"},
            ]
        });
    }
}

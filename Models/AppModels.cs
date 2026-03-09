using GleemLet;
namespace GleemLet.Models;

// Newtonsoft.Json null-safe deserialization için
using Newtonsoft.Json;

// FIX: Magic string'ler yerine güvenli enum
public enum StudyMode
{
    Flashcard,
    Learn,
    Test,
    Timed
}

public static class StudyModeExtensions
{
    public static string ToIcon(this StudyMode mode) => mode switch
    {
        StudyMode.Flashcard => "📇",
        StudyMode.Learn     => "💡",
        StudyMode.Test      => "📝",
        StudyMode.Timed     => "⏰",
        _                   => "📖"
    };

    public static string ToLabel(this StudyMode mode)
    {
        bool tr = L.Lang == AppLanguage.Turkish;
        return mode switch
        {
            StudyMode.Flashcard => tr ? "📇 Kartlar"   : "📇 Flashcards",
            StudyMode.Learn     => tr ? "💡 Öğren"     : "💡 Learn",
            StudyMode.Test      => tr ? "📝 Test"       : "📝 Test",
            StudyMode.Timed     => tr ? "⏰ Zamanlı"   : "⏰ Timed",
            _                   => mode.ToString()
        };
    }

    public static string ToSerializedString(this StudyMode mode) => mode switch
    {
        StudyMode.Flashcard => "flashcard",
        StudyMode.Learn     => "learn",
        StudyMode.Test      => "test",
        StudyMode.Timed     => "timed",
        _                   => "flashcard"
    };

    public static StudyMode FromString(string s) => s switch
    {
        "flashcard" => StudyMode.Flashcard,
        "learn"     => StudyMode.Learn,
        "test"      => StudyMode.Test,
        "timed"     => StudyMode.Timed,
        _           => StudyMode.Flashcard
    };
}

public class Flashcard
{
    public string Id           { get; set; } = Guid.NewGuid().ToString();
    public string En           { get; set; } = "";
    public string Tr           { get; set; } = "";
    public string Example      { get; set; } = "";
    public string Level        { get; set; } = "";  // CEFR: A1 A2 B1 B2 C1 C2
    public bool   Learned      { get; set; }
    public int    CorrectCount { get; set; }
    public int    WrongCount   { get; set; }
    public DateTime? LastStudied { get; set; }
}

public class FlashcardSet
{
    public string Id          { get; set; } = Guid.NewGuid().ToString();
    public string Name        { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category    { get; set; } = "General";
    public List<Flashcard> Words { get; set; } = [];
    public DateTime  Created     { get; set; } = DateTime.Now;
    public DateTime? LastStudied { get; set; }
    public int  StudyCount  { get; set; }
    public bool IsFavorite  { get; set; }
    public int    LearnedCount => Words.Count(w => w.Learned);
    public double Progress     => Words.Count > 0 ? (double)LearnedCount / Words.Count * 100 : 0;
}

public class Badge
{
    public string Id          { get; set; } = "";
    public string Name        { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon        { get; set; } = "";
    public bool   Earned      { get; set; }
    public DateTime? EarnedDate { get; set; }
    // Localization helpers — not persisted to JSON
    [Newtonsoft.Json.JsonIgnore] public string LocalizedName        { get; set; } = "";
    [Newtonsoft.Json.JsonIgnore] public string LocalizedDescription { get; set; } = "";
    public string DisplayName => !string.IsNullOrEmpty(LocalizedName)        ? LocalizedName        : Name;
    public string DisplayDesc => !string.IsNullOrEmpty(LocalizedDescription) ? LocalizedDescription : Description;
}

public class StudySession
{
    public string Id       { get; set; } = Guid.NewGuid().ToString();
    public string SetId    { get; set; } = "";
    public string SetName  { get; set; } = "";
    public string Mode     { get; set; } = "";  // serialized string, use StudyModeExtensions
    public int    Correct  { get; set; }
    public int    Wrong    { get; set; }
    public int    Total    { get; set; }
    public DateTime Date   { get; set; } = DateTime.Now;
    public int DurationSeconds { get; set; }
    public double Accuracy => Total > 0 ? (double)Correct / Total * 100 : 0;
}

public class UserProfile
{
    public string Name { get; set; } = "Learner";
    public string Goal { get; set; } = "Learn English vocabulary";
    public int    XP     { get; set; }
    public int    Level  { get; set; } = 1;
    public int    Streak { get; set; }
    public DateTime? LastStudyDate    { get; set; }
    public int TotalStudySessions     { get; set; }
    public int TotalStudySeconds      { get; set; }
    public bool SoundEnabled          { get; set; } = true;
    public bool MusicEnabled          { get; set; } = true;
    public bool ShuffleDefault        { get; set; }

    // Dil tercihi — "en" veya "tr"
    public string Language { get; set; } = "en";

    // Tema — "dark" | "light"
    public string Theme { get; set; } = "dark";
    // Aksent renk — "teal" | "purple" | "blue" | "orange" | "pink" | "red"
    public string AccentColor { get; set; } = "teal";
    // Avatar emoji
    public string Avatar { get; set; } = "🎓";
    // Günlük kelime hedefi
    public int DailyGoalWords { get; set; } = 10;
}


public class AppData
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public UserProfile Profile { get; set; } = new();

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<FlashcardSet> Sets { get; set; } = [];

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<Badge> Badges { get; set; } = [];

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<StudySession> Sessions { get; set; } = [];
}

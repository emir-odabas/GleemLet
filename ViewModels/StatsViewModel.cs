using CommunityToolkit.Mvvm.ComponentModel;
using GleemLet.Models;
using GleemLet.Services;

namespace GleemLet.ViewModels;

public partial class StatsViewModel : BaseViewModel
{
    private readonly DataService _ds = DataService.Instance;

    [ObservableProperty] private int    _streak;
    [ObservableProperty] private int    _level;
    [ObservableProperty] private int    _totalXp;
    [ObservableProperty] private int    _totalSessions;
    [ObservableProperty] private double _avgAccuracy;
    [ObservableProperty] private int    _totalStudyTime;

    public string StudyTimeFormatted => TotalStudyTime < 60 ? $"{TotalStudyTime}s" : $"{TotalStudyTime / 60}m";

    public List<FlashcardSet>   SetsProgress    { get; private set; } = [];
    public List<StudySession>   RecentSessions  { get; private set; } = [];

    // Localized Strings
    [ObservableProperty] private string _statsTitle = "";
    [ObservableProperty] private string _streakLabel = "";
    [ObservableProperty] private string _levelLabel = "";
    [ObservableProperty] private string _totalXpLabel = "";
    [ObservableProperty] private string _sessionsLabel = "";
    [ObservableProperty] private string _accuracyLabel = "";
    [ObservableProperty] private string _timeLabel = "";
    [ObservableProperty] private string _progressPerSetTitle = "";
    [ObservableProperty] private string _recentSessionsTitle = "";

    public StatsViewModel()
    {
        Title = "Statistics";
        Load();
    }

    public void Load()
    {
        var p = _ds.Data.Profile;

        Streak         = p.Streak;
        Level          = p.Level;
        TotalXp        = p.XP;
        TotalSessions  = p.TotalStudySessions;
        TotalStudyTime = p.TotalStudySeconds;
        AvgAccuracy    = _ds.Data.Sessions.Count > 0
            ? _ds.Data.Sessions.Average(s => s.Accuracy)
            : 0;

        // Localized Strings
        StatsTitle           = L.Statistics;
        StreakLabel          = L.DayStreak;
        LevelLabel           = L.LevelLabel;
        TotalXpLabel         = L.TotalXP;
        SessionsLabel        = L.Sessions;
        AccuracyLabel        = L.AvgAccuracy;
        TimeLabel            = L.StudyTime;
        ProgressPerSetTitle  = L.ProgressPerSet;
        RecentSessionsTitle  = L.RecentSessions;

        SetsProgress   = _ds.Data.Sets.OrderByDescending(s => s.Progress).ToList();
        RecentSessions = _ds.Data.Sessions.OrderByDescending(s => s.Date).Take(10).ToList();

        OnPropertyChanged(nameof(SetsProgress));
        OnPropertyChanged(nameof(RecentSessions));
    }
}

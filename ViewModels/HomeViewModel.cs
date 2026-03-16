using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GleemLet.Models;
using GleemLet.Services;

namespace GleemLet.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly DataService _ds = DataService.Instance;

    // ── Observable Properties ──────────────────────────────
    [ObservableProperty] private string _welcomeMessage = "";
    [ObservableProperty] private string _goal = "";
    [ObservableProperty] private int    _streak;
    [ObservableProperty] private int    _totalWords;
    [ObservableProperty] private int    _totalLearned;
    [ObservableProperty] private int    _totalSessions;
    [ObservableProperty] private int    _level;
    [ObservableProperty] private int    _xp;
    [ObservableProperty] private int    _dailyGoalWords;
    [ObservableProperty] private int    _todayLearned;
    [ObservableProperty] private double _dailyGoalPercent;
    [ObservableProperty] private double _xpPercent;

    // Localized Strings
    [ObservableProperty] private string _recentSetsTitle = "";
    [ObservableProperty] private string _viewAllLabel = "";
    [ObservableProperty] private string _recentActivityTitle = "";
    [ObservableProperty] private string _wordsLabel = "";
    [ObservableProperty] private string _learnedLabel = "";
    [ObservableProperty] private string _sessionsLabel = "";
    [ObservableProperty] private string _streakLabel = "";
    [ObservableProperty] private string _levelLabel = "";
    [ObservableProperty] private string _dailyGoalTitle = "";
    [ObservableProperty] private string _todayProgressText = "";

    // Listeler
    public List<FlashcardSet>   RecentSets     { get; private set; } = [];
    public List<StudySession>   RecentSessions { get; private set; } = [];

    public HomeViewModel()
    {
        Title = "Home";
        Load();

        NavigationService.Instance.DailyGoalChanged += () =>
        {
            Load();
        };
    }

    public void Load()
    {
        var p = _ds.Data.Profile;

        WelcomeMessage = L.WelcomeBack(p.Name);
        Goal           = p.Goal;
        Streak         = p.Streak;
        Level          = p.Level;
        Xp             = p.XP;
        XpPercent      = (p.XP % 200) / 200.0;
        TotalWords     = _ds.Data.Sets.Sum(s => s.Words.Count);
        TotalLearned   = _ds.Data.Sets.Sum(s => s.LearnedCount);
        TotalSessions  = p.TotalStudySessions;
        DailyGoalWords = p.DailyGoalWords;

        // Localized Strings
        RecentSetsTitle     = L.RecentSets;
        ViewAllLabel        = "View All"; // Or L.ViewAll if available
        RecentActivityTitle = L.RecentActivity;
        WordsLabel          = L.WordsLabel;
        LearnedLabel        = L.LearnedLabel;
        SessionsLabel       = L.SessionsLabel;
        StreakLabel         = L.StreakLabel;
        LevelLabel          = L.LevelLabel;
        DailyGoalTitle      = L.DailyGoalSection;
        
        TodayLearned = _ds.Data.Sets
            .SelectMany(s => s.Words)
            .Count(w => w.LastStudied.HasValue && w.LastStudied.Value.Date == DateTime.Today);

        TodayProgressText = L.TodayProgress(TodayLearned, DailyGoalWords);

        DailyGoalPercent = DailyGoalWords > 0
            ? Math.Min(1.0, (double)TodayLearned / DailyGoalWords)
            : 0;

        RecentSets     = _ds.Data.Sets.OrderByDescending(s => s.LastStudied ?? s.Created).Take(4).ToList();
        RecentSessions = _ds.Data.Sessions.OrderByDescending(s => s.Date).Take(5).ToList();

        OnPropertyChanged(nameof(RecentSets));
        OnPropertyChanged(nameof(RecentSessions));
    }

    // ── Commands ───────────────────────────────────────────
    [RelayCommand]
    private void GoToSets()
    {
        NavigationService.Instance.NavigateTo("sets");
    }

    [RelayCommand]
    private void StartFlashcards(FlashcardSet set)
    {
        NavigationService.Instance.RequestStudy(set.Id, StudyMode.Flashcard);
    }

    [RelayCommand]
    private void StartLearn(FlashcardSet set)
    {
        NavigationService.Instance.RequestStudy(set.Id, StudyMode.Learn);
    }

    [RelayCommand]
    private void StartTest(FlashcardSet set)
    {
        NavigationService.Instance.RequestStudy(set.Id, StudyMode.Test);
    }
}

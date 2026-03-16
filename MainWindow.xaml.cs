using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GleemLet.Models;
using GleemLet.Services;
using GleemLet.Windows;
using SoundSvc = GleemLet.Services.SoundService;

namespace GleemLet;

public partial class MainWindow : Window
{
    private readonly DataService _ds = DataService.Instance;

    public ViewModels.HomeViewModel    HomeVM    { get; } = new();
    public ViewModels.SetsViewModel    SetsVM    { get; } = new();
    public ViewModels.StatsViewModel   StatsVM   { get; } = new();
    public ViewModels.BadgesViewModel  BadgesVM  { get; } = new();
    public ViewModels.ProfileViewModel ProfileVM { get; } = new();

    private string _currentSetId = "";
    private StudyMode _studyMode = StudyMode.Flashcard;
    private List<Flashcard> _studyQueue = [];
    private int _studyIndex;
    private int _studyCorrect;
    private int _studyWrong;
    private bool _fcFlipped;
    private bool _answered;
    private DispatcherTimer? _timer;
    private int _timedSeconds;
    private DateTime _sessionStart;
    private string _currentPage = "home";

    public MainWindow()
    {
        InitializeComponent();
        ThemeService.Apply(_ds.Data.Profile.Theme, _ds.Data.Profile.AccentColor);
        UpdateSidebarLabels();
        UpdateSidebar();

        // NavigationService'e abone ol
        NavigationService.Instance.PageChanged += page =>
        {
            Dispatcher.Invoke(() =>
            {
                // Sidebar butonlarını senkronize et
                foreach (var nb in new[] { NavHome, NavSets, NavStats, NavBadges, NavProfile })
                    nb.IsChecked = nb.Tag?.ToString() == page;

                switch (page)
                {
                    case "home":    ShowHome();    break;
                    case "sets":    ShowSets();    break;
                    case "stats":   ShowStats();   break;
                    case "badges":  ShowBadges();  break;
                    case "profile": ShowProfile(); break;
                }
            });
        };

        NavigationService.Instance.NewSetRequested += () =>
        {
            Dispatcher.Invoke(() => OpenSetEditor(null));
        };

        NavigationService.Instance.RebuildRequested += () =>
        {
            Dispatcher.Invoke(RebuildUI);
        };

        NavigationService.Instance.StudyRequested += (setId, mode) =>
        {
            Dispatcher.Invoke(() =>
            {
                _currentSetId = setId;
                StartStudy(mode);
            });
        };

        NavigationService.Instance.AvatarChanged += () =>
        {
            Dispatcher.Invoke(UpdateSidebar);
        };

        ShowHome();
        LoadViewModels();
    }

    private void LoadViewModels()
    {
        HomeVM.Load();
        SetsVM.Load();
        StatsVM.Load();
        BadgesVM.Load();
        ProfileVM.Load();
    }

    private void RebuildUI()
    {
        LoadViewModels(); 
        UpdateSidebarLabels();
        UpdateSidebar();
        switch (_currentPage)
        {
            case "home":    ShowHome();                    break;
            case "sets":    ShowSets();                    break;
            case "detail":  ShowDetail(_currentSetId);     break;
            case "stats":   ShowStats();                   break;
            case "badges":  ShowBadges();                  break;
            case "profile": ShowProfile();                 break;
        }
    }

    private void UpdateSidebarLabels()
    {
        NavHomeText.Text    = L.NavHome;
        NavSetsText.Text    = L.NavSets;
        NavStatsText.Text   = L.NavStats;
        NavBadgesText.Text  = L.NavBadges;
        NavProfileText.Text = L.NavProfile;
        QuickStudyText.Text = L.QuickStudy;

        DetailBackBtn.Content     = L.BackToSets;
        DetailEditBtn.Content     = L.EditSet;
        DetailDeleteBtn.Content   = L.DeleteSet;
        DetailFlashcardsText.Text = L.Flashcards;
        DetailLearnText.Text      = L.Learn;
        DetailTestText.Text       = L.Test;
        DetailTimedText.Text      = L.Timed;
        DetailSearchHint.Text     = L.DetailSearchHint;

        // Legacy UI elements are now inside UserControls
        // SetsSearchHint.Text = L.SearchPlaceholder;
        // NewSetBtn.Content   = L.NewSet;
        // UpdateCategoryFilter();
    }

    private void UpdateCategoryFilter()
    {
        /* logic moved to ViewModels
        var currentTag = (CategoryFilter.SelectedItem as ComboBoxItem)?.Tag as string ?? "";
        CategoryFilter.SelectionChanged -= Filter_Changed;
        CategoryFilter.Items.Clear();
        ...
        */
    }

    // ═══════════════════════════════════════
    //  NAVIGATION
    // ═══════════════════════════════════════
    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton btn) return;
        foreach (var nb in new[] { NavHome, NavSets, NavStats, NavBadges, NavProfile })
            nb.IsChecked = false;
        btn.IsChecked = true;

        // Artık NavigationService üzerinden
        NavigationService.Instance.NavigateTo(btn.Tag?.ToString() ?? "home");
    }

    private void HideAllPages()
    {
        PageHome.Visibility    = Visibility.Collapsed;
        PageSets.Visibility    = Visibility.Collapsed;
        PageDetail.Visibility  = Visibility.Collapsed;
        PageStudy.Visibility   = Visibility.Collapsed;
        PageStats.Visibility   = Visibility.Collapsed;
        PageBadges.Visibility  = Visibility.Collapsed;
        PageProfile.Visibility = Visibility.Collapsed;
    }

    private void UpdateSidebar()
    {
        var p = _ds.Data.Profile;
        StreakText.Text = L.Lang == AppLanguage.Turkish ? $"{p.Streak} günlük seri" : $"{p.Streak} day streak";
        LevelText.Text  = $"Lv.{p.Level}";
        int xpInLevel   = p.XP % 200;
        XPText.Text     = $"{p.XP} XP";
        if (AvatarText != null) AvatarText.Text = p.Avatar;
        Dispatcher.InvokeAsync(() =>
        {
            var w = (XPBar.Parent as Border)?.ActualWidth ?? 180;
            XPBar.Width = w * (xpInLevel / 200.0);
        }, DispatcherPriority.Loaded);
    }

    // ═══════════════════════════════════════
    //  WINDOW CONTROLS
    // ═══════════════════════════════════════
    private void TitleBar_Drag(object s, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else DragMove();
    }

    private void CloseBtn_Click(object s, RoutedEventArgs e) => Close();
    private void MinBtn_Click(object s,   RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Window_KeyDown(object s, KeyEventArgs e)
    {
        if (_currentPage != "study") return;
        if (e.Key == Key.Space && (_studyMode == StudyMode.Flashcard || _studyMode == StudyMode.Timed))
        {
            if (StudyContent.Children.Count > 0)
            {
                if (StudyContent.Children[0] is Grid container
                    && container.Children.Count > 0
                    && container.Children[0] is Border cardBorder)
                {
                    cardBorder.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.MouseLeftButtonDownEvent
                    });
                }
            }
            e.Handled = true;
        }
        if (e.Key == Key.Right && _studyMode == StudyMode.Flashcard)
        {
            _studyIndex++;
            RenderStudyQuestion();
            e.Handled = true;
        }
    }
}

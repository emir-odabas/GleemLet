using System.Diagnostics;
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

    private string      _currentSetId = "";
    private StudyMode   _studyMode    = StudyMode.Flashcard;
    private List<Flashcard> _studyQueue = [];
    private int  _studyIndex;
    private int  _studyCorrect;
    private int  _studyWrong;
    private bool _fcFlipped;
    private bool _answered;
    private DispatcherTimer? _timer;
    private int      _timedSeconds;
    private DateTime _sessionStart;
    private string   _currentPage = "home";

    public MainWindow()
    {
        InitializeComponent();
        // Kayıtlı temayı uygula
        ThemeService.Apply(_ds.Data.Profile.Theme, _ds.Data.Profile.AccentColor);
        UpdateSidebarLabels();
        UpdateSidebar();
        ShowHome();
    }

    // Dil değişince tüm UI'ı yeniden çiz
    private void RebuildUI()
    {
        UpdateSidebarLabels();
        UpdateSidebar();
        switch (_currentPage)
        {
            case "home":    ShowHome();         break;
            case "sets":    ShowSets();         break;
            case "detail":  ShowDetail(_currentSetId); break;
            case "stats":   ShowStats();        break;
            case "badges":  ShowBadges();       break;
            case "profile": ShowProfile();      break;
        }
    }

    // XAML'daki sidebar nav TextBlock'larını dil değişince güncelle
    private void UpdateSidebarLabels()
    {
        NavHomeText.Text    = L.NavHome;
        NavSetsText.Text    = L.NavSets;
        NavStatsText.Text   = L.NavStats;
        NavBadgesText.Text  = L.NavBadges;
        NavProfileText.Text = L.NavProfile;
        QuickStudyText.Text = L.QuickStudy;
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
        switch (btn.Tag?.ToString())
        {
            case "home":    ShowHome();    break;
            case "sets":    ShowSets();    break;
            case "stats":   ShowStats();   break;
            case "badges":  ShowBadges();  break;
            case "profile": ShowProfile(); break;
        }
    }

    private void HideAllPages()
    {
        // Lobi müziği sadece ana ekranda çalar — diğer sayfaya geçince durdur
        SoundSvc.StopLobbyMusic();
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
        var p       = _ds.Data.Profile;
        StreakText.Text = L.Lang == AppLanguage.Turkish ? $"{p.Streak} günlük seri" : $"{p.Streak} day streak";
        LevelText.Text  = $"Lv.{p.Level}";
        int xpInLevel   = p.XP % 200;
        XPText.Text     = $"{p.XP} XP";
        // Avatar
        if (AvatarText != null) AvatarText.Text = p.Avatar;
        Dispatcher.InvokeAsync(() =>
        {
            var w = (XPBar.Parent as Border)?.ActualWidth ?? 180;
            XPBar.Width = w * (xpInLevel / 200.0);
        }, DispatcherPriority.Loaded);
    }

    // ═══════════════════════════════════════
    //  HOME PAGE
    // ═══════════════════════════════════════
    private void ShowHome()
    {
        _currentPage = "home";
        HideAllPages();
        PageHome.Visibility = Visibility.Visible;
        HomePanel.Children.Clear();

        // Lobi müziğini başlat (eğer açıksa)
        if (_ds.Data.Profile.MusicEnabled)
            SoundSvc.StartLobbyMusic();

        var p            = _ds.Data.Profile;
        int totalWords   = _ds.Data.Sets.Sum(s => s.Words.Count);
        int totalLearned = _ds.Data.Sets.Sum(s => s.LearnedCount);

        // Hero card
        var hero     = MakeCard("TealBg", "Teal2");
        var heroGrid = new Grid();
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var heroLeft = new StackPanel();
        heroLeft.Children.Add(MakeText(L.WelcomeBack(p.Name), 13, "#4FAC82", isBold: true));
        heroLeft.Children.Add(MakeText(p.Goal, 22, "#E8E8F0", isBold: true, margin: new Thickness(0, 4, 0, 6)));
        heroLeft.Children.Add(MakeText(L.ConsistencyTip, 13, "#8888A0"));

        var xpWrap = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
        var xpBarBg = new Border { Background = new SolidColorBrush(Color.FromRgb(30, 30, 36)), Height = 8, CornerRadius = new CornerRadius(4) };
        var xpFill  = new Border
        {
            Height = 8, CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = new LinearGradientBrush(Color.FromRgb(229, 192, 123), Color.FromRgb(224, 108, 117), 0)
        };
        xpBarBg.Child = xpFill;
        xpWrap.Children.Add(xpBarBg);
        xpWrap.Children.Add(MakeText($"Level {p.Level}  ·  {L.XPToNextLevel(p.XP % 200)}", 11, "#4A4A60", margin: new Thickness(0, 5, 0, 0)));
        heroLeft.Children.Add(xpWrap);
        Dispatcher.InvokeAsync(() => xpFill.Width = Math.Max(0, xpBarBg.ActualWidth * ((p.XP % 200) / 200.0)), DispatcherPriority.Loaded);

        Grid.SetColumn(heroLeft, 0);
        heroGrid.Children.Add(heroLeft);

        var statsPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
        statsPanel.Children.Add(MakeStat(p.Streak.ToString() + "🔥", L.StreakLabel));
        statsPanel.Children.Add(MakeStat(totalWords.ToString(), L.WordsLabel));
        statsPanel.Children.Add(MakeStat(totalLearned.ToString(), L.LearnedLabel));
        statsPanel.Children.Add(MakeStat(p.TotalStudySessions.ToString(), L.SessionsLabel));
        Grid.SetColumn(statsPanel, 1);
        heroGrid.Children.Add(statsPanel);
        hero.Child = heroGrid;
        HomePanel.Children.Add(hero);

        HomePanel.Children.Add(MakeSectionHeader(L.RecentSets, L.NewSet, () => { NavSets.IsChecked = true; ShowSets(); }));

        var recentSets = _ds.Data.Sets.OrderByDescending(s => s.LastStudied ?? s.Created).Take(4).ToList();
        if (recentSets.Count == 0)
        {
            HomePanel.Children.Add(MakeEmptyState("📚", L.NoSetsYet, L.CreateFirstSet, L.CreateSet, () => { NavSets.IsChecked = true; ShowSets(); }));
        }
        else
        {
            var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (var set in recentSets)
                wrap.Children.Add(MakeSetCard(set));
            HomePanel.Children.Add(wrap);
        }

        if (_ds.Data.Sessions.Count > 0)
        {
            HomePanel.Children.Add(MakeSectionHeader(L.RecentActivity, null, null));
            foreach (var session in _ds.Data.Sessions.OrderByDescending(s => s.Date).Take(5))
            {
                var row = new Border
                {
                    Background      = new SolidColorBrush(Color.FromRgb(28, 28, 34)),
                    BorderBrush     = new SolidColorBrush(Color.FromRgb(46, 46, 58)),
                    BorderThickness = new Thickness(1),
                    CornerRadius    = new CornerRadius(8),
                    Padding         = new Thickness(14, 10, 14, 10),
                    Margin          = new Thickness(0, 0, 0, 6),
                };
                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // FIX: StudyMode enum extension ile icon — "timed" artık ⏱ gösteriyor
                var modeIcon = StudyModeExtensions.FromString(session.Mode).ToIcon();
                var left = new StackPanel();
                left.Children.Add(MakeText($"{modeIcon}  {session.SetName}", 13, "#E8E8F0", isBold: true));
                left.Children.Add(MakeText(session.Date.ToString("MMM d, yyyy  h:mm tt"), 11, "#4A4A60", margin: new Thickness(0, 2, 0, 0)));
                Grid.SetColumn(left, 0);
                var acc = new Border { Background = new SolidColorBrush(Color.FromRgb(26, 46, 36)), CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4, 8, 4) };
                acc.Child = MakeText($"{session.Accuracy:F0}%  ·  {session.Correct}/{session.Total}", 12, "#4FAC82", isBold: true);
                Grid.SetColumn(acc, 1);
                g.Children.Add(left); g.Children.Add(acc);
                row.Child = g;
                HomePanel.Children.Add(row);
            }
        }

        // ── Günlük Hedef Progress Bar ──────────────────────────────────────
        if (p.DailyGoalWords > 0)
        {
            int todayLearned = _ds.Data.Sets
                .SelectMany(s => s.Words)
                .Count(w => w.LastStudied.HasValue && w.LastStudied.Value.Date == DateTime.Today);
            double goalPct = Math.Min(1.0, (double)todayLearned / p.DailyGoalWords);

            var goalCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(46, 46, 58)),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18, 14, 18, 14), Margin = new Thickness(0, 0, 0, 16)
            };
            var goalInner  = new StackPanel();
            var goalHeader = new Grid();
            goalHeader.Children.Add(MakeText(L.DailyGoalSection, 13, "#E8E8F0", isBold: true));
            var goalPctLbl = MakeText($"{goalPct * 100:F0}%", 13, "#4FAC82", isBold: true);
            goalPctLbl.HorizontalAlignment = HorizontalAlignment.Right;
            goalHeader.Children.Add(goalPctLbl);
            goalInner.Children.Add(goalHeader);
            goalInner.Children.Add(MakeText(L.TodayProgress(todayLearned, p.DailyGoalWords), 11, "#8888A0", margin: new Thickness(0, 4, 0, 8)));
            var goalBg   = new Border { Background = new SolidColorBrush(Color.FromRgb(36, 36, 48)), Height = 8, CornerRadius = new CornerRadius(4) };
            var goalFill = new Border { CornerRadius = new CornerRadius(4), HorizontalAlignment = HorizontalAlignment.Left, Height = 8,
                Background = new LinearGradientBrush(Color.FromRgb(79, 172, 130), Color.FromRgb(92, 222, 181), 0) };
            goalBg.Child = goalFill;
            goalInner.Children.Add(goalBg);
            goalCard.Child = goalInner;
            HomePanel.Children.Add(goalCard);
            Dispatcher.InvokeAsync(() => goalFill.Width = Math.Max(0, goalBg.ActualWidth * goalPct), DispatcherPriority.Loaded);
        }

        UpdateSidebar();
    }


    // ═══════════════════════════════════════
    //  SETS PAGE
    // ═══════════════════════════════════════
    private void ShowSets(string filter = "", string cat = "")
    {
        _currentPage = "sets";
        HideAllPages();
        PageSets.Visibility = Visibility.Visible;
        RenderSets(filter, cat);
    }

    private void RenderSets(string filter = "", string cat = "")
    {
        SetsPanel.Children.Clear();
        var sets = _ds.Data.Sets.AsEnumerable();
        if (!string.IsNullOrEmpty(filter))
            sets = sets.Where(s => s.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                                   s.Words.Any(w => (w.En ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase) || (w.Tr ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrEmpty(cat) && cat != "All Categories")
            sets = sets.Where(s => s.Category == cat);

        var list = sets.OrderByDescending(s => s.LastStudied ?? s.Created).ToList();
        if (list.Count == 0)
        {
            SetsPanel.Children.Add(MakeEmptyState("📚", L.NoSetsFound, L.CreateSetToStart, L.CreateSet, () => OpenSetEditor(null)));
            return;
        }
        foreach (var set in list)
            SetsPanel.Children.Add(MakeSetCard(set, large: true));
    }

    private void Search_Changed(object s, TextChangedEventArgs e)
    {
        // FIX: InitializeComponent sırasında CategoryFilter item'ları set edilince
        // SelectionChanged tetiklenir ama SetsPanel henüz null olur — guard ekledik.
        if (SetsPanel == null) return;
        RenderSets(SearchBox.Text, (CategoryFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "");
    }

    private void Filter_Changed(object s, SelectionChangedEventArgs e)
    {
        // FIX: Aynı sebep — XAML yüklenirken erken tetiklenmeye karşı guard.
        if (SetsPanel == null) return;
        RenderSets(SearchBox.Text, (CategoryFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "");
    }

    // ═══════════════════════════════════════
    //  SET DETAIL
    // ═══════════════════════════════════════
    private void ShowDetail(string setId)
    {
        _currentPage = "detail";
        _currentSetId = setId;
        HideAllPages();
        PageDetail.Visibility = Visibility.Visible;
        var set = _ds.Data.Sets.FirstOrDefault(s => s.Id == setId);
        if (set == null) return;

        DetailName.Text = set.Name;
        DetailMeta.Text = L.WordsCount(set.Words.Count, set.LearnedCount, set.Category, set.Progress);
        FavBtn.Content  = set.IsFavorite ? "★" : "☆";
        FavBtn.Foreground = set.IsFavorite
            ? new SolidColorBrush(Color.FromRgb(229, 192, 123))
            : new SolidColorBrush(Color.FromRgb(136, 136, 160));

        DetailWords.Children.Clear();

        var pb = new Border { Background = new SolidColorBrush(Color.FromRgb(28, 28, 34)), Height = 8, CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 0, 20) };
        var pf = new Border { Background = new SolidColorBrush(Color.FromRgb(79, 172, 130)), CornerRadius = new CornerRadius(4), HorizontalAlignment = HorizontalAlignment.Left };
        pb.Child = pf;
        DetailWords.Children.Add(pb);
        Dispatcher.InvokeAsync(() => pf.Width = pb.ActualWidth * (set.Progress / 100.0), DispatcherPriority.Loaded);

        foreach (var w in set.Words)
        {
            var row = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(28, 28, 34)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(46, 46, 58)),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
                Margin          = new Thickness(0, 0, 0, 6),
                Padding         = new Thickness(0, 10, 14, 10),
            };
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var accent = new Border
            {
                Width = 4, CornerRadius = new CornerRadius(2, 0, 0, 2),
                Background = w.Learned
                    ? new SolidColorBrush(Color.FromRgb(79, 172, 130))
                    : new SolidColorBrush(Color.FromRgb(46, 46, 58))
            };
            Grid.SetColumn(accent, 0);

            var enSp = new StackPanel { Margin = new Thickness(14, 0, 0, 0) };
            enSp.Children.Add(MakeText(w.En, 14, "#E8E8F0", isBold: true));
            if (!string.IsNullOrEmpty(w.Example))
                enSp.Children.Add(MakeText(w.Example, 11, "#4A4A60", margin: new Thickness(0, 2, 0, 0)));
            Grid.SetColumn(enSp, 1);

            var trText = MakeText(w.Tr, 13, "#8888A0");
            trText.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(trText, 2);

            var learned = new CheckBox
            {
                IsChecked = w.Learned, VerticalAlignment = VerticalAlignment.Center,
                ToolTip   = L.MarkLearned
            };
            var capturedId = w.Id;
            learned.Checked   += (s, e) => { var fw = set.Words.FirstOrDefault(x => x.Id == capturedId); if (fw != null) { fw.Learned = true;  _ds.Save(); } };
            learned.Unchecked += (s, e) => { var fw = set.Words.FirstOrDefault(x => x.Id == capturedId); if (fw != null) { fw.Learned = false; _ds.Save(); } };
            Grid.SetColumn(learned, 3);

            g.Children.Add(accent); g.Children.Add(enSp); g.Children.Add(trText); g.Children.Add(learned);
            row.Child = g;
            DetailWords.Children.Add(row);
        }
    }

    // ═══════════════════════════════════════
    //  STUDY MODES
    // ═══════════════════════════════════════
    private void StartStudy(StudyMode mode)
    {
        var set = _ds.Data.Sets.FirstOrDefault(s => s.Id == _currentSetId);
        if (set == null || set.Words.Count == 0) { ShowMsg(L.AddWordsFirst); return; }
        if (set.Words.Count < 2 && mode == StudyMode.Learn) { ShowMsg(L.NeedTwoWords); return; }

        // FIX: Learn modu için tüm settlerde yeterli kelime var mı kontrol et
        if (mode == StudyMode.Learn)
        {
            int totalOthers = _ds.Data.Sets.SelectMany(s => s.Words).Count(w => w.Id != set.Words.FirstOrDefault()?.Id);
            if (totalOthers < 3 && set.Words.Count < 4)
            {
                ShowMsg(L.NeedFourWords);
                return;
            }
        }

        _studyMode    = mode;
        _studyQueue   = [.. set.Words];
        if (_ds.Data.Profile.ShuffleDefault || mode == StudyMode.Timed)
            _studyQueue = [.. _studyQueue.OrderBy(_ => Random.Shared.Next())];
        _studyIndex   = 0;
        _studyCorrect = 0;
        _studyWrong   = 0;
        _answered     = false;
        _sessionStart = DateTime.Now;

        _currentPage = "study";
        HideAllPages();
        PageStudy.Visibility = Visibility.Visible;

        // FIX: StudyMode enum extension ile label — magic string yok
        StudyTitle.Text = mode.ToLabel();

        if (mode == StudyMode.Timed)
        {
            _timedSeconds  = 0;
            TimerText.Text = "0:00";
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                _timedSeconds++;
                TimerText.Text = $"{_timedSeconds / 60}:{_timedSeconds % 60:D2}";
            };
            _timer.Start();
        }
        else { TimerText.Text = ""; _timer?.Stop(); }

        RenderStudyQuestion();
    }

    private void RenderStudyQuestion()
    {
        StudyContent.Children.Clear();
        _answered = false;

        if (_studyIndex >= _studyQueue.Count) { ShowResults(); return; }

        var w   = _studyQueue[_studyIndex];
        double pct = _studyQueue.Count > 0 ? (double)_studyIndex / _studyQueue.Count : 0;
        StudyCounter.Text = $"{_studyIndex + 1}/{_studyQueue.Count}  ·  ✓{_studyCorrect} ✗{_studyWrong}";
        Dispatcher.InvokeAsync(() => ProgressFill.Width = ProgressBorder.ActualWidth * pct, DispatcherPriority.Loaded);

        switch (_studyMode)
        {
            case StudyMode.Flashcard:
            case StudyMode.Timed:
                RenderFlashcard(w); break;
            case StudyMode.Learn:
                RenderLearn(w);     break;
            case StudyMode.Test:
                RenderTestQ(w);     break;
        }
    }

    // ── FLASHCARD ──
    private void RenderFlashcard(Flashcard w)
    {
        _fcFlipped = false;
        var container = new Grid();
        container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var cardBorder = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(28, 28, 34)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(46, 46, 58)),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(16),
            Cursor          = Cursors.Hand,
            Margin          = new Thickness(80, 40, 80, 20),
            Padding         = new Thickness(40),
        };

        var inner     = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var labelText = MakeText(L.ClickToFlip, 10, "#4A4A60");
        labelText.HorizontalAlignment = HorizontalAlignment.Center;
        labelText.Margin = new Thickness(0, 0, 0, 24);

        var wordText = new TextBlock
        {
            Text = w.En, FontFamily = new FontFamily("Segoe UI"), FontSize = 36, FontWeight = FontWeights.Bold,
            Foreground          = new SolidColorBrush(Color.FromRgb(232, 232, 240)),
            HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap,
            TextAlignment       = TextAlignment.Center
        };
        var trText = new TextBlock
        {
            Text = w.Tr, FontFamily = new FontFamily("Segoe UI"), FontSize = 20,
            Foreground          = new SolidColorBrush(Color.FromRgb(79, 172, 130)),
            HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap,
            TextAlignment       = TextAlignment.Center, Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 16, 0, 0)
        };
        var exText = new TextBlock
        {
            Text = w.Example, FontFamily = new FontFamily("Segoe UI"), FontSize = 13, FontStyle = FontStyles.Italic,
            Foreground          = new SolidColorBrush(Color.FromRgb(74, 74, 96)),
            HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap,
            TextAlignment       = TextAlignment.Center, Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 12, 0, 0)
        };

        inner.Children.Add(labelText); inner.Children.Add(wordText);
        inner.Children.Add(trText);    inner.Children.Add(exText);
        cardBorder.Child = inner;

        var actionPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16), Visibility = Visibility.Collapsed
        };
        var knowBtn  = new Button { Content = L.GotIt, Padding = new Thickness(24, 10, 24, 10), Margin = new Thickness(0, 0, 12, 0) };
        knowBtn.Style = (Style)FindResource("PrimaryButton");
        var stillBtn = new Button { Content = L.StillLearning, Padding = new Thickness(16, 10, 16, 10) };
        stillBtn.Style = (Style)FindResource("GhostButton");
        actionPanel.Children.Add(stillBtn); actionPanel.Children.Add(knowBtn);

        var navPanel   = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 32) };
        var prevBtn    = new Button { Content = L.Prev,    Style = (Style)FindResource("GhostButton"),   Padding = new Thickness(20, 10, 20, 10), Margin = new Thickness(0, 0, 10, 0) };
        var nextBtn    = new Button { Content = L.Next,    Style = (Style)FindResource("PrimaryButton"), Padding = new Thickness(20, 10, 20, 10) };
        var shuffleBtn = new Button { Content = L.Shuffle, Style = (Style)FindResource("GhostButton"),   Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(10, 0, 0, 0) };
        navPanel.Children.Add(prevBtn); navPanel.Children.Add(nextBtn); navPanel.Children.Add(shuffleBtn);

        Grid.SetRow(cardBorder, 0);
        var bottomPanel = new StackPanel();
        bottomPanel.Children.Add(actionPanel); bottomPanel.Children.Add(navPanel);
        Grid.SetRow(bottomPanel, 1);
        container.Children.Add(cardBorder); container.Children.Add(bottomPanel);
        StudyContent.Children.Add(container);

        void Flip()
        {
            if (_fcFlipped) return;
            _fcFlipped          = true;
            trText.Visibility   = Visibility.Visible;
            exText.Visibility   = !string.IsNullOrEmpty(w.Example) ? Visibility.Visible : Visibility.Collapsed;
            labelText.Text      = L.Meaning;
            wordText.Foreground = new SolidColorBrush(Color.FromRgb(79, 172, 130));
            actionPanel.Visibility = Visibility.Visible;
            cardBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(79, 172, 130));
        }

        cardBorder.MouseLeftButtonDown += (s, e) => Flip();

        // FIX: Space tuşu ile flip artık çalışıyor (Window_KeyDown'a da bağlı)
        cardBorder.KeyDown += (s, e) => { if (e.Key == Key.Space) Flip(); };

        knowBtn.Click    += (s, e) => { w.Learned = true; w.CorrectCount++; _studyCorrect++; _ds.Save(); _studyIndex++; RenderStudyQuestion(); };
        stillBtn.Click   += (s, e) => { w.WrongCount++; _studyWrong++;                                   _studyIndex++; RenderStudyQuestion(); };
        nextBtn.Click    += (s, e) => { _studyIndex++; RenderStudyQuestion(); };
        prevBtn.Click    += (s, e) => { if (_studyIndex > 0) { _studyIndex--; RenderStudyQuestion(); } };
        shuffleBtn.Click += (s, e) => { _studyQueue = [.. _studyQueue.OrderBy(_ => Random.Shared.Next())]; _studyIndex = 0; RenderStudyQuestion(); };
    }

    // ── LEARN (MULTIPLE CHOICE) ──
    private void RenderLearn(Flashcard correct)
    {
        var panel = new StackPanel { Margin = new Thickness(60, 32, 60, 32) };
        panel.Children.Add(MakeText(L.WhatMeaning, 12, "#8888A0"));

        var qCard = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(28, 28, 34)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(46, 46, 58)),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12),
            Padding         = new Thickness(28, 24, 28, 24), Margin = new Thickness(0, 10, 0, 24)
        };
        var qInner = new StackPanel();
        qInner.Children.Add(MakeText(correct.En, 30, "#E8E8F0", isBold: true));
        if (!string.IsNullOrEmpty(correct.Example))
            qInner.Children.Add(MakeText(correct.Example, 12, "#4A4A60", margin: new Thickness(0, 8, 0, 0)));
        qCard.Child = qInner;
        panel.Children.Add(qCard);

        var allWords = _ds.Data.Sets.SelectMany(s => s.Words).Where(w => w.Id != correct.Id).ToList();
        var wrong3   = allWords.OrderBy(_ => Random.Shared.Next()).Take(3).ToList();

        // FIX: Yeterli yanlış seçenek yoksa aynı set'ten doldur
        if (wrong3.Count < 3)
        {
            var extraFromSet = _studyQueue.Where(w => w.Id != correct.Id).Take(3 - wrong3.Count).ToList();
            wrong3.AddRange(extraFromSet.Where(e => wrong3.All(x => x.Id != e.Id)));
        }

        var options      = wrong3.Append(correct).OrderBy(_ => Random.Shared.Next()).ToList();
        var resultBorder = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 14, 0, 0), Visibility = Visibility.Collapsed };
        var resultText   = MakeText("", 13, "#E8E8F0");
        resultBorder.Child = resultText;

        var nextBtn = new Button
        {
            Content = L.Next, Style = (Style)FindResource("PrimaryButton"),
            Padding = new Thickness(24, 10, 24, 10), HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0), Visibility = Visibility.Collapsed
        };

        var optGrid = new UniformGrid { Columns = 2 };
        foreach (var opt in options)
        {
            var btn = new Button
            {
                Content = opt.Tr,
                Style   = (Style)FindResource("GhostButton"),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 0, 8, 8),
                FontSize = 13,
                // FIX: Tag'e ID saklıyoruz — Tr string karşılaştırması kaldırıldı
                Tag = opt.Id
            };
            var capturedOpt = opt;
            btn.Click += (s, e) =>
            {
                if (_answered) return;
                _answered = true;
                foreach (Button b in optGrid.Children) b.IsEnabled = false;

                if (capturedOpt.Id == correct.Id)
                {
                    if (_ds.Data.Profile.SoundEnabled) SoundSvc.PlayCorrect();
                    AnimationHelper.PulseGreen(btn);
                    btn.Background  = new SolidColorBrush(Color.FromRgb(26, 46, 36));
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                    btn.Foreground  = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                    correct.Learned = true; correct.CorrectCount++; _studyCorrect++;
                    resultBorder.Background   = new SolidColorBrush(Color.FromRgb(26, 46, 36));
                    resultBorder.BorderBrush  = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                    resultBorder.BorderThickness = new Thickness(1);
                    resultText.Text       = L.Correct;
                    resultText.Foreground = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                }
                else
                {
                    if (_ds.Data.Profile.SoundEnabled) SoundSvc.PlayWrong();
                    AnimationHelper.Shake(btn);
                    AnimationHelper.PulseRed(btn);
                    btn.Background  = new SolidColorBrush(Color.FromRgb(46, 26, 30));
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 108, 117));
                    btn.Foreground  = new SolidColorBrush(Color.FromRgb(224, 108, 117));
                    _studyWrong++;

                    // FIX: ID karşılaştırması — Tr string değil
                    foreach (Button b in optGrid.Children)
                    {
                        if ((string?)b.Tag == correct.Id)
                        {
                            b.Background  = new SolidColorBrush(Color.FromRgb(26, 46, 36));
                            b.BorderBrush = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                            b.Foreground  = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                        }
                    }
                    resultBorder.Background   = new SolidColorBrush(Color.FromRgb(46, 26, 30));
                    resultBorder.BorderBrush  = new SolidColorBrush(Color.FromRgb(224, 108, 117));
                    resultBorder.BorderThickness = new Thickness(1);
                    resultText.Text       = L.Incorrect(correct.Tr);
                    resultText.Foreground = new SolidColorBrush(Color.FromRgb(224, 108, 117));
                }
                _ds.Save();
                resultBorder.Visibility = Visibility.Visible;
                nextBtn.Visibility      = Visibility.Visible;
            };
            optGrid.Children.Add(btn);
        }
        panel.Children.Add(optGrid);
        panel.Children.Add(resultBorder);
        nextBtn.Click += (s, e) => { _studyIndex++; RenderStudyQuestion(); };
        panel.Children.Add(nextBtn);

        var sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = panel };
        StudyContent.Children.Add(sv);
    }

    // ── TEST (TYPE ANSWER) ──
    private void RenderTestQ(Flashcard w)
    {
        var panel = new StackPanel { Margin = new Thickness(80, 40, 80, 32) };
        panel.Children.Add(MakeText(L.TypeMeaning, 12, "#8888A0"));

        var qCard = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(28, 28, 34)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(46, 46, 58)),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12),
            Padding         = new Thickness(28, 24, 28, 24), Margin = new Thickness(0, 10, 0, 24)
        };
        var qInner = new StackPanel();
        qInner.Children.Add(MakeText(w.En, 30, "#E8E8F0", isBold: true));
        if (!string.IsNullOrEmpty(w.Example))
            qInner.Children.Add(MakeText(w.Example, 12, "#4A4A60", margin: new Thickness(0, 8, 0, 0)));
        qCard.Child = qInner;
        panel.Children.Add(qCard);

        var input        = new TextBox { Style = (Style)Application.Current.Resources["DarkTextBox"], FontSize = 15, Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 0, 0, 12) };
        var feedback     = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 12), Visibility = Visibility.Collapsed };
        var feedbackText = MakeText("", 13, "#E8E8F0");
        feedback.Child = feedbackText;
        var checkBtn = new Button { Content = L.Check, Style = (Style)FindResource("PrimaryButton"), Padding = new Thickness(24, 10, 24, 10), HorizontalAlignment = HorizontalAlignment.Right };
        var nextBtn  = new Button { Content = L.Next,  Style = (Style)FindResource("PrimaryButton"), Padding = new Thickness(24, 10, 24, 10), HorizontalAlignment = HorizontalAlignment.Right, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 10, 0, 0) };

        panel.Children.Add(input); panel.Children.Add(feedback);
        panel.Children.Add(checkBtn); panel.Children.Add(nextBtn);

        void Check()
        {
            if (_answered) return;
            _answered = true;
            var answer   = input.Text.Trim().ToLower();
            var corrects = w.Tr.Split([',', '/', ';'], StringSplitOptions.TrimEntries).Select(x => x.ToLower()).ToList();
            bool ok      = corrects.Any(c => c == answer || answer.Contains(c) || c.Contains(answer));
            input.IsEnabled = false;
            if (ok)
            {
                if (_ds.Data.Profile.SoundEnabled) SoundSvc.PlayCorrect();
                input.BorderBrush        = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                feedback.Background      = new SolidColorBrush(Color.FromRgb(26, 46, 36));
                feedback.BorderBrush     = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                feedback.BorderThickness = new Thickness(1);
                feedbackText.Text        = L.Correct;
                feedbackText.Foreground  = new SolidColorBrush(Color.FromRgb(79, 172, 130));
                w.Learned = true; w.CorrectCount++; _studyCorrect++;
            }
            else
            {
                if (_ds.Data.Profile.SoundEnabled) SoundSvc.PlayWrong();
                input.BorderBrush        = new SolidColorBrush(Color.FromRgb(224, 108, 117));
                feedback.Background      = new SolidColorBrush(Color.FromRgb(46, 26, 30));
                feedback.BorderBrush     = new SolidColorBrush(Color.FromRgb(224, 108, 117));
                feedback.BorderThickness = new Thickness(1);
                feedbackText.Text        = L.Lang == AppLanguage.Turkish ? $"✗  Doğrusu: {w.Tr}" : $"✗  Correct answer: {w.Tr}";
                feedbackText.Foreground  = new SolidColorBrush(Color.FromRgb(224, 108, 117));
                _studyWrong++;
            }
            _ds.Save();
            feedback.Visibility = Visibility.Visible;
            checkBtn.Visibility = Visibility.Collapsed;
            nextBtn.Visibility  = Visibility.Visible;
        }

        checkBtn.Click += (s, e) => Check();
        input.KeyDown  += (s, e) => { if (e.Key == Key.Enter) Check(); };
        nextBtn.Click  += (s, e) => { _studyIndex++; RenderStudyQuestion(); };

        var sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = panel };
        StudyContent.Children.Add(sv);
        Dispatcher.InvokeAsync(() => input.Focus(), DispatcherPriority.Loaded);
    }

    // ── RESULTS ──
    private void ShowResults()
    {
        _timer?.Stop();
        int total    = _studyCorrect + _studyWrong;
        double pct   = total > 0 ? (double)_studyCorrect / total * 100 : 0;
        var session  = new StudySession
        {
            SetId   = _currentSetId,
            SetName = _ds.Data.Sets.FirstOrDefault(s => s.Id == _currentSetId)?.Name ?? "",
            // FIX: enum'dan serialized string
            Mode    = _studyMode.ToSerializedString(),
            Correct = _studyCorrect, Wrong = _studyWrong,
            Total   = total, DurationSeconds = (int)(DateTime.Now - _sessionStart).TotalSeconds
        };

        // FIX: RecordSession artık badge listesini döndürüyor — ikinci CheckAndAwardBadges() çağrısı yok
        var newBadges = _ds.RecordSession(session);

        StudyContent.Children.Clear();
        var sv    = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(40) };

        var emoji = pct == 100 ? "🏆" : pct >= 80 ? "🎉" : pct >= 60 ? "👍" : "💪";
        panel.Children.Add(MakeText(emoji, 56, "#E8E8F0", margin: new Thickness(0, 0, 0, 8)));
        panel.Children.Add(MakeText(pct == 100 ? L.Perfect : pct >= 80 ? L.GreatJob : pct >= 60 ? L.GoodEffort : L.KeepGoing, 28, "#E8E8F0", isBold: true, margin: new Thickness(0, 0, 0, 6)));
        panel.Children.Add(MakeText(L.Score(pct), 18, "#4FAC82", isBold: true, margin: new Thickness(0, 0, 0, 24)));

        var breakdown = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 24) };
        breakdown.Children.Add(MakeStatBox(_studyCorrect.ToString(), L.CorrectLabel, "#4FAC82"));
        breakdown.Children.Add(MakeStatBox(_studyWrong.ToString(), L.WrongLabel, "#E06C75"));
        breakdown.Children.Add(MakeStatBox(total.ToString(), L.TotalLabel, "#61AFEF"));
        breakdown.Children.Add(MakeStatBox(session.DurationSeconds < 60 ? $"{session.DurationSeconds}s" : $"{session.DurationSeconds / 60}m{session.DurationSeconds % 60}s", L.TimeLabel, "#E5C07B"));
        panel.Children.Add(breakdown);

        if (newBadges.Count > 0)
        {
            var badgeBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(26, 46, 36)), BorderBrush = new SolidColorBrush(Color.FromRgb(79, 172, 130)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 0, 0, 20), MaxWidth = 400 };
            var badgeInner  = new StackPanel();
            badgeInner.Children.Add(MakeText(L.NewBadges, 13, "#4FAC82", isBold: true, margin: new Thickness(0, 0, 0, 6)));
            foreach (var b in newBadges)
                badgeInner.Children.Add(MakeText(b, 13, "#E8E8F0", margin: new Thickness(0, 2, 0, 0)));
            badgeBorder.Child = badgeInner;
            panel.Children.Add(badgeBorder);
        }

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        var backBtn  = new Button { Content = L.BackToSet, Style = (Style)FindResource("GhostButton"),   Padding = new Thickness(20, 10, 20, 10), Margin = new Thickness(0, 0, 10, 0) };
        var retryBtn = new Button { Content = L.TryAgain,  Style = (Style)FindResource("PrimaryButton"), Padding = new Thickness(20, 10, 20, 10) };
        backBtn.Click  += (s, e) => ShowDetail(_currentSetId);
        retryBtn.Click += (s, e) => StartStudy(_studyMode);
        btnPanel.Children.Add(backBtn); btnPanel.Children.Add(retryBtn);
        panel.Children.Add(btnPanel);

        panel.Children.Add(MakeText("created by gleemron · emirodabas.dev", 10, "#2E2E3A", margin: new Thickness(0, 32, 0, 0)));

        sv.Content = panel;
        StudyContent.Children.Add(sv);
        ProgressFill.Width = ProgressBorder.ActualWidth;
        UpdateSidebar();
    }

    // ═══════════════════════════════════════
    //  STATISTICS PAGE
    // ═══════════════════════════════════════
    private void ShowStats()
    {
        _currentPage = "stats";
        HideAllPages();
        PageStats.Visibility = Visibility.Visible;
        StatsPanel.Children.Clear();
        StatsPanel.Children.Add(MakeText(L.Statistics, 22, "#E8E8F0", isBold: true, margin: new Thickness(0, 0, 0, 20)));

        var p            = _ds.Data.Profile;
        int totalWords   = _ds.Data.Sets.Sum(s => s.Words.Count);
        int totalLearned = _ds.Data.Sets.Sum(s => s.LearnedCount);
        int totalSessions = p.TotalStudySessions;
        int totalTime     = p.TotalStudySeconds;
        double avgAcc     = _ds.Data.Sessions.Count > 0 ? _ds.Data.Sessions.Average(s => s.Accuracy) : 0;

        var topRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 24) };
        topRow.Children.Add(MakeStatBox($"{p.Streak}🔥", L.DayStreak, "#E5C07B"));
        topRow.Children.Add(MakeStatBox($"Lv.{p.Level}", L.LevelLabel, "#4FAC82"));
        topRow.Children.Add(MakeStatBox($"{p.XP}", L.TotalXP, "#61AFEF"));
        topRow.Children.Add(MakeStatBox($"{totalSessions}", L.Sessions, "#E06C75"));
        topRow.Children.Add(MakeStatBox($"{avgAcc:F0}%", L.AvgAccuracy, "#4FAC82"));
        topRow.Children.Add(MakeStatBox(totalTime < 60 ? $"{totalTime}s" : $"{totalTime / 60}m", L.StudyTime, "#E5C07B"));
        StatsPanel.Children.Add(topRow);

        StatsPanel.Children.Add(MakeText(L.ProgressPerSet, 15, "#8888A0", isBold: true, margin: new Thickness(0, 0, 0, 12)));
        foreach (var set in _ds.Data.Sets.OrderByDescending(s => s.Progress))
        {
            var row = new Border { Background = new SolidColorBrush(Color.FromRgb(28, 28, 34)), BorderBrush = new SolidColorBrush(Color.FromRgb(46, 46, 58)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 0, 0, 8) };
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            var nameText      = MakeText(set.Name, 13, "#E8E8F0", isBold: true);
            var progressWrap  = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 16, 0) };
            var pbBg          = new Border { Background = new SolidColorBrush(Color.FromRgb(36, 36, 48)), Height = 6, CornerRadius = new CornerRadius(3) };
            var pbFill        = new Border { Background = new SolidColorBrush(Color.FromRgb(79, 172, 130)), CornerRadius = new CornerRadius(3), HorizontalAlignment = HorizontalAlignment.Left };
            pbBg.Child = pbFill;
            progressWrap.Children.Add(pbBg);
            Dispatcher.InvokeAsync(() => pbFill.Width = pbBg.ActualWidth * (set.Progress / 100.0), DispatcherPriority.Loaded);
            var pctText = MakeText($"{set.Progress:F0}%", 13, "#4FAC82", isBold: true);
            pctText.HorizontalAlignment = HorizontalAlignment.Right;
            pctText.VerticalAlignment   = VerticalAlignment.Center;
            Grid.SetColumn(nameText, 0); Grid.SetColumn(progressWrap, 1); Grid.SetColumn(pctText, 2);
            g.Children.Add(nameText); g.Children.Add(progressWrap); g.Children.Add(pctText);
            row.Child = g;
            StatsPanel.Children.Add(row);
        }

        if (_ds.Data.Sessions.Count > 0)
        {
            StatsPanel.Children.Add(MakeText(L.RecentSessions, 15, "#8888A0", isBold: true, margin: new Thickness(0, 20, 0, 12)));
            foreach (var session in _ds.Data.Sessions.OrderByDescending(s => s.Date).Take(10))
            {
                var row = new Border { Background = new SolidColorBrush(Color.FromRgb(28, 28, 34)), BorderBrush = new SolidColorBrush(Color.FromRgb(46, 46, 58)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(16, 10, 16, 10), Margin = new Thickness(0, 0, 0, 6) };
                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // FIX: Enum extension ile icon — timed artık doğru görünüyor
                var icon = StudyModeExtensions.FromString(session.Mode).ToIcon();
                var left = new StackPanel();
                left.Children.Add(MakeText($"{icon}  {session.SetName}", 13, "#E8E8F0", isBold: true));
                left.Children.Add(MakeText($"{session.Date:MMM d, yyyy}  ·  {session.DurationSeconds}s", 11, "#4A4A60", margin: new Thickness(0, 2, 0, 0)));
                var acc = new Border { Background = new SolidColorBrush(Color.FromRgb(26, 46, 36)), CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4, 8, 4), VerticalAlignment = VerticalAlignment.Center };
                acc.Child = MakeText($"{session.Accuracy:F0}%  ·  {session.Correct}/{session.Total}", 12, "#4FAC82", isBold: true);
                Grid.SetColumn(left, 0); Grid.SetColumn(acc, 1);
                g.Children.Add(left); g.Children.Add(acc);
                row.Child = g;
                StatsPanel.Children.Add(row);
            }
        }
    }

    // ═══════════════════════════════════════
    //  BADGES PAGE
    // ═══════════════════════════════════════
    private void ShowBadges()
    {
        _currentPage = "badges";
        HideAllPages();
        PageBadges.Visibility = Visibility.Visible;
        BadgesPanel.Children.Clear();
        BadgesPanel.Children.Add(MakeText(L.Badges, 22, "#E8E8F0", isBold: true, margin: new Thickness(0, 0, 0, 6)));
        int earned = _ds.Data.Badges.Count(b => b.Earned);
        BadgesPanel.Children.Add(MakeText(L.BadgesEarned(earned, _ds.Data.Badges.Count), 13, "#8888A0", margin: new Thickness(0, 0, 0, 20)));

        var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var badge in _ds.Data.Badges)
        {
            var card = new Border
            {
                Width = 160, Margin = new Thickness(0, 0, 12, 12),
                Background  = badge.Earned ? new SolidColorBrush(Color.FromRgb(26, 46, 36)) : new SolidColorBrush(Color.FromRgb(28, 28, 34)),
                BorderBrush = badge.Earned ? new SolidColorBrush(Color.FromRgb(79, 172, 130)) : new SolidColorBrush(Color.FromRgb(46, 46, 58)),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16), Opacity = badge.Earned ? 1.0 : 0.45,
                ToolTip = badge.Description
            };
            var inner = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            inner.Children.Add(MakeText(badge.Icon, 34, "#E8E8F0", margin: new Thickness(0, 0, 0, 8)));
            inner.Children.Add(MakeText(badge.Name, 12, badge.Earned ? "#4FAC82" : "#8888A0", isBold: true));
            inner.Children.Add(MakeText(badge.Description, 10, "#4A4A60", margin: new Thickness(0, 4, 0, 0)));
            if (badge.Earned && badge.EarnedDate.HasValue)
                inner.Children.Add(MakeText(badge.EarnedDate.Value.ToString("MMM d"), 10, "#3A8A68", margin: new Thickness(0, 4, 0, 0)));
            card.Child = inner;
            wrap.Children.Add(card);
        }
        BadgesPanel.Children.Add(wrap);
    }

    // ═══════════════════════════════════════
    //  PROFILE PAGE
    // ═══════════════════════════════════════
    private void ShowProfile()
    {
        _currentPage = "profile";
        HideAllPages();
        PageProfile.Visibility = Visibility.Visible;
        ProfilePanel.Children.Clear();
        ProfilePanel.Children.Add(MakeText(L.ProfileSettings, 22, "#E8E8F0", isBold: true, margin: new Thickness(0, 0, 0, 24)));

        var p = _ds.Data.Profile;

        AddSettingsCard(ProfilePanel, L.ProfileSection, panel =>
        {
            AddFormRow(panel, L.DisplayName, p.Name, val => { p.Name = val; _ds.Save(); });
            AddFormRow(panel, L.LearningGoal, p.Goal, val => { p.Goal = val; _ds.Save(); });
        });

        AddSettingsCard(ProfilePanel, L.Preferences, panel =>
        {
            AddToggleRow(panel, L.SoundEffects, L.SoundDesc,     p.SoundEnabled,   val => { p.SoundEnabled   = val; _ds.Save(); });
            AddToggleRow(panel, L.LobbyMusic,   L.LobbyMusicDesc, p.MusicEnabled,  val =>
            {
                p.MusicEnabled = val;
                _ds.Save();
                if (val) SoundSvc.StartLobbyMusic();
                else     SoundSvc.StopLobbyMusic();
            });
            AddToggleRow(panel, L.ShuffleDefault, L.ShuffleDesc,  p.ShuffleDefault, val => { p.ShuffleDefault = val; _ds.Save(); });
        });

        // ── DİL SEÇİCİ ──
        AddSettingsCard(ProfilePanel, L.LanguageSection, panel =>
        {
            panel.Children.Add(MakeText(L.LanguageDesc, 12, "#8888A0", margin: new Thickness(0, 0, 0, 12)));
            var langRow = new StackPanel { Orientation = Orientation.Horizontal };

            var btnTR = new Button
            {
                Content = "🇹🇷  Türkçe",
                Style   = (Style)FindResource(L.Lang == AppLanguage.Turkish ? "PrimaryButton" : "GhostButton"),
                Padding = new Thickness(20, 10, 20, 10),
                Margin  = new Thickness(0, 0, 10, 0)
            };
            var btnEN = new Button
            {
                Content = "🇬🇧  English",
                Style   = (Style)FindResource(L.Lang == AppLanguage.English ? "PrimaryButton" : "GhostButton"),
                Padding = new Thickness(20, 10, 20, 10)
            };

            btnTR.Click += (s, e) =>
            {
                L.Lang = AppLanguage.Turkish;
                _ds.Data.Profile.Language = "tr";
                _ds.Save();
                RebuildUI();
            };
            btnEN.Click += (s, e) =>
            {
                L.Lang = AppLanguage.English;
                _ds.Data.Profile.Language = "en";
                _ds.Save();
                RebuildUI();
            };

            langRow.Children.Add(btnTR);
            langRow.Children.Add(btnEN);
            panel.Children.Add(langRow);
        });

        // ── GÖRÜNÜM (TEMA + AKSENT) ──
        AddSettingsCard(ProfilePanel, L.ThemeSection, panel =>
        {
            var p2  = _ds.Data.Profile;
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            var btnDark = new Button
            {
                Content = L.ThemeDark, Padding = new Thickness(20, 10, 20, 10), Margin = new Thickness(0, 0, 10, 0),
                Style   = (Style)FindResource(p2.Theme == "dark" ? "PrimaryButton" : "GhostButton")
            };
            var btnLight = new Button
            {
                Content = L.ThemeLight, Padding = new Thickness(20, 10, 20, 10),
                Style   = (Style)FindResource(p2.Theme == "light" ? "PrimaryButton" : "GhostButton")
            };
            void ApplyAndSave(string theme)
            {
                p2.Theme = theme;
                _ds.Save();
                ThemeService.Apply(theme, p2.AccentColor);
                RebuildUI();
            }
            btnDark.Click  += (s, e) => ApplyAndSave("dark");
            btnLight.Click += (s, e) => ApplyAndSave("light");
            row.Children.Add(btnDark); row.Children.Add(btnLight);
            panel.Children.Add(row);

            // Aksent renk seçici — 6 renkli daire
            panel.Children.Add(MakeText(L.AccentLabel, 11, "#8888A0", isBold: true, margin: new Thickness(0, 0, 0, 8)));
            var accentRow = new StackPanel { Orientation = Orientation.Horizontal };
            var accents = new[] {
                ("teal","#4FAC82"), ("purple","#9B59B6"), ("blue","#61AFEF"),
                ("orange","#E5A44A"), ("pink","#E06C9F"), ("red","#E06C75")
            };
            foreach (var (name, hex) in accents)
            {
                var col  = (Color)ColorConverter.ConvertFromString(hex);
                var circ = new Border
                {
                    Width = 28, Height = 28, CornerRadius = new CornerRadius(14),
                    Background = new SolidColorBrush(col),
                    Margin  = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand,
                    BorderThickness = new Thickness(p2.AccentColor == name ? 3 : 0),
                    BorderBrush = new SolidColorBrush(Colors.White)
                };
                var capturedName = name;
                circ.MouseLeftButtonDown += (s, e) =>
                {
                    p2.AccentColor = capturedName;
                    _ds.Save();
                    ThemeService.Apply(p2.Theme, capturedName);
                    RebuildUI();
                };
                accentRow.Children.Add(circ);
            }
            panel.Children.Add(accentRow);
        });

        // ── AVATAR SEÇİCİ ──
        AddSettingsCard(ProfilePanel, L.AvatarSection, panel =>
        {
            panel.Children.Add(MakeText(L.AvatarDesc, 12, "#8888A0", margin: new Thickness(0, 0, 0, 12)));
            var p2 = _ds.Data.Profile;
            var avatars = new[] { "🎓","🦁","🐺","🦊","🐉","⚡","🌟","🔥","🎯","🧠","🚀","🎮" };
            var wrapAvatar = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (var av in avatars)
            {
                var capturedAv = av;
                var btn = new Border
                {
                    Width = 44, Height = 44, CornerRadius = new CornerRadius(10),
                    Margin = new Thickness(0, 0, 8, 8), Cursor = Cursors.Hand,
                    Background = p2.Avatar == av
                        ? new SolidColorBrush(Color.FromRgb(26, 46, 36))
                        : new SolidColorBrush(Color.FromRgb(36, 36, 48)),
                    BorderBrush = p2.Avatar == av
                        ? new SolidColorBrush(Color.FromRgb(79, 172, 130))
                        : new SolidColorBrush(Color.FromRgb(46, 46, 58)),
                    BorderThickness = new Thickness(2)
                };
                btn.Child = new TextBlock
                {
                    Text = av, FontSize = 22,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center
                };
                btn.MouseLeftButtonDown += (s, e) =>
                {
                    p2.Avatar = capturedAv;
                    _ds.Save();
                    RebuildUI();
                };
                wrapAvatar.Children.Add(btn);
            }
            panel.Children.Add(wrapAvatar);
        });

        // ── GÜNLÜK HEDEF ──
        AddSettingsCard(ProfilePanel, L.DailyGoalSection, panel =>
        {
            panel.Children.Add(MakeText(L.DailyGoalDesc, 12, "#8888A0", margin: new Thickness(0, 0, 0, 12)));
            var p2 = _ds.Data.Profile;
            var goalLabel = MakeText(L.DailyGoalWords(p2.DailyGoalWords), 13, "#4FAC82", isBold: true, margin: new Thickness(0, 0, 0, 8));
            panel.Children.Add(goalLabel);
            var slider = new Slider
            {
                Minimum = 5, Maximum = 50, Value = p2.DailyGoalWords,
                TickFrequency = 5, IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 0)
            };
            slider.ValueChanged += (s, e) =>
            {
                p2.DailyGoalWords = (int)slider.Value;
                goalLabel.Text    = L.DailyGoalWords(p2.DailyGoalWords);
                _ds.Save();
            };
            panel.Children.Add(slider);
        });

        AddSettingsCard(ProfilePanel, L.DataSection, panel =>
        {
            var btnRow    = new StackPanel { Orientation = Orientation.Horizontal };
            var exportBtn = new Button { Content = L.ExportJSON,    Style = (Style)FindResource("GhostButton"),  Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(14, 8, 14, 8) };
            var importBtn = new Button { Content = L.ImportJSON,    Style = (Style)FindResource("GhostButton"),  Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(14, 8, 14, 8) };
            var clearBtn  = new Button { Content = L.ClearAllData, Style = (Style)FindResource("DangerButton"), Padding = new Thickness(14, 8, 14, 8) };
            exportBtn.Click += ExportData;
            importBtn.Click += ImportData;
            clearBtn.Click  += ClearData;
            btnRow.Children.Add(exportBtn); btnRow.Children.Add(importBtn); btnRow.Children.Add(clearBtn);
            panel.Children.Add(btnRow);
        });

        AddSettingsCard(ProfilePanel, L.AboutSection, panel =>
        {
            panel.Children.Add(MakeText("GleemLet v1.0", 14, "#E8E8F0", isBold: true));
            panel.Children.Add(MakeText(L.AppDesc, 13, "#8888A0", margin: new Thickness(0, 4, 0, 8)));
            var link = new TextBlock { FontFamily = new FontFamily("Segoe UI"), FontSize = 13, Cursor = Cursors.Hand };
            var hl   = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run("emirodabas.dev"))
            {
                NavigateUri = new Uri("https://emir-odabas.github.io/emirodabas-dev/"),
                Foreground  = new SolidColorBrush(Color.FromRgb(79, 172, 130))
            };
            hl.RequestNavigate += (s, e) => { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); e.Handled = true; };
            link.Inlines.Add(new System.Windows.Documents.Run("created by gleemron  ·  ") { Foreground = new SolidColorBrush(Color.FromRgb(74, 74, 96)) });
            link.Inlines.Add(hl);
            panel.Children.Add(link);
        });
    }

    // ═══════════════════════════════════════
    //  SET EDITOR
    // ═══════════════════════════════════════
    private void OpenSetEditor(FlashcardSet? existing)
    {
        var win = new SetEditorWindow(existing) { Owner = this };
        if (win.ShowDialog() == true)
        {
            if (existing == null)
                _ds.Data.Sets.Insert(0, win.Result!);
            _ds.CheckAndAwardBadges();
            _ds.Save();
            if (_currentPage == "detail" && existing != null)
                ShowDetail(existing.Id);
            else
                ShowSets();
        }
    }

    // ═══════════════════════════════════════
    //  EVENT HANDLERS
    // ═══════════════════════════════════════
    private void NewSet_Click(object s, RoutedEventArgs e) => OpenSetEditor(null);

    private void QuickStudy_Click(object s, RoutedEventArgs e)
    {
        var set = _ds.Data.Sets.OrderByDescending(x => x.LastStudied).FirstOrDefault();
        if (set == null) { ShowMsg(L.CreateSetFirst); return; }
        _currentSetId = set.Id;
        NavSets.IsChecked = true;
        foreach (var nb in new[] { NavHome, NavBadges, NavStats, NavProfile }) nb.IsChecked = false;
        StartStudy(StudyMode.Flashcard);
    }

    private void BackToSets_Click(object s, RoutedEventArgs e) => ShowSets();

    private void EditSet_Click(object s, RoutedEventArgs e)
    {
        var set = _ds.Data.Sets.FirstOrDefault(x => x.Id == _currentSetId);
        if (set != null) OpenSetEditor(set);
    }

    private void DeleteSet_Click(object s, RoutedEventArgs e)
    {
        var set = _ds.Data.Sets.FirstOrDefault(x => x.Id == _currentSetId);
        if (set == null) return;
        if (MessageBox.Show(L.ConfirmDelete(set.Name), L.Confirm, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        { _ds.Data.Sets.Remove(set); _ds.Save(); ShowSets(); }
    }

    private void Fav_Click(object s, RoutedEventArgs e)
    {
        var set = _ds.Data.Sets.FirstOrDefault(x => x.Id == _currentSetId);
        if (set == null) return;
        set.IsFavorite    = !set.IsFavorite;
        _ds.Save();
        FavBtn.Content    = set.IsFavorite ? "★" : "☆";
        FavBtn.Foreground = set.IsFavorite
            ? new SolidColorBrush(Color.FromRgb(229, 192, 123))
            : new SolidColorBrush(Color.FromRgb(136, 136, 160));
    }

    private void StartFlash_Click(object s, RoutedEventArgs e) => StartStudy(StudyMode.Flashcard);
    private void StartLearn_Click(object s, RoutedEventArgs e) => StartStudy(StudyMode.Learn);
    private void StartTest_Click(object s,  RoutedEventArgs e) => StartStudy(StudyMode.Test);
    private void StartTimed_Click(object s, RoutedEventArgs e) => StartStudy(StudyMode.Timed);

    private void EndStudy_Click(object s, RoutedEventArgs e)
    {
        _timer?.Stop();
        if (!string.IsNullOrEmpty(_currentSetId)) ShowDetail(_currentSetId);
        else ShowHome();
    }

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

        // FIX: Space tuşu ile flashcard flip artık çalışıyor
        if (e.Key == Key.Space && (_studyMode == StudyMode.Flashcard || _studyMode == StudyMode.Timed))
        {
            if (StudyContent.Children.Count > 0)
            {
                // Container > cardBorder bul ve simüle et
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

    private void ExportData(object s, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "JSON|*.json", FileName = $"gleemlet_{DateTime.Now:yyyyMMdd}.json" };
        if (dlg.ShowDialog() == true)
        {
            System.IO.File.WriteAllText(dlg.FileName, Newtonsoft.Json.JsonConvert.SerializeObject(_ds.Data, Newtonsoft.Json.Formatting.Indented));
            ShowMsg(L.Exported);
        }
    }

    private void ImportData(object s, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "JSON|*.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var imported = Newtonsoft.Json.JsonConvert.DeserializeObject<AppData>(System.IO.File.ReadAllText(dlg.FileName));
            if (imported == null) return;
            foreach (var set in imported.Sets)
                if (!_ds.Data.Sets.Any(x => x.Id == set.Id)) _ds.Data.Sets.Add(set);
            _ds.Save();
            ShowSets();
            ShowMsg(L.ImportedSets(imported.Sets.Count));
        }
        catch { ShowMsg(L.ImportFailed); }
    }

    private void ClearData(object s, RoutedEventArgs e)
    {
        if (MessageBox.Show(L.DeleteAllWarning, L.AppTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        _ds.Data.Sets.Clear();
        _ds.Data.Sessions.Clear();
        _ds.Data.Profile = new();
        _ds.Save();
        ShowHome();
        UpdateSidebar();
    }

    // ═══════════════════════════════════════
    //  UI HELPERS
    // ═══════════════════════════════════════
    private UIElement MakeSetCard(FlashcardSet set, bool large = false)
    {
        double w = large ? 280 : 240;
        var card = new Border
        {
            Width = w, Margin = new Thickness(0, 0, 12, 12),
            Background      = new SolidColorBrush(Color.FromRgb(28, 28, 34)),
            BorderBrush     = new SolidColorBrush(set.IsFavorite ? Color.FromRgb(229, 192, 123) : Color.FromRgb(46, 46, 58)),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16), Cursor = Cursors.Hand
        };
        var inner  = new StackPanel();
        var header = new Grid();
        var nameText = new TextBlock { Text = set.Name, FontFamily = new FontFamily("Segoe UI"), FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 240)), TextWrapping = TextWrapping.Wrap };
        var catBadge = new Border { Background = new SolidColorBrush(Color.FromRgb(36, 36, 48)), CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2, 6, 2), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
        catBadge.Child = new TextBlock { Text = set.Category, FontFamily = new FontFamily("Segoe UI"), FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(79, 172, 130)) };
        header.Children.Add(nameText); header.Children.Add(catBadge);
        inner.Children.Add(header);
        inner.Children.Add(MakeText(L.Lang == AppLanguage.Turkish ? $"{set.Words.Count} kelime{(set.StudyCount > 0 ? L.StudiedX(set.StudyCount) : "")}" : $"{set.Words.Count} words{(set.StudyCount > 0 ? L.StudiedX(set.StudyCount) : "")}", 11, "#4A4A60", margin: new Thickness(0, 4, 0, 10)));

        var pbBg   = new Border { Background = new SolidColorBrush(Color.FromRgb(36, 36, 48)), Height = 4, CornerRadius = new CornerRadius(2) };
        var pbFill = new Border { Background = new SolidColorBrush(Color.FromRgb(79, 172, 130)), CornerRadius = new CornerRadius(2), HorizontalAlignment = HorizontalAlignment.Left };
        pbBg.Child = pbFill;
        inner.Children.Add(pbBg);
        Dispatcher.InvokeAsync(() => pbFill.Width = pbBg.ActualWidth * (set.Progress / 100.0), DispatcherPriority.Loaded);
        inner.Children.Add(MakeText(L.LearnedOf(set.LearnedCount, set.Words.Count), 10, "#4A4A60", margin: new Thickness(0, 4, 0, 14)));

        var modeRow = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var mode in new[] { StudyMode.Flashcard, StudyMode.Learn, StudyMode.Test })
        {
            var btn = new Button { Content = mode.ToIcon(), FontSize = 14, Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 0, 6, 0) };
            btn.Style = (Style)FindResource("GhostButton");
            var capturedMode = mode; var capturedId = set.Id;
            btn.Click += (s, e) => { e.Handled = true; _currentSetId = capturedId; StartStudy(capturedMode); };
            modeRow.Children.Add(btn);
        }
        inner.Children.Add(modeRow);
        card.Child = inner;
        card.MouseLeftButtonDown += (s, e) => ShowDetail(set.Id);
        card.MouseEnter += (s, e) => card.BorderBrush = new SolidColorBrush(Color.FromRgb(79, 172, 130));
        card.MouseLeave += (s, e) => card.BorderBrush = new SolidColorBrush(set.IsFavorite ? Color.FromRgb(229, 192, 123) : Color.FromRgb(46, 46, 58));
        return card;
    }

    private Border MakeCard(string bgKey, string borderKey) =>
        new()
        {
            Background      = ThemeService.B(bgKey),
            BorderBrush     = ThemeService.B(borderKey),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(24, 20, 24, 20), Margin = new Thickness(0, 0, 0, 24)
        };

    private FrameworkElement MakeSectionHeader(string title, string? btnText, Action? btnAction)
    {
        var g = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        g.Children.Add(MakeText(title, 14, "#8888A0", isBold: true));
        if (btnText != null)
        {
            var btn = new Button { Content = btnText, Style = (Style)FindResource("GhostButton"), HorizontalAlignment = HorizontalAlignment.Right, Padding = new Thickness(12, 6, 12, 6), FontSize = 12 };
            if (btnAction != null) btn.Click += (s, e) => btnAction();
            g.Children.Add(btn);
        }
        return g;
    }

    private FrameworkElement MakeEmptyState(string icon, string title, string sub, string btnText, Action btnAction)
    {
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 40, 0, 0) };
        sp.Children.Add(MakeText(icon, 48, "#2E2E3A", margin: new Thickness(0, 0, 0, 12)));
        sp.Children.Add(MakeText(title, 16, "#8888A0", isBold: true, margin: new Thickness(0, 0, 0, 6)));
        sp.Children.Add(MakeText(sub,   13, "#4A4A60",               margin: new Thickness(0, 0, 0, 16)));
        var btn = new Button { Content = btnText, Style = (Style)FindResource("PrimaryButton"), HorizontalAlignment = HorizontalAlignment.Center };
        btn.Click += (s, e) => btnAction();
        sp.Children.Add(btn);
        return sp;
    }

    private FrameworkElement MakeStat(string value, string label)
    {
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
        sp.Children.Add(new TextBlock { Text = value, FontFamily = new FontFamily("Segoe UI"), FontSize = 22, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(79, 172, 130)), HorizontalAlignment = HorizontalAlignment.Center });
        sp.Children.Add(new TextBlock { Text = label, FontFamily = new FontFamily("Segoe UI"), FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 160)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });
        return sp;
    }

    private FrameworkElement MakeStatBox(string value, string label, string color)
    {
        var b  = new Border
        {
            Background = ThemeService.B("Surface"),
            BorderBrush = ThemeService.B("Border"),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 0, 10, 0), MinWidth = 80
        };
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        sp.Children.Add(new TextBlock { Text = value, FontFamily = new FontFamily("Segoe UI"), FontSize = 20, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), HorizontalAlignment = HorizontalAlignment.Center });
        sp.Children.Add(new TextBlock { Text = label, FontFamily = new FontFamily("Segoe UI"), FontSize = 10, Foreground = ThemeService.B("Dim"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });
        b.Child = sp;
        return b;
    }

    private TextBlock MakeText(string text, double size, string? color, bool isBold = false, Thickness? margin = null) =>
        new()
        {
            Text        = text, FontFamily = new FontFamily("Segoe UI"), FontSize = size,
            FontWeight  = isBold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground  = color != null
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(color))
                : ThemeService.B("Text"),
            TextWrapping = TextWrapping.Wrap, Margin = margin ?? new Thickness(0)
        };

    private void AddSettingsCard(StackPanel parent, string title, Action<StackPanel> buildContent)
    {
        var card = new Border
        {
            Background = ThemeService.B("Surface"),
            BorderBrush = ThemeService.B("Border"),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10),
            Padding = new Thickness(20, 16, 20, 16), Margin = new Thickness(0, 0, 0, 14)
        };
        var sp   = new StackPanel();
        sp.Children.Add(MakeText(title, 14, null, isBold: true, margin: new Thickness(0, 0, 0, 14)));
        buildContent(sp);
        card.Child = sp;
        parent.Children.Add(card);
    }

    private void AddFormRow(StackPanel parent, string label, string value, Action<string> onSave)
    {
        parent.Children.Add(MakeText(label.ToUpper(), 10, "#8888A0", isBold: true, margin: new Thickness(0, 0, 0, 4)));
        var row = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var input = new TextBox { Style = (Style)Application.Current.Resources["DarkTextBox"], Text = value };
        var btn   = new Button  { Content = "Save", Style = (Style)FindResource("PrimaryButton"), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(8, 0, 0, 0) };
        btn.Click += (s, e) => onSave(input.Text.Trim());
        Grid.SetColumn(input, 0); Grid.SetColumn(btn, 1);
        row.Children.Add(input); row.Children.Add(btn);
        parent.Children.Add(row);
    }

    private void AddToggleRow(StackPanel parent, string name, string desc, bool value, Action<bool> onChanged)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        var sp  = new StackPanel();
        sp.Children.Add(MakeText(name, 13, "#E8E8F0", isBold: true));
        sp.Children.Add(MakeText(desc, 11, "#8888A0", margin: new Thickness(0, 2, 0, 0)));
        var cb = new CheckBox { IsChecked = value, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
        cb.Checked   += (s, e) => onChanged(true);
        cb.Unchecked += (s, e) => onChanged(false);
        row.Children.Add(sp); row.Children.Add(cb);
        parent.Children.Add(row);
    }

    private void ShowMsg(string msg) => MessageBox.Show(msg, L.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using GleemLet.Models;
using GleemLet.Services;

namespace GleemLet;

public partial class MainWindow
{
    // ═══════════════════════════════════════
    //  HOME PAGE
    // ═══════════════════════════════════════
    private void ShowHome()
    {
        _currentPage = "home";
        HideAllPages();
        PageHome.Visibility = Visibility.Visible;
        HomePanel.Children.Clear();

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
}

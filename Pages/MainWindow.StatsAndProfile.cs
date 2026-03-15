using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GleemLet.Models;
using GleemLet.Services;

namespace GleemLet;

public partial class MainWindow
{
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

        var p             = _ds.Data.Profile;
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
            var nameText     = MakeText(set.Name, 13, "#E8E8F0", isBold: true);
            var progressWrap = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 16, 0) };
            var pbBg         = new Border { Background = new SolidColorBrush(Color.FromRgb(36, 36, 48)), Height = 6, CornerRadius = new CornerRadius(3) };
            var pbFill       = new Border { Background = new SolidColorBrush(Color.FromRgb(79, 172, 130)), CornerRadius = new CornerRadius(3), HorizontalAlignment = HorizontalAlignment.Left };
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
                Background      = badge.Earned ? new SolidColorBrush(Color.FromRgb(26, 46, 36)) : new SolidColorBrush(Color.FromRgb(28, 28, 34)),
                BorderBrush     = badge.Earned ? new SolidColorBrush(Color.FromRgb(79, 172, 130)) : new SolidColorBrush(Color.FromRgb(46, 46, 58)),
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
            AddToggleRow(panel, L.SoundEffects, L.SoundDesc, p.SoundEnabled, val => { p.SoundEnabled = val; _ds.Save(); });
            AddToggleRow(panel, L.ShuffleDefault, L.ShuffleDesc, p.ShuffleDefault, val => { p.ShuffleDefault = val; _ds.Save(); });
        });

        AddSettingsCard(ProfilePanel, L.LanguageSection, panel =>
        {
            panel.Children.Add(MakeText(L.LanguageDesc, 12, "#8888A0", margin: new Thickness(0, 0, 0, 12)));
            var langRow = new StackPanel { Orientation = Orientation.Horizontal };
            var btnTR = new Button { Content = "🇹🇷  Türkçe", Style = (Style)FindResource(L.Lang == AppLanguage.Turkish ? "PrimaryButton" : "GhostButton"), Padding = new Thickness(20, 10, 20, 10), Margin = new Thickness(0, 0, 10, 0) };
            var btnEN = new Button { Content = "🇬🇧  English", Style = (Style)FindResource(L.Lang == AppLanguage.English ? "PrimaryButton" : "GhostButton"), Padding = new Thickness(20, 10, 20, 10) };
            btnTR.Click += (s, e) => { L.Lang = AppLanguage.Turkish; _ds.Data.Profile.Language = "tr"; _ds.Save(); RebuildUI(); };
            btnEN.Click += (s, e) => { L.Lang = AppLanguage.English; _ds.Data.Profile.Language = "en"; _ds.Save(); RebuildUI(); };
            langRow.Children.Add(btnTR); langRow.Children.Add(btnEN);
            panel.Children.Add(langRow);
        });

        AddSettingsCard(ProfilePanel, L.ThemeSection, panel =>
        {
            var p2  = _ds.Data.Profile;
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            var btnDark  = new Button { Content = L.ThemeDark,  Padding = new Thickness(20, 10, 20, 10), Margin = new Thickness(0, 0, 10, 0), Style = (Style)FindResource(p2.Theme == "dark"  ? "PrimaryButton" : "GhostButton") };
            var btnLight = new Button { Content = L.ThemeLight, Padding = new Thickness(20, 10, 20, 10), Style = (Style)FindResource(p2.Theme == "light" ? "PrimaryButton" : "GhostButton") };
            void ApplyAndSave(string theme) { p2.Theme = theme; _ds.Save(); ThemeService.Apply(theme, p2.AccentColor); RebuildUI(); }
            btnDark.Click  += (s, e) => ApplyAndSave("dark");
            btnLight.Click += (s, e) => ApplyAndSave("light");
            row.Children.Add(btnDark); row.Children.Add(btnLight);
            panel.Children.Add(row);

            panel.Children.Add(MakeText(L.AccentLabel, 11, "#8888A0", isBold: true, margin: new Thickness(0, 0, 0, 8)));
            var accentRow = new StackPanel { Orientation = Orientation.Horizontal };
            var accents = new[] { ("teal","#4FAC82"), ("purple","#9B59B6"), ("blue","#61AFEF"), ("orange","#E5A44A"), ("pink","#E06C9F"), ("red","#E06C75") };
            foreach (var (name, hex) in accents)
            {
                var col  = (Color)ColorConverter.ConvertFromString(hex);
                var circ = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(14), Background = new SolidColorBrush(col), Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand, BorderThickness = new Thickness(p2.AccentColor == name ? 3 : 0), BorderBrush = new SolidColorBrush(Colors.White) };
                var capturedName = name;
                circ.MouseLeftButtonDown += (s, e) => { p2.AccentColor = capturedName; _ds.Save(); ThemeService.Apply(p2.Theme, capturedName); RebuildUI(); };
                accentRow.Children.Add(circ);
            }
            panel.Children.Add(accentRow);
        });

        AddSettingsCard(ProfilePanel, L.AvatarSection, panel =>
        {
            panel.Children.Add(MakeText(L.AvatarDesc, 12, "#8888A0", margin: new Thickness(0, 0, 0, 12)));
            var p2 = _ds.Data.Profile;
            var avatars = new[] { "🎓","🦁","🐺","🦊","🐉","⚡","🌟","🔥","🎯","🧠","🚀","🎮" };
            var wrapAvatar = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (var av in avatars)
            {
                var capturedAv = av;
                var btn = new Border { Width = 44, Height = 44, CornerRadius = new CornerRadius(10), Margin = new Thickness(0, 0, 8, 8), Cursor = Cursors.Hand, Background = p2.Avatar == av ? new SolidColorBrush(Color.FromRgb(26, 46, 36)) : new SolidColorBrush(Color.FromRgb(36, 36, 48)), BorderBrush = p2.Avatar == av ? new SolidColorBrush(Color.FromRgb(79, 172, 130)) : new SolidColorBrush(Color.FromRgb(46, 46, 58)), BorderThickness = new Thickness(2) };
                btn.Child = new TextBlock { Text = av, FontSize = 22, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                btn.MouseLeftButtonDown += (s, e) => { p2.Avatar = capturedAv; _ds.Save(); RebuildUI(); };
                wrapAvatar.Children.Add(btn);
            }
            panel.Children.Add(wrapAvatar);
        });

        AddSettingsCard(ProfilePanel, L.DailyGoalSection, panel =>
        {
            panel.Children.Add(MakeText(L.DailyGoalDesc, 12, "#8888A0", margin: new Thickness(0, 0, 0, 12)));
            var p2 = _ds.Data.Profile;
            var goalLabel = MakeText(L.DailyGoalWords(p2.DailyGoalWords), 13, "#4FAC82", isBold: true, margin: new Thickness(0, 0, 0, 8));
            panel.Children.Add(goalLabel);
            var slider = new Slider { Minimum = 5, Maximum = 50, Value = p2.DailyGoalWords, TickFrequency = 5, IsSnapToTickEnabled = true };
            slider.ValueChanged += (s, e) => { p2.DailyGoalWords = (int)slider.Value; goalLabel.Text = L.DailyGoalWords(p2.DailyGoalWords); _ds.Save(); };
            panel.Children.Add(slider);
        });

        AddSettingsCard(ProfilePanel, L.DataSection, panel =>
        {
            var btnRow    = new StackPanel { Orientation = Orientation.Horizontal };
            var exportBtn = new Button { Content = L.ExportJSON,   Style = (Style)FindResource("GhostButton"),  Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(14, 8, 14, 8) };
            var importBtn = new Button { Content = L.ImportJSON,   Style = (Style)FindResource("GhostButton"),  Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(14, 8, 14, 8) };
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
            var link = new TextBlock { FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), FontSize = 13, Cursor = Cursors.Hand };
            var hl   = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run("emirodabas.dev")) { NavigateUri = new Uri("https://emir-odabas.github.io/emirodabas-dev/"), Foreground = new SolidColorBrush(Color.FromRgb(79, 172, 130)) };
            hl.RequestNavigate += (s, e) => { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); e.Handled = true; };
            link.Inlines.Add(new System.Windows.Documents.Run("created by gleemron  ·  ") { Foreground = new SolidColorBrush(Color.FromRgb(74, 74, 96)) });
            link.Inlines.Add(hl);
            panel.Children.Add(link);
        });
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
}

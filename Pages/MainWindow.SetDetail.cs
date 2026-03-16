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
    //  SET DETAIL
    // ═══════════════════════════════════════
    private void ShowDetail(string setId, string wordFilter = "")
    {
        _currentPage  = "detail";
        _currentSetId = setId;
        HideAllPages();
        PageDetail.Visibility = Visibility.Visible;
        var set = _ds.Data.Sets.FirstOrDefault(s => s.Id == setId);
        if (set == null) return;

        if (string.IsNullOrEmpty(wordFilter))
            DetailSearchBox.Text = "";

        DetailName.Text = set.Name;
        DetailMeta.Text = L.WordsCount(set.Words.Count, set.LearnedCount, set.Category, set.Progress);
        FavBtn.Content  = set.IsFavorite ? "★" : "☆";
        FavBtn.Foreground = set.IsFavorite
            ? new SolidColorBrush(Color.FromRgb(229, 192, 123))
            : new SolidColorBrush(Color.FromRgb(136, 136, 160));

        DetailWords.Children.Clear();

        var pb = new Border { Background = new SolidColorBrush(Color.FromRgb(28, 28, 34)), Height = 8, CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 0, 14) };
        var pf = new Border { Background = new SolidColorBrush(Color.FromRgb(79, 172, 130)), CornerRadius = new CornerRadius(4), HorizontalAlignment = HorizontalAlignment.Left };
        pb.Child = pf;
        DetailWords.Children.Add(pb);
        Dispatcher.InvokeAsync(() => pf.Width = pb.ActualWidth * (set.Progress / 100.0), DispatcherPriority.Loaded);

        var words = set.Words.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(wordFilter))
            words = words.Where(w =>
                    (w.En ?? "").Contains(wordFilter, StringComparison.OrdinalIgnoreCase) ||
                    (w.Tr ?? "").Contains(wordFilter, StringComparison.OrdinalIgnoreCase) ||
                    (w.Level ?? "").Contains(wordFilter, StringComparison.OrdinalIgnoreCase));

        foreach (var w in words)
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

            var levelBadge = BuildCefrBadge(w.Level);
            Grid.SetColumn(levelBadge, 3);

            var learned = new CheckBox
            {
                IsChecked = w.Learned, VerticalAlignment = VerticalAlignment.Center,
                ToolTip   = L.MarkLearned, Margin = new Thickness(8, 0, 0, 0)
            };
            var capturedId = w.Id;
            learned.Checked   += (s, e) => { var fw = set.Words.FirstOrDefault(x => x.Id == capturedId); if (fw != null) { fw.Learned = true;  _ds.Save(); } };
            learned.Unchecked += (s, e) => { var fw = set.Words.FirstOrDefault(x => x.Id == capturedId); if (fw != null) { fw.Learned = false; _ds.Save(); } };
            Grid.SetColumn(learned, 4);

            g.Children.Add(accent); g.Children.Add(enSp); g.Children.Add(trText);
            g.Children.Add(levelBadge); g.Children.Add(learned);
            row.Child = g;
            DetailWords.Children.Add(row);
        }

        if (!string.IsNullOrWhiteSpace(wordFilter) && DetailWords.Children.Count <= 1)
            DetailWords.Children.Add(MakeText(L.Lang == AppLanguage.Turkish ? "Kelime bulunamadı." : "No words found.", 13, "#4A4A60", margin: new Thickness(0, 10, 0, 0)));
    }

    private static Border BuildCefrBadge(string? level)
    {
        bool hasLevel = !string.IsNullOrEmpty(level);
        Color bg, fg;
        switch (level?.ToUpper())
        {
            case "A1": case "A2": bg = Color.FromRgb(26, 58, 38);  fg = Color.FromRgb(80, 200, 120);  break;
            case "B1":            bg = Color.FromRgb(56, 50, 20);  fg = Color.FromRgb(229, 192, 80);  break;
            case "B2":            bg = Color.FromRgb(60, 44, 16);  fg = Color.FromRgb(229, 150, 60);  break;
            case "C1":            bg = Color.FromRgb(58, 26, 30);  fg = Color.FromRgb(224, 108, 117); break;
            case "C2":            bg = Color.FromRgb(50, 20, 58);  fg = Color.FromRgb(198, 120, 240); break;
            default:              bg = Color.FromRgb(30, 30, 38);  fg = Color.FromRgb(136, 136, 160); break;
        }
        var badge = new Border
        {
            Background = new SolidColorBrush(bg), BorderBrush = new SolidColorBrush(fg),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(8, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center, MinWidth = 32, 
            Visibility = hasLevel ? Visibility.Visible : Visibility.Collapsed
        };
        if (hasLevel)
        {
            badge.Child = new TextBlock
            {
                Text = level!, FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 10, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(fg), HorizontalAlignment = HorizontalAlignment.Center
            };
        }
        return badge;
    }

    // ═══════════════════════════════════════
    //  SET DETAIL EVENT HANDLERS
    // ═══════════════════════════════════════
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
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GleemLet.Models;
using GleemLet.Services;
using GleemLet.Windows;
using MaterialDesignThemes.Wpf;

namespace GleemLet;

public partial class MainWindow
{
    // ═══════════════════════════════════════
    //  SET EDITOR
    // ═══════════════════════════════════════
    private void OpenSetEditor(FlashcardSet? existing)
    {
        var win = new SetEditorWindow(existing) { Owner = this };
        if (win.ShowDialog() == true)
        {
            if (existing == null) _ds.Data.Sets.Insert(0, win.Result!);
            _ds.CheckAndAwardBadges();
            _ds.Save();
            if (_currentPage == "detail" && existing != null) ShowDetail(existing.Id);
            else ShowSets();
        }
    }

    private void NewSet_Click(object s, RoutedEventArgs e) => OpenSetEditor(null);

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
            var btn = new Button
            {
                Content = new PackIcon { Kind = (PackIconKind)Enum.Parse(typeof(PackIconKind), mode.ToIcon()), Width = 16, Height = 16 },
                Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 8, 0),
                ToolTip = mode.ToLabel()
            };
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
        new() { Background = ThemeService.B(bgKey), BorderBrush = ThemeService.B(borderKey), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(24, 20, 24, 20), Margin = new Thickness(0, 0, 0, 24) };

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

    private FrameworkElement MakeEmptyState(PackIconKind icon, string title, string sub, string btnText, Action btnAction)
    {
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 40, 0, 0) };
        sp.Children.Add(new PackIcon { Kind = icon, Width = 48, Height = 48, Foreground = ThemeService.B("Surface2"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) });
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
        var b  = new Border { Background = ThemeService.B("Surface"), BorderBrush = ThemeService.B("Border"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 0, 10, 0), MinWidth = 80 };
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        sp.Children.Add(new TextBlock { Text = value, FontFamily = new FontFamily("Segoe UI"), FontSize = 20, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), HorizontalAlignment = HorizontalAlignment.Center });
        sp.Children.Add(new TextBlock { Text = label, FontFamily = new FontFamily("Segoe UI"), FontSize = 10, Foreground = ThemeService.B("Dim"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });
        b.Child = sp;
        return b;
    }

    private TextBlock MakeText(string text, double size, string? color, bool isBold = false, Thickness? margin = null) =>
        new()
        {
            Text = text, FontFamily = new FontFamily("Segoe UI"), FontSize = size,
            FontWeight   = isBold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground   = color != null ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) : ThemeService.B("Text"),
            TextWrapping = TextWrapping.Wrap, Margin = margin ?? new Thickness(0)
        };

    private void AddSettingsCard(StackPanel parent, string title, Action<StackPanel> buildContent)
    {
        var card = new Border { Background = ThemeService.B("Surface"), BorderBrush = ThemeService.B("Border"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(20, 16, 20, 16), Margin = new Thickness(0, 0, 0, 14) };
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
        var btn   = new Button  { Content = L.Save, Style = (Style)FindResource("PrimaryButton"), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(8, 0, 0, 0) };
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

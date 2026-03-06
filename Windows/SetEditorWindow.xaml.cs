using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GleemLet.Models;
using GleemLet.Services;

namespace GleemLet.Windows;

public partial class SetEditorWindow : Window
{
    public FlashcardSet? Result { get; private set; }
    private FlashcardSet? _editing;

    // Placeholder rengi
    private static readonly SolidColorBrush PlaceholderBrush = new(Color.FromRgb(74, 74, 96));
    private static readonly SolidColorBrush NormalBrush      = new(Color.FromRgb(200, 200, 216));

    public SetEditorWindow(FlashcardSet? existing = null)
    {
        InitializeComponent();
        _editing = existing;

        if (existing != null)
        {
            WindowTitle.Text = "Edit Set";
            SetName.Text     = existing.Name;
            SetDesc.Text     = existing.Description;

            foreach (ComboBoxItem item in CategoryBox.Items)
                if (item.Content?.ToString() == existing.Category)
                { item.IsSelected = true; break; }

            foreach (var w in existing.Words)
                AddRow(w.En, w.Tr, w.Example);
        }
        else
        {
            AddRow(); AddRow(); AddRow();
        }

        UpdateCount();
        Loaded += (s, e) => SetName.Focus();
    }

    private void AddRow(string en = "", string tr = "", string ex = "")
    {
        int idx = WordRows.Children.Count + 1;

        var grid = new Grid { Margin = new Thickness(0, 0, 0, 6), Tag = "row" };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });

        var numText = new TextBlock
        {
            Text = idx.ToString(), FontFamily = new FontFamily("Segoe UI"), FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(74, 74, 96)),
            VerticalAlignment = VerticalAlignment.Center
        };

        // FIX: MakeInput artık placeholder'ı gerçekten gösteriyor
        var enInput = MakeInput(en, "English word");
        var trInput = MakeInput(tr, "Meaning / translation");
        var exInput = MakeInput(ex, "Example sentence (optional)");

        var delBtn = new Button
        {
            Content = "✕", FontSize = 11, Width = 28, Height = 28,
            Style = (Style)Application.Current.Resources["IconButton"],
            VerticalAlignment = VerticalAlignment.Center
        };
        delBtn.Click += (s, e) => { WordRows.Children.Remove(grid); Reindex(); UpdateCount(); };

        Grid.SetColumn(numText, 0); Grid.SetColumn(enInput, 1); Grid.SetColumn(trInput, 2);
        Grid.SetColumn(exInput, 3); Grid.SetColumn(delBtn,  4);

        // FIX: Tab navigasyonu — artık Children[^4] gibi kırılgan index yerine doğrudan referans
        enInput.KeyDown += (s, ev) => { if (ev.Key == Key.Tab) { ev.Handled = true; trInput.Focus(); } };
        trInput.KeyDown += (s, ev) => { if (ev.Key == Key.Tab) { ev.Handled = true; exInput.Focus(); } };
        exInput.KeyDown += (s, ev) =>
        {
            if (ev.Key != Key.Tab) return;
            ev.Handled = true;
            AddRow();
            UpdateCount();

            // FIX: Yeni eklenen row'un enInput'una güvenli şekilde focus
            if (WordRows.Children.Count > 0 && WordRows.Children[^1] is Grid lastGrid
                && lastGrid.Children[1] is TextBox newInput)
            {
                // Placeholder gösteriliyorsa temizle
                ClearPlaceholder(newInput);
                newInput.Focus();
            }
        };

        grid.Children.Add(numText); grid.Children.Add(enInput); grid.Children.Add(trInput);
        grid.Children.Add(exInput); grid.Children.Add(delBtn);

        WordRows.Children.Add(grid);
        UpdateCount();
    }

    // FIX: Placeholder artık gerçekten çalışıyor
    private static TextBox MakeInput(string text, string placeholder)
    {
        bool isEmpty = string.IsNullOrEmpty(text);

        var tb = new TextBox
        {
            Style      = (Style)Application.Current.Resources["DarkTextBox"],
            Text       = isEmpty ? placeholder : text,
            Foreground = isEmpty ? PlaceholderBrush : NormalBrush,
            Padding    = new Thickness(8, 7, 8, 7),
            FontSize   = 12,
            Margin     = new Thickness(4, 0, 4, 0),
            Tag        = placeholder   // placeholder metnini tag'de sakla
        };

        tb.GotFocus  += (s, e) => ClearPlaceholder(tb);
        tb.LostFocus += (s, e) => RestorePlaceholder(tb);

        return tb;
    }

    private static void ClearPlaceholder(TextBox tb)
    {
        if (tb.Foreground == PlaceholderBrush && tb.Text == (string?)tb.Tag)
        {
            tb.Text       = "";
            tb.Foreground = NormalBrush;
        }
    }

    private static void RestorePlaceholder(TextBox tb)
    {
        if (string.IsNullOrEmpty(tb.Text))
        {
            tb.Text       = (string?)tb.Tag ?? "";
            tb.Foreground = PlaceholderBrush;
        }
    }

    private static string GetInputValue(TextBox tb)
    {
        // Placeholder rengi ise gerçek değer yok
        if (tb.Foreground == PlaceholderBrush) return "";
        return tb.Text.Trim();
    }

    private void Reindex()
    {
        int i = 1;
        foreach (Grid row in WordRows.Children)
            if (row.Children[0] is TextBlock tb) tb.Text = (i++).ToString();
    }

    private void UpdateCount()
    {
        int filled = WordRows.Children.OfType<Grid>()
            .Count(g => g.Children[1] is TextBox tb && !string.IsNullOrWhiteSpace(GetInputValue(tb)));
        WordCount.Text = $"{filled} word{(filled == 1 ? "" : "s")}";
    }

    private void AddRow_Click(object s, RoutedEventArgs e)
    {
        AddRow();
        if (WordRows.Parent is ScrollViewer sv) sv.ScrollToBottom();
        if (WordRows.Children.Count > 0 && WordRows.Children[^1] is Grid g && g.Children[1] is TextBox tb)
        {
            ClearPlaceholder(tb);
            tb.Focus();
        }
    }


    private void Save_Click(object s, RoutedEventArgs e)
    {
        var name = SetName.Text.Trim();
        if (string.IsNullOrEmpty(name)) { SetName.Focus(); return; }

        var words = new List<Flashcard>();
        foreach (Grid row in WordRows.Children)
        {
            var en = row.Children[1] is TextBox tb1 ? GetInputValue(tb1) : "";
            var tr = row.Children[2] is TextBox tb2 ? GetInputValue(tb2) : "";
            var ex = row.Children[3] is TextBox tb3 ? GetInputValue(tb3) : "";
            if (!string.IsNullOrEmpty(en) || !string.IsNullOrEmpty(tr))
                words.Add(new() { En = en, Tr = tr, Example = ex });
        }

        var cat = (CategoryBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "General";

        if (_editing != null)
        {
            _editing.Name        = name;
            _editing.Description = SetDesc.Text.Trim();
            _editing.Category    = cat;
            _editing.Words       = words;
            Result = _editing;
        }
        else
        {
            Result = new FlashcardSet
            {
                Name        = name,
                Description = SetDesc.Text.Trim(),
                Category    = cat,
                Words       = words
            };
        }

        DialogResult = true;
    }

    private void Cancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
    private void Window_KeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Escape) DialogResult = false; }
}

using System.Windows;
using System.Windows.Controls;
using GleemLet.Services;

namespace GleemLet;

public partial class MainWindow
{
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
                                   s.Words.Any(w => (w.En ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                                                    (w.Tr ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase)));

        var selectedTag = (CategoryFilter.SelectedItem as ComboBoxItem)?.Tag as string ?? "";
        if (!string.IsNullOrEmpty(selectedTag))
            sets = sets.Where(s => s.Category == selectedTag);

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
        if (SetsPanel == null) return;
        RenderSets(SearchBox.Text, (CategoryFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "");
    }

    private void Filter_Changed(object s, SelectionChangedEventArgs e)
    {
        if (SetsPanel == null) return;
        RenderSets(SearchBox.Text, (CategoryFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "");
    }

    private void DetailSearch_Changed(object s, TextChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentSetId))
            ShowDetail(_currentSetId, DetailSearchBox.Text);
    }
}

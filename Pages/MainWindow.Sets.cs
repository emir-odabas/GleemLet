using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GleemLet.Models;

namespace GleemLet;

public partial class MainWindow
{
    // ═══════════════════════════════════════
    //  MY SETS PAGE
    // ═══════════════════════════════════════
    private void ShowSets(string filter = "", string cat = "")
    {
        _currentPage = "sets";
        HideAllPages();
        PageSets.Visibility = Visibility.Visible;
        
        // Legacy rendering disabled. Logic moved to SetsViewModel.
        // RenderSets(filter, cat);
    }

    private void RenderSets(string filter = "", string cat = "")
    {
        // Body removed to fix build errors
    }

    private void Search_Changed(object s, TextChangedEventArgs e)
    {
        // Event logic moved to ViewModel binding
    }

    private void Filter_Changed(object s, SelectionChangedEventArgs e)
    {
        // Event logic moved to ViewModel binding
    }

    private void DetailSearch_Changed(object s, TextChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentSetId))
            ShowDetail(_currentSetId, DetailSearchBox.Text);
    }
}

using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        // Legacy logic moved to StatsViewModel
    }

    // ═══════════════════════════════════════
    //  BADGES PAGE
    // ═══════════════════════════════════════
    private void ShowBadges()
    {
        _currentPage = "badges";
        HideAllPages();
        PageBadges.Visibility = Visibility.Visible;
        // Legacy logic moved to BadgesViewModel
    }

    // ═══════════════════════════════════════
    //  PROFILE PAGE
    // ═══════════════════════════════════════
    private void ShowProfile()
    {
        _currentPage = "profile";
        HideAllPages();
        PageProfile.Visibility = Visibility.Visible;
        // Legacy logic moved to ProfileViewModel
    }

    // Legacy UI helpers (kept as requested but bodies removed if they fail build)
    /*
    private void RenderLegacyProfile() { ... }
    */

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

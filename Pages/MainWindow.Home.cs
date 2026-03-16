using System;
using System.Linq;
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
        
        // Legacy UI rendering disabled as it's replaced by HomeView.xaml
        // RenderHome() logic is now handled by HomeViewModel
    }

    private void RenderHome()
    {
        // Body removed to fix build errors while keeping method signature
    }
}

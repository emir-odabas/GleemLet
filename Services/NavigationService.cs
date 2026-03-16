using System.Windows;
using System.Windows.Controls;

namespace GleemLet.Services;

/// <summary>
/// Sayfalar arası geçişi yöneten servis.
/// MainWindow'a doğrudan bağımlılığı kaldırır.
/// </summary>
public class NavigationService
{
    private static NavigationService? _instance;
    public static NavigationService Instance => _instance ??= new NavigationService();

    // Navigation events
    public event Action<string>? PageChanged;
    public event Action<string, Models.StudyMode>? StudyRequested;
    public event Action<string>? DetailRequested;
    public event Action? NewSetRequested;
    public event Action? RebuildRequested;
    public event Action? AvatarChanged;
    public event Action? DailyGoalChanged;

    private string _currentPage = "home";
    public string CurrentPage => _currentPage;

    // Sayfa geçmişi (geri butonu için)
    private readonly Stack<string> _history = new();

    private NavigationService() { }

    /// <summary>
    /// Belirtilen sayfaya git.
    /// </summary>
    public void NavigateTo(string page, bool addToHistory = true)
    {
        if (_currentPage == page) return;

        if (addToHistory)
            _history.Push(_currentPage);

        _currentPage = page;
        PageChanged?.Invoke(page);
    }

    public void RequestStudy(string setId, Models.StudyMode mode)
    {
        StudyRequested?.Invoke(setId, mode);
    }
    
    public void RequestDetail(string setId)
    {
        DetailRequested?.Invoke(setId);
    }

    public void RequestNewSet()  => NewSetRequested?.Invoke();
    public void RequestRebuild() => RebuildRequested?.Invoke();

    public void NotifyAvatarChanged()    => AvatarChanged?.Invoke();
    public void NotifyDailyGoalChanged() => DailyGoalChanged?.Invoke();

    /// <summary>
    /// Bir önceki sayfaya dön.
    /// </summary>
    public void GoBack()
    {
        if (_history.Count == 0) return;
        _currentPage = _history.Pop();
        PageChanged?.Invoke(_currentPage);
    }

    public bool CanGoBack => _history.Count > 0;

    /// <summary>
    /// Geçmişi temizle.
    /// </summary>
    public void ClearHistory() => _history.Clear();
}

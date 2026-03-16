using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GleemLet.Services;

namespace GleemLet.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly NavigationService _nav = NavigationService.Instance;

    // Aktif ViewModel — UI buna bind olacak
    [ObservableProperty] private BaseViewModel _currentViewModel;

    // Sidebar state
    [ObservableProperty] private bool _isHomeActive    = true;
    [ObservableProperty] private bool _isSetsActive    = false;
    [ObservableProperty] private bool _isStatsActive   = false;
    [ObservableProperty] private bool _isBadgesActive  = false;
    [ObservableProperty] private bool _isProfileActive = false;

    // ViewModel'ler — lazy yüklenecek
    private HomeViewModel?    _homeVM;
    private SetsViewModel?    _setsVM;
    private StatsViewModel?   _statsVM;
    private BadgesViewModel?  _badgesVM;
    private ProfileViewModel? _profileVM;

    public MainViewModel()
    {
        _currentViewModel = new HomeViewModel();
        _homeVM = (HomeViewModel)_currentViewModel;

        // NavigationService'e abone ol
        _nav.PageChanged += OnPageChanged;
    }

    private void OnPageChanged(string page)
    {
        // Sidebar butonlarını güncelle
        IsHomeActive    = page == "home";
        IsSetsActive    = page == "sets";
        IsStatsActive   = page == "stats";
        IsBadgesActive  = page == "badges";
        IsProfileActive = page == "profile";

        // İlgili ViewModel'i yükle
        CurrentViewModel = page switch
        {
            "home"    => _homeVM    ??= new HomeViewModel(),
            "sets"    => _setsVM    ??= new SetsViewModel(),
            "stats"   => _statsVM   ??= new StatsViewModel(),
            "badges"  => _badgesVM  ??= new BadgesViewModel(),
            "profile" => _profileVM ??= new ProfileViewModel(),
            _         => _homeVM    ??= new HomeViewModel()
        };

        // Sayfayı her açtığında yenile
        if (CurrentViewModel is HomeViewModel hvm)    hvm.Load();
        if (CurrentViewModel is StatsViewModel stvm)  stvm.Load();
        if (CurrentViewModel is BadgesViewModel bdvm)  bdvm.Load();
    }

    // ── Navigation Commands ────────────────────────────────
    [RelayCommand] private void GoHome()    => _nav.NavigateTo("home");
    [RelayCommand] private void GoSets()    => _nav.NavigateTo("sets");
    [RelayCommand] private void GoStats()   => _nav.NavigateTo("stats");
    [RelayCommand] private void GoBadges()  => _nav.NavigateTo("badges");
    [RelayCommand] private void GoProfile() => _nav.NavigateTo("profile");
}

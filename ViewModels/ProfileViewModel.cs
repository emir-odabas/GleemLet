using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GleemLet.Models;
using GleemLet.Services;

namespace GleemLet.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly DataService _ds = DataService.Instance;

    [ObservableProperty] private string _name          = "";
    [ObservableProperty] private string _goal          = "";
    [ObservableProperty] private string _avatar        = "🎓";
    [ObservableProperty] private string _theme         = "dark";
    [ObservableProperty] private string _accentColor   = "teal";
    [ObservableProperty] private string _language      = "en";
    [ObservableProperty] private bool   _soundEnabled;
    [ObservableProperty] private bool   _shuffleDefault;
    [ObservableProperty] private int    _dailyGoalWords;

    public List<string> Avatars { get; } =
    [
        "🎓","🦁","🐺","🦊","🐉","⚡","🌟","🔥","🎯","🧠","🚀","🎮"
    ];

    // Localized Strings
    [ObservableProperty] private string _profileHeader = "";
    [ObservableProperty] private string _displayNameLabel = "";
    [ObservableProperty] private string _learningGoalLabel = "";
    [ObservableProperty] private string _saveLabel = "";
    [ObservableProperty] private string _preferencesHeader = "";
    [ObservableProperty] private string _soundEffectsLabel = "";
    [ObservableProperty] private string _soundDesc = "";
    [ObservableProperty] private string _lobbyMusicLabel = "";
    [ObservableProperty] private string _lobbyMusicDesc = "";
    [ObservableProperty] private string _shuffleDefaultLabel = "";
    [ObservableProperty] private string _shuffleDesc = "";
    [ObservableProperty] private string _languageHeader = "";
    [ObservableProperty] private string _languageDesc = "";
    [ObservableProperty] private string _themeHeader = "";
    [ObservableProperty] private string _avatarHeader = "";
    [ObservableProperty] private string _dailyGoalHeader = "";
    [ObservableProperty] private string _dailyGoalDesc = "";
    [ObservableProperty] private string _wordsPerDaySuffix = "";
    [ObservableProperty] private string _dataHeader = "";
    [ObservableProperty] private string _exportLabel = "";
    [ObservableProperty] private string _importLabel = "";
    [ObservableProperty] private string _clearLabel = "";

    public ProfileViewModel()
    {
        Title = "Profile";
        Load();
    }

    public void Load()
    {
        var p       = _ds.Data.Profile;
        Name          = p.Name;
        Goal          = p.Goal;
        Avatar        = p.Avatar;
        Theme         = p.Theme;
        AccentColor   = p.AccentColor;
        Language      = p.Language;
        SoundEnabled  = p.SoundEnabled;
        ShuffleDefault = p.ShuffleDefault;
        DailyGoalWords = p.DailyGoalWords;

        // Localized Strings
        ProfileHeader      = L.ProfileSection;
        DisplayNameLabel   = L.DisplayName;
        LearningGoalLabel  = L.LearningGoal;
        SaveLabel          = L.Save;
        PreferencesHeader  = L.Preferences;
        SoundEffectsLabel  = L.SoundEffects;
        SoundDesc          = L.SoundDesc;
        LobbyMusicLabel    = L.LobbyMusic;
        LobbyMusicDesc     = L.LobbyMusicDesc;
        ShuffleDefaultLabel = L.ShuffleDefault;
        ShuffleDesc        = L.ShuffleDesc;
        LanguageHeader     = L.LanguageSection;
        LanguageDesc       = L.LanguageDesc;
        ThemeHeader        = L.ThemeSection;
        AvatarHeader       = L.AvatarSection;
        DailyGoalHeader    = L.DailyGoalSection;
        DailyGoalDesc      = L.DailyGoalDesc;
        WordsPerDaySuffix  = L.Lang == AppLanguage.Turkish ? " kelime/gün" : " words per day";
        DataHeader         = L.DataSection;
        ExportLabel        = L.ExportJSON;
        ImportLabel        = L.ImportJSON;
        ClearLabel         = L.ClearAllData;
    }

    [RelayCommand]
    private void Save()
    {
        var p           = _ds.Data.Profile;
        
        bool goalUpdate = p.DailyGoalWords != DailyGoalWords || p.Name != Name || p.Goal != Goal;

        p.Name          = Name;
        p.Goal          = Goal;
        p.Avatar        = Avatar;
        p.Theme         = Theme;
        p.AccentColor   = AccentColor;
        p.Language      = Language;
        p.SoundEnabled  = SoundEnabled;
        p.ShuffleDefault = ShuffleDefault;
        p.DailyGoalWords = DailyGoalWords;
        _ds.Save();

        ThemeService.Apply(Theme, AccentColor);
        
        if (goalUpdate)
            NavigationService.Instance.NotifyDailyGoalChanged();
    }

    [RelayCommand]
    private void SetAvatar(string avatar)
    {
        Avatar = avatar;
        Save();
        NavigationService.Instance.NotifyAvatarChanged();
    }

    [RelayCommand]
    private void SetTheme(string theme)
    {
        Theme = theme;
        Save();
    }

    [RelayCommand]
    private void SetAccent(string accent)
    {
        AccentColor = accent;
        Save();
    }

    [RelayCommand]
    private void SetLanguage(string lang)
    {
        Language = lang;
        L.Lang   = lang == "tr" ? AppLanguage.Turkish : AppLanguage.English;
        
        var p = _ds.Data.Profile;
        p.Language = lang;
        _ds.Save();

        NavigationService.Instance.RequestRebuild();
    }

    [RelayCommand]
    private void ExportData()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter   = "JSON|*.json",
            FileName = $"gleemlet_{DateTime.Now:yyyyMMdd}.json"
        };
        if (dlg.ShowDialog() == true)
        {
            System.IO.File.WriteAllText(
                dlg.FileName,
                Newtonsoft.Json.JsonConvert.SerializeObject(_ds.Data, Newtonsoft.Json.Formatting.Indented));
        }
    }

    [RelayCommand]
    private void ImportData()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "JSON|*.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var imported = Newtonsoft.Json.JsonConvert.DeserializeObject<AppData>(
                System.IO.File.ReadAllText(dlg.FileName));
            if (imported == null) return;
            foreach (var set in imported.Sets)
                if (!_ds.Data.Sets.Any(x => x.Id == set.Id))
                    _ds.Data.Sets.Add(set);
            _ds.Save();
        }
        catch { }
    }

    [RelayCommand]
    private void ClearData()
    {
        _ds.Data.Sets.Clear();
        _ds.Data.Sessions.Clear();
        _ds.Data.Profile = new();
        _ds.Save();
        Load();
    }

    // ── Live Update Triggers ──────────────────────────────
    partial void OnNameChanged(string value)
    {
        _ds.Data.Profile.Name = value;
        NavigationService.Instance.NotifyDailyGoalChanged();
    }
    partial void OnGoalChanged(string value)
    {
        _ds.Data.Profile.Goal = value;
        NavigationService.Instance.NotifyDailyGoalChanged();
    }
    partial void OnDailyGoalWordsChanged(int value)
    {
        _ds.Data.Profile.DailyGoalWords = value;
        NavigationService.Instance.NotifyDailyGoalChanged();
    }
}

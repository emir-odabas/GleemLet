using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GleemLet.Models;
using GleemLet.Services;

namespace GleemLet.ViewModels;

public partial class SetsViewModel : BaseViewModel
{
    private readonly DataService _ds = DataService.Instance;

    [ObservableProperty] private string _searchText    = "";
    [ObservableProperty] private string _selectedCategory = "";
    [ObservableProperty] private List<FlashcardSet> _filteredSets = [];

    // Localized Strings
    [ObservableProperty] private string _setsTitle = "";
    [ObservableProperty] private string _searchPlaceholder = "";
    [ObservableProperty] private string _newSetLabel = "";
    [ObservableProperty] private string _allCategoriesLabel = "";

    public List<string> Categories { get; } =
    [
        "", "General", "Academic", "Business", "Daily", "Technical"
    ];

    public SetsViewModel()
    {
        Title = "My Sets";
        Load();
    }

    public void Load()
    {
        SetsTitle         = L.MySets;
        SearchPlaceholder = L.SearchPlaceholder;
        NewSetLabel       = L.NewSet;
        AllCategoriesLabel = L.AllCategories;
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)       => ApplyFilter();
    partial void OnSelectedCategoryChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var sets = _ds.Data.Sets.AsEnumerable();

        if (!string.IsNullOrEmpty(SearchText))
            sets = sets.Where(s =>
                s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                s.Words.Any(w =>
                    (w.En ?? "").Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (w.Tr ?? "").Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrEmpty(SelectedCategory))
            sets = sets.Where(s => s.Category == SelectedCategory);

        FilteredSets = sets.OrderByDescending(s => s.LastStudied ?? s.Created).ToList();
    }

    [RelayCommand]
    private void NewSet()
    {
        NavigationService.Instance.RequestNewSet();
    }

    [RelayCommand]
    private void StartFlashcards(FlashcardSet set)
    {
        NavigationService.Instance.RequestStudy(set.Id, StudyMode.Flashcard);
    }

    [RelayCommand]
    private void StartLearn(FlashcardSet set)
    {
        NavigationService.Instance.RequestStudy(set.Id, StudyMode.Learn);
    }

    [RelayCommand]
    private void StartTest(FlashcardSet set)
    {
        NavigationService.Instance.RequestStudy(set.Id, StudyMode.Test);
    }
}

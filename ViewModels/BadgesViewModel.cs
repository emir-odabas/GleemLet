using CommunityToolkit.Mvvm.ComponentModel;
using GleemLet.Models;
using GleemLet.Services;

namespace GleemLet.ViewModels;

public partial class BadgesViewModel : BaseViewModel
{
    private readonly DataService _ds = DataService.Instance;

    [ObservableProperty] private int _earnedCount;
    [ObservableProperty] private int _totalCount;

    public List<Badge> Badges { get; private set; } = [];

    public BadgesViewModel()
    {
        Title = "Badges";
        Load();
    }

    public void Load()
    {
        Badges      = _ds.Data.Badges;
        EarnedCount = Badges.Count(b => b.Earned);
        TotalCount  = Badges.Count;

        OnPropertyChanged(nameof(Badges));
    }
}

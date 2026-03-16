using CommunityToolkit.Mvvm.ComponentModel;

namespace GleemLet.ViewModels;

/// <summary>
/// Tüm ViewModel'lerin türeyeceği temel sınıf.
/// ObservableObject: PropertyChanged'ı otomatik yönetir.
/// </summary>
public abstract class BaseViewModel : ObservableObject
{
    private bool _isBusy;
    private string _title = "";

    /// <summary>
    /// Sayfa yüklenirken true olur, UI'da loading göstergesi için kullanılır.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// Sayfa başlığı.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}

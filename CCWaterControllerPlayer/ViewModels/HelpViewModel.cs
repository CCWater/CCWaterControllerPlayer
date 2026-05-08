using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CCWaterControllerPlayer.Services;

namespace CCWaterControllerPlayer.ViewModels;

public partial class HelpViewModel : ViewModelBase
{
    private readonly LocalizationService _localization;

    [ObservableProperty]
    private string _currentDocContent = string.Empty;

    [ObservableProperty]
    private string _currentDocKey = "quickstart";

    public HelpViewModel()
    {
        _localization = LocalizationService.Instance;
        LoadDoc("quickstart");
    }

    [RelayCommand]
    private void LoadDoc(string key)
    {
        CurrentDocKey = key;
        CurrentDocContent = _localization.GetDoc(key);
    }

    public void RefreshContent()
    {
        CurrentDocContent = _localization.GetDoc(CurrentDocKey);
    }
}

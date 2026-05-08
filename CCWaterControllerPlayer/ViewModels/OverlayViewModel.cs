using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CCWaterControllerPlayer.Models;
using CCWaterControllerPlayer.Services;
using Microsoft.Win32;

namespace CCWaterControllerPlayer.ViewModels;

public partial class OverlayViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;

    public Action? OnToggleOverlay;
    public Action? OnPinOverlay;
    public Action? OnUnpinOverlay;
    public Action? OnSettingsApplied;
    public Action? OnToggleImageOverlay;
    public Action? OnPinImageOverlay;
    public Action? OnUnpinImageOverlay;
    public Action<string>? OnSelectImage;
    public Func<bool>? GetIsOverlayVisible;
    public Func<bool>? GetIsOverlayPinned;
    public Func<bool>? GetIsImageOverlayVisible;
    public Func<bool>? GetIsImageOverlayPinned;

    [ObservableProperty]
    private bool _isOverlayVisible;

    [ObservableProperty]
    private bool _isOverlayPinned;

    [ObservableProperty]
    private int _overlayWidth;

    [ObservableProperty]
    private int _overlayHeight;

    [ObservableProperty]
    private float _overlayOpacity;

    [ObservableProperty]
    private bool _overlayShowRealtime;

    [ObservableProperty]
    private bool _overlayShowHistory;

    [ObservableProperty]
    private bool _overlayShowRecoilAnalysis;

    [ObservableProperty]
    private bool _isImageOverlayVisible;

    [ObservableProperty]
    private bool _isImageOverlayPinned;

    [ObservableProperty]
    private float _imageOverlayOpacity;

    [ObservableProperty]
    private string _imageOverlayPath = string.Empty;

    public OverlayViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadFromSettings();
    }

    public void RefreshState()
    {
        IsOverlayVisible = GetIsOverlayVisible?.Invoke() ?? false;
        IsOverlayPinned = GetIsOverlayPinned?.Invoke() ?? false;
        IsImageOverlayVisible = GetIsImageOverlayVisible?.Invoke() ?? false;
        IsImageOverlayPinned = GetIsImageOverlayPinned?.Invoke() ?? false;
    }

    private void LoadFromSettings()
    {
        var config = _settingsService.Settings.OverlayConfig;
        OverlayWidth = config.Width;
        OverlayHeight = config.Height;
        OverlayOpacity = config.Opacity;
        OverlayShowRealtime = config.ShowRealtime;
        OverlayShowHistory = config.ShowHistory;
        OverlayShowRecoilAnalysis = config.ShowRecoilAnalysis;

        var imgConfig = _settingsService.Settings.ImageOverlayConfig;
        ImageOverlayOpacity = imgConfig.Opacity;
        ImageOverlayPath = imgConfig.ImagePath;
    }

    [RelayCommand]
    private void ToggleOverlay()
    {
        OnToggleOverlay?.Invoke();
        IsOverlayVisible = GetIsOverlayVisible?.Invoke() ?? false;
    }

    [RelayCommand]
    private void PinOverlay()
    {
        OnPinOverlay?.Invoke();
        IsOverlayPinned = GetIsOverlayPinned?.Invoke() ?? false;
    }

    [RelayCommand]
    private void UnpinOverlay()
    {
        OnUnpinOverlay?.Invoke();
        IsOverlayPinned = GetIsOverlayPinned?.Invoke() ?? false;
    }

    [RelayCommand]
    private void ToggleImageOverlay()
    {
        OnToggleImageOverlay?.Invoke();
        IsImageOverlayVisible = GetIsImageOverlayVisible?.Invoke() ?? false;
    }

    [RelayCommand]
    private void PinImageOverlay()
    {
        OnPinImageOverlay?.Invoke();
        IsImageOverlayPinned = GetIsImageOverlayPinned?.Invoke() ?? false;
    }

    [RelayCommand]
    private void UnpinImageOverlay()
    {
        OnUnpinImageOverlay?.Invoke();
        IsImageOverlayPinned = GetIsImageOverlayPinned?.Invoke() ?? false;
    }

    [RelayCommand]
    private void BrowseImage()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.tiff|All Files|*.*",
            Title = "Select Recoil Pattern Image"
        };

        if (dialog.ShowDialog() == true)
        {
            ImageOverlayPath = dialog.FileName;
            OnSelectImage?.Invoke(dialog.FileName);
        }
    }

    [RelayCommand]
    private async Task SaveOverlaySettingsAsync()
    {
        var config = _settingsService.Settings.OverlayConfig;
        config.Width = OverlayWidth;
        config.Height = OverlayHeight;
        config.Opacity = OverlayOpacity;
        config.ShowRealtime = OverlayShowRealtime;
        config.ShowHistory = OverlayShowHistory;
        config.ShowRecoilAnalysis = OverlayShowRecoilAnalysis;

        var imgConfig = _settingsService.Settings.ImageOverlayConfig;
        imgConfig.Opacity = ImageOverlayOpacity;
        imgConfig.ImagePath = ImageOverlayPath;

        OnSettingsApplied?.Invoke();
        await _settingsService.SaveAsync();
    }
}

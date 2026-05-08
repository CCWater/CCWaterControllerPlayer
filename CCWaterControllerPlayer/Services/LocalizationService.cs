using System.ComponentModel;
using System.Globalization;
using CCWaterControllerPlayer.Resources.Localization;

namespace CCWaterControllerPlayer.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    private CultureInfo _currentCulture;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LangStrings Strings { get; private set; }

    public string CurrentLanguage => _currentCulture.TwoLetterISOLanguageName == "zh" ? "zh" : "en";

    public LocalizationService()
    {
        _currentCulture = DetectSystemLanguage();
        Strings = LoadStrings(_currentCulture);
    }

    public void SetLanguage(string langCode)
    {
        _currentCulture = langCode == "zh"
            ? new CultureInfo("zh-CN")
            : new CultureInfo("en-US");

        Strings = LoadStrings(_currentCulture);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Strings)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
    }

    private static CultureInfo DetectSystemLanguage()
    {
        var culture = CultureInfo.CurrentUICulture;
        if (culture.TwoLetterISOLanguageName == "zh")
            return new CultureInfo("zh-CN");
        return new CultureInfo("en-US");
    }

    private static LangStrings LoadStrings(CultureInfo culture)
    {
        if (culture.TwoLetterISOLanguageName == "zh")
            return new LangStrings_ZH();
        return new LangStrings_EN();
    }

    public string GetDoc(string docKey)
    {
        return Resources.Docs.DocsProvider.GetDoc(docKey, CurrentLanguage);
    }
}

using System.Windows;
using DKAerialView.Models;
using DKAerialView.Services;

namespace DKAerialView;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private AppSettings _settings = new();

    public SettingsWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        Loaded += SettingsWindow_Loaded;
    }

    private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _settings = await _settingsService.LoadAsync();
        GoogleKeyBox.Password = _settings.GoogleMapsApiKey;
        NaverKeyBox.Text = _settings.NaverClientId;
        KakaoKeyBox.Password = _settings.KakaoJavaScriptKey;
        OpenRouterKeyBox.Password = _settings.OpenRouterApiKey;
        OpenRouterModelBox.Text = _settings.OpenRouterModel;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.GoogleMapsApiKey = GoogleKeyBox.Password.Trim();
        _settings.NaverClientId = NaverKeyBox.Text.Trim();
        _settings.KakaoJavaScriptKey = KakaoKeyBox.Password.Trim();
        _settings.OpenRouterApiKey = OpenRouterKeyBox.Password.Trim();
        _settings.OpenRouterModel = OpenRouterModelBox.Text.Trim();
        await _settingsService.SaveAsync(_settings);
        DialogResult = true;
    }
}

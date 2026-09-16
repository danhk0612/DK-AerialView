using System.Windows;
using System.Windows.Controls;
using DKAerialView.Models;
using DKAerialView.Services;

namespace DKAerialView;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly OpenRouterImageService _openRouterImageService = new();
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
        OpenRouterPromptBox.Text = _settings.OpenRouterPrompt;
    }

    private async void LoadModels_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LoadModelsButton.IsEnabled = false;
            ModelCapabilityText.Text = "OpenRouter 이미지 모델 목록을 불러오는 중...";
            var models = await _openRouterImageService.GetEditingModelsAsync(OpenRouterKeyBox.Password.Trim());
            OpenRouterModelBox.ItemsSource = models;

            var currentModel = OpenRouterModelBox.Text.Trim();
            var match = models.FirstOrDefault(model => string.Equals(model.Id, currentModel, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                OpenRouterModelBox.SelectedItem = match;
                ModelCapabilityText.Text = match.CapabilitySummary;
            }
            else
            {
                ModelCapabilityText.Text = $"편집 가능한 이미지 모델 {models.Count}개를 불러왔습니다. 모델을 선택하세요.";
            }
        }
        catch (Exception ex)
        {
            ModelCapabilityText.Text = $"모델 목록 로드 실패: {ex.Message}";
        }
        finally
        {
            LoadModelsButton.IsEnabled = true;
        }
    }

    private void OpenRouterModelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OpenRouterModelBox.SelectedItem is OpenRouterImageModel model)
            ModelCapabilityText.Text = model.CapabilitySummary;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.GoogleMapsApiKey = GoogleKeyBox.Password.Trim();
        _settings.NaverClientId = NaverKeyBox.Text.Trim();
        _settings.KakaoJavaScriptKey = KakaoKeyBox.Password.Trim();
        _settings.OpenRouterApiKey = OpenRouterKeyBox.Password.Trim();
        _settings.OpenRouterModel = OpenRouterModelBox.SelectedItem is OpenRouterImageModel model
            ? model.Id
            : OpenRouterModelBox.Text.Trim();
        _settings.OpenRouterPrompt = OpenRouterPromptBox.Text.Trim();
        await _settingsService.SaveAsync(_settings);
        DialogResult = true;
    }
}

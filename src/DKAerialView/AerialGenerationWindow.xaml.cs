using System.Windows;
using System.Windows.Controls;
using DKAerialView.Models;

namespace DKAerialView;

public partial class AerialGenerationWindow : Window
{
    private readonly int _currentWidth;
    private readonly int _currentHeight;
    private bool _collectingRoadview;

    public AerialGenerationOptions? Options { get; private set; }

    public AerialGenerationWindow(int currentWidth, int currentHeight)
    {
        InitializeComponent();
        _currentWidth = currentWidth;
        _currentHeight = currentHeight;
        WidthBox.Text = currentWidth.ToString();
        HeightBox.Text = currentHeight.ToString();
    }

    private void UseCurrentOutputCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || WidthBox is null || HeightBox is null) return;

        var custom = UseCurrentOutputCheckBox.IsChecked != true;
        WidthBox.IsEnabled = custom;
        HeightBox.IsEnabled = custom;
        if (!custom)
        {
            WidthBox.Text = _currentWidth.ToString();
            HeightBox.Text = _currentHeight.ToString();
        }
    }

    private async void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (_collectingRoadview) return;

        var useCurrent = UseCurrentOutputCheckBox.IsChecked == true;
        if (!TryReadDimension(WidthBox.Text, out var width) || !TryReadDimension(HeightBox.Text, out var height))
        {
            MessageBox.Show(this, "출력 크기는 1 이상의 정수로 입력하세요.", "DK AerialView", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (useCurrent)
        {
            width = _currentWidth;
            height = _currentHeight;
        }

        var options = new AerialGenerationOptions
        {
            ViewAngle = ReadEnum<AerialViewAnglePreset>(AngleBox, AerialViewAnglePreset.StandardOblique),
            Direction = ReadEnum<AerialDirectionPreset>(DirectionBox, AerialDirectionPreset.Automatic),
            StructurePreservation = ReadEnum<StructurePreservationLevel>(PreservationBox, StructurePreservationLevel.High),
            RenderStyle = ReadEnum<AerialRenderStyle>(StyleBox, AerialRenderStyle.Realistic),
            TargetWidth = width,
            TargetHeight = height,
            UseKakaoRoadviewReferences = UseRoadviewCheckBox.IsChecked == true,
            RoadviewDistanceMeters = ReadIntTag(RoadviewDistanceBox, 100),
            RoadviewSearchRadiusMeters = ReadIntTag(RoadviewRadiusBox, 100),
            RoadviewTilt = 0,
            RoadviewZoom = 0
        };

        if (options.UseKakaoRoadviewReferences && Owner is MainWindow mainWindow)
        {
            try
            {
                _collectingRoadview = true;
                IsEnabled = false;
                Title = "AI 조감도 생성 - 카카오 로드뷰 참조 수집 중...";
                options.RoadviewReferences = await mainWindow.CollectKakaoRoadviewReferencesAsync(options);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"카카오 로드뷰 참조 수집에 실패했습니다. 항공사진만으로 계속 진행합니다.\n\n{ex.Message}",
                    "DK AerialView",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                options.RoadviewReferences = Array.Empty<byte[]>();
            }
            finally
            {
                IsEnabled = true;
                Title = "AI 조감도 생성";
                _collectingRoadview = false;
            }
        }

        Options = options;
        DialogResult = true;
    }

    private static bool TryReadDimension(string? text, out int value)
        => int.TryParse(text, out value) && value > 0;

    private static int ReadIntTag(ComboBox box, int fallback)
    {
        var tag = (box.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return int.TryParse(tag, out var value) ? value : fallback;
    }

    private static T ReadEnum<T>(ComboBox box, T fallback) where T : struct
    {
        var tag = (box.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return Enum.TryParse<T>(tag, true, out var value) ? value : fallback;
    }
}

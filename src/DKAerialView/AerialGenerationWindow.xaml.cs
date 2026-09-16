using System.Windows;
using System.Windows.Controls;
using DKAerialView.Models;

namespace DKAerialView;

public partial class AerialGenerationWindow : Window
{
    private readonly int _currentWidth;
    private readonly int _currentHeight;

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
        var custom = UseCurrentOutputCheckBox.IsChecked != true;
        WidthBox.IsEnabled = custom;
        HeightBox.IsEnabled = custom;
        if (!custom)
        {
            WidthBox.Text = _currentWidth.ToString();
            HeightBox.Text = _currentHeight.ToString();
        }
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
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

        Options = new AerialGenerationOptions
        {
            ViewAngle = ReadEnum<AerialViewAnglePreset>(AngleBox, AerialViewAnglePreset.StandardOblique),
            Direction = ReadEnum<AerialDirectionPreset>(DirectionBox, AerialDirectionPreset.Automatic),
            StructurePreservation = ReadEnum<StructurePreservationLevel>(PreservationBox, StructurePreservationLevel.High),
            RenderStyle = ReadEnum<AerialRenderStyle>(StyleBox, AerialRenderStyle.Realistic),
            TargetWidth = width,
            TargetHeight = height
        };

        DialogResult = true;
    }

    private static bool TryReadDimension(string? text, out int value)
        => int.TryParse(text, out value) && value > 0;

    private static T ReadEnum<T>(ComboBox box, T fallback) where T : struct
    {
        var tag = (box.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return Enum.TryParse<T>(tag, true, out var value) ? value : fallback;
    }
}

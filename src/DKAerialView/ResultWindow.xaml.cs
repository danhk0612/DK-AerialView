using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace DKAerialView;

public partial class ResultWindow : Window
{
    private readonly byte[] _resultBytes;

    public ResultWindow(byte[] sourceBytes, byte[] resultBytes)
    {
        InitializeComponent();
        _resultBytes = resultBytes;
        SourceImage.Source = LoadImage(sourceBytes);
        ResultImage.Source = LoadImage(resultBytes);
    }

    private static BitmapImage LoadImage(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private async void SaveResult_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "AI 조감도 결과 저장",
            Filter = "PNG 이미지 (*.png)|*.png|모든 파일 (*.*)|*.*",
            FileName = $"DK-AerialView-AI-{DateTime.Now:yyyyMMdd-HHmmss}.png",
            DefaultExt = ".png"
        };
        if (dialog.ShowDialog(this) != true) return;
        await File.WriteAllBytesAsync(dialog.FileName, _resultBytes);
    }
}

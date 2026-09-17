using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DKAerialView.Models;

namespace DKAerialView;

public partial class RoadviewReviewWindow : Window
{
    private readonly List<RoadviewReferenceItem> _items;

    public IReadOnlyList<RoadviewReferenceItem> SelectedReferences =>
        _items.Where(item => item.IsSelected).ToArray();

    public RoadviewReviewWindow(IReadOnlyList<RoadviewReferenceItem> items)
    {
        InitializeComponent();
        _items = items.Take(4).ToList();
        BuildCards();
        UpdateSelectionText();
    }

    private void BuildCards()
    {
        ItemsPanel.Children.Clear();

        foreach (var item in _items)
        {
            var image = new Image
            {
                Source = CreateBitmap(item.ImageBytes),
                Stretch = Stretch.Uniform,
                Height = 245,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var checkBox = new CheckBox
            {
                Content = $"사용 · {item.DisplayLabel}",
                IsChecked = item.IsSelected,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 2, 0, 0)
            };
            checkBox.Checked += (_, _) =>
            {
                item.IsSelected = true;
                UpdateSelectionText();
            };
            checkBox.Unchecked += (_, _) =>
            {
                item.IsSelected = false;
                UpdateSelectionText();
            };

            var content = new StackPanel();
            content.Children.Add(image);
            content.Children.Add(checkBox);

            var card = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(210, 210, 210)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Margin = new Thickness(6),
                Child = content
            };

            ItemsPanel.Children.Add(card);
        }
    }

    private static BitmapImage CreateBitmap(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void UpdateSelectionText()
    {
        if (SelectionText is null) return;
        SelectionText.Text = $"로드뷰 {_items.Count}장 수집 · {_items.Count(item => item.IsSelected)}장 선택";
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void AerialOnly_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items)
            item.IsSelected = false;
        DialogResult = true;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DKAerialView.Models;

namespace DKAerialView;

public partial class RoadviewReviewWindow : Window
{
    private readonly Func<IProgress<RoadviewCollectionProgress>, CancellationToken, Task<IReadOnlyList<RoadviewReferenceItem>>> _collector;
    private readonly List<RoadviewReferenceItem> _items = new();
    private readonly CancellationTokenSource _collectionCts = new();
    private bool _collectionCompleted;
    private bool _decisionMade;

    public IReadOnlyList<RoadviewReferenceItem> SelectedReferences =>
        _items.Where(item => item.IsSelected).ToArray();

    public RoadviewReviewWindow(
        Func<IProgress<RoadviewCollectionProgress>, CancellationToken, Task<IReadOnlyList<RoadviewReferenceItem>>> collector)
    {
        InitializeComponent();
        _collector = collector;
        Loaded += RoadviewReviewWindow_Loaded;
        Closed += (_, _) => _collectionCts.Cancel();
        UpdateSelectionText();
    }

    private async void RoadviewReviewWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var progress = new Progress<RoadviewCollectionProgress>(ApplyProgress);

        try
        {
            var collected = await _collector(progress, _collectionCts.Token);
            if (_decisionMade) return;

            foreach (var item in collected)
            {
                if (!_items.Any(existing => IsSameReference(existing, item)))
                    AddReferenceCard(item);
            }

            _collectionCompleted = true;
            CollectionStatusText.Text = collected.Count > 0
                ? $"수집 완료 · 서로 다른 로드뷰 {collected.Count}장 확보"
                : "수집 완료 · 사용할 수 있는 로드뷰를 찾지 못했습니다.";
            ProgressDetailText.Text = collected.Count > 0
                ? "이미지를 확인한 뒤 사용할 항목만 체크하고 생성하세요."
                : "항공사진만 진행하거나 취소할 수 있습니다.";
            CollectionProgressBar.Value = CollectionProgressBar.Maximum;
            AddLog($"[완료] 로드뷰 {collected.Count}장 확보");
            UpdateSelectionText();
        }
        catch (OperationCanceledException)
        {
            if (_decisionMade) return;
            CollectionStatusText.Text = "로드뷰 수집이 취소되었습니다.";
            ProgressDetailText.Text = "현재 확보된 이미지만 사용할 수 있습니다.";
            AddLog("[취소] 로드뷰 수집 중단");
        }
        catch (Exception ex)
        {
            if (_decisionMade) return;
            CollectionStatusText.Text = "로드뷰 수집 중 오류가 발생했습니다.";
            ProgressDetailText.Text = ex.Message;
            AddLog($"[오류] {ex.Message}");
        }
        finally
        {
            _collectionCompleted = true;
            UpdateSelectionText();
        }
    }

    private void ApplyProgress(RoadviewCollectionProgress progress)
    {
        if (_decisionMade) return;

        if (progress.TotalAttempts > 0)
        {
            CollectionProgressBar.Maximum = progress.TotalAttempts;
            CollectionProgressBar.Value = Math.Min(progress.AttemptIndex, progress.TotalAttempts);
        }
        else
        {
            CollectionProgressBar.Maximum = 1;
            CollectionProgressBar.Value = 0;
        }

        CollectionStatusText.Text = progress.IsCompleted
            ? "카카오 로드뷰 수집 완료"
            : $"수집 중 · {progress.CurrentCandidate}";
        CounterText.Text = $"성공 {progress.SuccessCount} · 중복 {progress.DuplicateCount} · 실패 {progress.FailureCount}";
        ProgressDetailText.Text = progress.TotalAttempts > 0
            ? $"후보 {progress.AttemptIndex}/{progress.TotalAttempts} · {progress.Message}"
            : progress.Message;

        if (!string.IsNullOrWhiteSpace(progress.Message))
            AddLog(progress.Message);

        if (progress.AddedReference is not null &&
            !_items.Any(existing => IsSameReference(existing, progress.AddedReference)))
        {
            AddReferenceCard(progress.AddedReference);
        }

        if (progress.IsCompleted)
            _collectionCompleted = true;

        UpdateSelectionText();
    }

    private void AddReferenceCard(RoadviewReferenceItem item)
    {
        if (_items.Count >= 4) return;
        _items.Add(item);

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
        UpdateSelectionText();
    }

    private void AddLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        LogList.Items.Add($"{DateTime.Now:HH:mm:ss}  {message}");
        while (LogList.Items.Count > 40)
            LogList.Items.RemoveAt(0);

        if (LogList.Items.Count > 0)
            LogList.ScrollIntoView(LogList.Items[^1]);
    }

    private static bool IsSameReference(RoadviewReferenceItem left, RoadviewReferenceItem right)
    {
        if (left.PanoId != 0 && right.PanoId != 0)
            return left.PanoId == right.PanoId;

        return ReferenceEquals(left, right);
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
        if (SelectionText is null || ConfirmButton is null) return;

        var selectedCount = _items.Count(item => item.IsSelected);
        var suffix = _collectionCompleted ? " · 수집 완료" : " · 수집 진행 중";
        SelectionText.Text = $"로드뷰 {_items.Count}장 수집 · {selectedCount}장 선택{suffix}";
        ConfirmButton.IsEnabled = selectedCount > 0;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _decisionMade = true;
        _collectionCts.Cancel();
        DialogResult = false;
    }

    private void AerialOnly_Click(object sender, RoutedEventArgs e)
    {
        _decisionMade = true;
        _collectionCts.Cancel();
        foreach (var item in _items)
            item.IsSelected = false;
        DialogResult = true;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (!_items.Any(item => item.IsSelected)) return;

        _decisionMade = true;
        _collectionCts.Cancel();
        DialogResult = true;
    }
}

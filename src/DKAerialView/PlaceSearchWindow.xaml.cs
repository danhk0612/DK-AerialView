using System.Windows;
using System.Windows.Input;
using DKAerialView.Models;

namespace DKAerialView;

public partial class PlaceSearchWindow : Window
{
    public PlaceSearchWindow(string query, IReadOnlyList<PlaceSearchItem> results)
    {
        InitializeComponent();
        TitleText.Text = $"'{query}' 검색 결과 {results.Count}건";
        ResultsGrid.ItemsSource = results;
        if (results.Count > 0)
            ResultsGrid.SelectedIndex = 0;
    }

    public PlaceSearchItem? SelectedPlace { get; private set; }

    private void Select_Click(object sender, RoutedEventArgs e) => ConfirmSelection();

    private void ResultsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResultsGrid.SelectedItem is PlaceSearchItem)
            ConfirmSelection();
    }

    private void ConfirmSelection()
    {
        if (ResultsGrid.SelectedItem is not PlaceSearchItem selected)
            return;

        SelectedPlace = selected;
        DialogResult = true;
    }
}

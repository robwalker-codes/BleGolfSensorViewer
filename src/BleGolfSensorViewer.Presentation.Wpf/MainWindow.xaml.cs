using System.Collections.Specialized;
using System.Windows;
using BleGolfSensorViewer.Presentation.Wpf.ViewModels;

namespace BleGolfSensorViewer.Presentation.Wpf;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.Measurements.CollectionChanged += OnMeasurementsCollectionChanged;
        Unloaded += (_, _) => _viewModel.Measurements.CollectionChanged -= OnMeasurementsCollectionChanged;
    }

    private void OnMeasurementsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is { Count: > 0 })
        {
            var lastItem = e.NewItems[^1];
            MeasurementsGrid.ScrollIntoView(lastItem);
        }
    }

    private void LogBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        LogBox.CaretIndex = LogBox.Text.Length;
        LogBox.ScrollToEnd();
    }
}

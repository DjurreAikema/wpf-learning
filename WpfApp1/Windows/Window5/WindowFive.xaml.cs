using System.Reactive.Subjects;
using System.Windows;
using WpfApp1.Windows.Window5.Events;

namespace WpfApp1.Windows.Window5;

public partial class WindowFive : Window
{
    public WindowFiveViewModel ViewModel { get; } = new();
    public Subject<bool> TriggerDispose { get; set; } = new();

    // --- Constructor
    public WindowFive()
    {
        InitializeComponent();

        // Dispose of all subscriptions when the window is closed
        Closing += (_, _) =>
        {
            ViewModel.Dispose();
            TriggerDispose.OnNext(true);
        };
    }

    private void SnacksGrid_SnackSelected(object? sender, SnackSelectedEventArgs snackSelectedEventArgs)
    {
        ViewModel.SelectedSnackChanged.OnNext(snackSelectedEventArgs.SelectedSnack);
    }
}
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using WpfApp1.Shared.Classes;

namespace WpfApp1.Windows.Window8.UI;

public partial class SnacksGridEight
{
    // --- Dependency Properties
    public static readonly DependencyProperty SnacksObsProperty = DependencyProperty.Register(
        nameof(SnacksObs), typeof(IObservable<IEnumerable<SnackV2>>), typeof(SnacksGridEight),
        new PropertyMetadata(null, (d, _) =>
        {
            if (d is not SnacksGridEight c) return;
            c.Disposables.Add(c.SnacksObs.Subscribe(snacks =>
            {
                c.Snacks = new ObservableCollection<SnackV2>(snacks);
            }));
        }));

    public IObservable<IEnumerable<SnackV2>> SnacksObs
    {
        get => (IObservable<IEnumerable<SnackV2>>) GetValue(SnacksObsProperty);
        set => SetValue(SnacksObsProperty, value);
    }

    // --- Events
    public event Action<SnackV2>? SnackSelected;
    public event Action? AddSnack;
    public event Action? Reload;


    // --- Internal Properties
    [ObservableProperty] private ObservableCollection<SnackV2>? _snacks;

    // --- Constructor
    public SnacksGridEight()
    {
        InitializeComponent();

        SnacksDataGrid.SelectionChanged += (_, _) =>
        {
            if (SnacksDataGrid.SelectedItem is SnackV2 selectedSnack) SnackSelected?.Invoke(selectedSnack);
        };
    }

    private void New_OnClick(object sender, RoutedEventArgs e) => AddSnack?.Invoke();

    private void Reload_OnClick(object sender, RoutedEventArgs e) => Reload?.Invoke();
}
using System.Reactive;
using System.Reactive.Subjects;
using System.Windows;
using WpfApp2.Shared.Classes;

namespace WpfApp2.Views.StamData.Countries;

public partial class CountriesView
{
    public CountriesViewModel Vm { get; } = new();
    public Subject<bool> TriggerDispose { get; set; } = new();

    public CountriesView()
    {
        InitializeComponent();

        // Dispose of all subscriptions when the window is closed
        Unloaded += (_, _) =>
        {
            Vm.Dispose();
            TriggerDispose.OnNext(true);
        };
    }

    // --- List
    private void OnCountrySelected(Country obj) => Vm.SelectedChanged.OnNext(obj);

    private void OnAddCountry() => Vm.SelectedChanged.OnNext(new Country());

    private void OnReloadCountries() => Vm.Reload.OnNext(Unit.Default);

    // --- Details
    private void OnCountrySaved(Country obj)
    {
        Console.WriteLine($"Saving: {obj.Name}, ID: {obj.Id}");

        if (obj.Id is 0 or null)
            Vm.Create.OnNext(obj);
        else
            Vm.Update.OnNext(obj);
    }

    private void OnCountryDeleted(int id) => Vm.Delete.OnNext(id);

    // --- Internal
    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        // Navigate back to Stamdata
        var mainWindow = (MainWindow) Application.Current.MainWindow;
        var navigationService = mainWindow.GetNavigationService();
        navigationService.NavigateBack();
    }
}
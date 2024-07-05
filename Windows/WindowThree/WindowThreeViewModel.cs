﻿using System.Reactive.Linq;
using System.Reactive.Subjects;
using WpfApp1.Classes;

namespace WpfApp1.Windows.WindowThree;

public record WindowThreeState
{
    public List<Snack> Snacks { get; set; } = [];
    public Snack SelectedSnack { get; set; } = new();
    public bool Loading { get; set; } = true;
}

public class WindowThreeViewModel
{
    // --- State
    private readonly BehaviorSubject<WindowThreeState> _stateSubject = new(new WindowThreeState());
    private IObservable<WindowThreeState> State => _stateSubject.AsObservable();


    // --- Selectors
    public IObservable<List<Snack>> Snacks => State.Select(state => state.Snacks);
    public IObservable<Snack> SelectedSnack => State.Select(state => state.SelectedSnack);
    public IObservable<bool> Loading => State.Select(state => state.Loading);


    // --- Sources
    public Subject<Snack> SelectedSnackChanged = new();
    private IObservable<List<Snack>> SnacksLoaded => GetSnacks();

    // --- Reducers
    public WindowThreeViewModel()
    {
        // SelectedSnackChanged reducer
        SelectedSnackChanged.Subscribe(snack => { _stateSubject.OnNext(_stateSubject.Value with {SelectedSnack = snack}); });

        // SnacksLoaded reducer
        SnacksLoaded.Subscribe(snacks =>
        {
            _stateSubject.OnNext(_stateSubject.Value with
            {
                Snacks = snacks,
                Loading = false
            });
        });
    }

    // --- Functions
    private IObservable<List<Snack>> GetSnacks()
    {
        return Observable.Return(new List<Snack>
        {
            new() {Name = "Doritos", Price = 1.50, Quantity = 4},
            new() {Name = "Lays", Price = 1.00, Quantity = 3},
            new() {Name = "Pringles", Price = 2.00, Quantity = 2},
            new() {Name = "Cheetos", Price = 1.25, Quantity = 5},
            new() {Name = "Ruffles", Price = 1.75, Quantity = 1},
            new() {Name = "Tostitos", Price = 1.50, Quantity = 6},
            new() {Name = "Sun Chips", Price = 1.25, Quantity = 7},
        });
    }
}
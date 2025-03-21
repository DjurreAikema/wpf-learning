using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using WpfApp1.Shared.Abstract;
using WpfApp1.Shared.Classes;
using WpfApp1.Shared.DataAccess;
using WpfApp1.Shared.ExtensionMethods;
using WpfApp1.Shared.FormBuilder;

namespace WpfApp1.Windows.Window8;

public record WindowEightState : BaseState<WindowEightState>
{
    public List<SnackV2> Snacks { get; init; } = [];
    public SnackV2? SelectedSnack { get; init; }
    public FormGroup Form { get; init; }

    public override WindowEightState WithInProgress(bool inProgress) =>
        this with {InProgress = inProgress};
}

public class WindowEightViewModel : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly SnackServiceV2 _snackService;

    // --- State
    private readonly BehaviorSubject<WindowEightState> _stateSubject;
    private IObservable<WindowEightState> StateObs => _stateSubject.AsObservable();

    // --- Selectors
    public IObservable<List<SnackV2>> SnacksObs => StateObs.Select(state => state.Snacks);
    public IObservable<SnackV2?> SelectedSnackObs => StateObs.Select(state => state.SelectedSnack);
    public IObservable<bool> LoadingObs => StateObs.Select(state => state.Loading);
    public IObservable<FormGroup> FormObs => StateObs.Select(state => state.Form);
    public IObservable<bool> FormValidObs => StateObs.Select(state => state.Form?.IsValid ?? false);
    public IObservable<bool> FormDirtyObs => StateObs.Select(state => state.Form?.IsDirty ?? false);

    // --- Notifications
    private readonly Subject<NotificationMessage> _notifications = new();
    public IObservable<NotificationMessage> NotificationsObs => _notifications.AsObservable();

    // --- Track original snack states for error recovery
    private SnackV2? _originalBeforeOperation;

    // --- Sources
    public readonly Subject<SnackV2> SelectedSnackChanged = new();
    public readonly Subject<SnackV2> Create = new();
    public readonly Subject<SnackV2> Update = new();
    public readonly Subject<Unit> Reload = new();
    public readonly Subject<int> Delete = new();
    public readonly Subject<Unit> SaveForm = new();

    // Load
    private IObservable<List<SnackV2>?> SnacksLoadedObs =>
        Reload.StartWith(Unit.Default)
            .ExecuteAsyncOperation(
                _stateSubject,
                _ => _snackService.GetAllSnacksAsync(),
                _notifications, "Snacks loaded successfully.", e => $"Error loading snacks: {e.Message}", []
            );

    // Create
    private IObservable<SnackV2?> SnackCreatedObs => Create
        .Do(snack => _originalBeforeOperation = new SnackV2(snack))
        .ExecuteAsyncOperation(
            _stateSubject,
            obj => _snackService.AddSnackAsync(obj),
            _notifications, "Snack added successfully.", e => $"Error creating snack: {e.Message}"
        );

    // Update
    private IObservable<SnackV2?> SnackUpdatedObs => Update
        .Do(snack => _originalBeforeOperation = new SnackV2(snack))
        .ExecuteAsyncOperation(
            _stateSubject,
            obj => _snackService.UpdateSnackAsync(obj),
            _notifications, "Snack updated successfully.", e => $"Error updating snack: {e.Message}"
        );

    // Delete
    private IObservable<SnackV2?> SnackDeletedObs => Delete
        .ExecuteAsyncOperation(
            _stateSubject,
            id => _snackService.DeleteSnackAsync(id),
            _notifications, "Snack deleted successfully.", e => $"Error deleting snack: {e.Message}"
        );

    // --- Form handling
    private IObservable<Unit> FormSubmittedObs => SaveForm
        .Where(_ => _stateSubject.Value.Form.IsValid)
        .Select(_ =>
        {
            var snack = new SnackV2();

            // Get the existing ID if we're editing
            if (_stateSubject.Value.SelectedSnack != null)
            {
                snack.Id = _stateSubject.Value.SelectedSnack.Id;
            }

            // Map form values to the model
            FormBuilder.MapToModel(_stateSubject.Value.Form, snack);

            if (snack.Id is 0 or null)
                Create.OnNext(snack);
            else
                Update.OnNext(snack);

            return Unit.Default;
        });

    // --- Constructor
    public WindowEightViewModel()
    {
        // Initialize with an empty form
        var initialForm = FormBuilder.FromModel(new SnackV2());
        _stateSubject = new BehaviorSubject<WindowEightState>(new WindowEightState {Form = initialForm});

        _snackService = new SnackServiceV2
        {
            SimulateFailures = true,
            FailureProbability = 0.3,
            FailureProbabilityOnLoad = 0.3
        };

        // SelectedSnackChanged reducer
        _disposables.Add(SelectedSnackChanged
            .ObserveOnCurrentSynchronizationContext()
            .Subscribe(
                snack =>
                {
                    // Update the form with the selected snack's values
                    if (snack != null)
                    {
                        FormModelBuilder.UpdateFormFromModel(_stateSubject.Value.Form, snack);
                    }
                    else
                    {
                        // Reset form with an empty snack
                        FormModelBuilder.UpdateFormFromModel(_stateSubject.Value.Form, new SnackV2());
                    }

                    _stateSubject.OnNext(_stateSubject.Value with
                    {
                        SelectedSnack = snack
                    });
                },
                error => Console.WriteLine($"Unexpected error in SelectedSnackChanged: {error.Message}")
            ));

        // SnacksLoaded reducer
        _disposables.Add(SnacksLoadedObs
            .ObserveOnCurrentSynchronizationContext()
            .Subscribe(snacks =>
                {
                    if (snacks is null) return;

                    _stateSubject.OnNext(_stateSubject.Value with
                    {
                        Snacks = [..snacks],
                        Loading = false
                    });
                },
                error =>
                {
                    Console.WriteLine($"Unhandled error in SnacksLoaded reducer: {error.Message}");
                    _stateSubject.OnNext(_stateSubject.Value with
                    {
                        Loading = false
                    });
                }));

        // SnackCreated reducer
        _disposables.Add(SnackCreatedObs
            .ObserveOnCurrentSynchronizationContext()
            .Subscribe(snack =>
                {
                    if (snack is null)
                    {
                        _stateSubject.OnNext(_stateSubject.Value with
                        {
                            SelectedSnack = _originalBeforeOperation,
                            InProgress = false
                        });

                        // Restore form to original values
                        if (_originalBeforeOperation != null)
                        {
                            FormModelBuilder.UpdateFormFromModel(_stateSubject.Value.Form, _originalBeforeOperation);
                        }

                        return;
                    }

                    var snacks = new List<SnackV2>(_stateSubject.Value.Snacks) {snack};

                    _stateSubject.OnNext(_stateSubject.Value with
                    {
                        Snacks = snacks,
                        SelectedSnack = snack,
                        InProgress = false
                    });

                    // Reset form dirty state
                    _stateSubject.Value.Form.Reset();
                    FormModelBuilder.UpdateFormFromModel(_stateSubject.Value.Form, snack);
                },
                error =>
                {
                    Console.WriteLine($"Unhandled error in SnackCreated reducer: {error.Message}");
                    _stateSubject.OnNext(_stateSubject.Value with
                    {
                        SelectedSnack = _originalBeforeOperation,
                        InProgress = false
                    });

                    // Restore form to original values
                    if (_originalBeforeOperation != null)
                    {
                        FormModelBuilder.UpdateFormFromModel(_stateSubject.Value.Form, _originalBeforeOperation);
                    }
                }
            ));

        // Rest of the reducers remain the same...

        // Form submitted reducer
        _disposables.Add(FormSubmittedObs.Subscribe());
    }

    // --- Dispose
    public void Dispose() => _disposables.Dispose();
}
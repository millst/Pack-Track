// ViewModels/SceneTransitionViewModel.cs - Updated with smart transition tracking
using Pack_Track.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;

namespace Pack_Track.ViewModels
{
    public class SceneTransitionViewModel : BaseViewModel
    {
        private readonly SceneTransition _transition;
        private readonly Show _show;

        public SceneTransitionViewModel(SceneTransition transition, Show show)
        {
            _transition = transition;
            _show = show;

            Actions = new ObservableCollection<TransitionActionViewModel>();

            // Set up actions with show reference for smart tracking
            foreach (var action in _transition.Actions)
            {
                action.Show = _show; // Enable smart status tracking
                Actions.Add(new TransitionActionViewModel(action, this));
            }

            // Subscribe to asset status changes to update transition display
            foreach (var assetStatus in _show.AssetStatuses)
            {
                assetStatus.PropertyChanged += OnAssetStatusChanged;
            }
        }

        private void OnAssetStatusChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AssetStatus.Status) ||
                e.PropertyName == nameof(AssetStatus.CurrentlyAssignedToActorId))
            {
                // Refresh all transition actions when any asset status changes
                foreach (var action in Actions)
                {
                    action.RefreshStatus();
                }
                RefreshProgress();
            }
        }

        public SceneTransition Transition => _transition;

        public string Title => _transition.Title;

        // Smart progress based on visible actions only
        public string ProgressText
        {
            get
            {
                var visibleActions = Actions.Where(a => a.IsVisible).ToList();
                var completedVisible = visibleActions.Count(a => a.IsSmartCompleted);
                return $"{completedVisible}/{visibleActions.Count} actions completed";
            }
        }

        public bool AllActionsCompleted => Actions.Where(a => a.IsVisible).All(a => a.IsSmartCompleted);

        // Filtered actions - only show visible ones
        public ObservableCollection<TransitionActionViewModel> VisibleActions
        {
            get
            {
                var visibleActions = new ObservableCollection<TransitionActionViewModel>();
                foreach (var action in Actions.Where(a => a.IsVisible))
                {
                    visibleActions.Add(action);
                }
                return visibleActions;
            }
        }

        public ObservableCollection<TransitionActionViewModel> Actions { get; }

        public void RefreshProgress()
        {
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(AllActionsCompleted));
            OnPropertyChanged(nameof(VisibleActions));
        }
    }

    public class TransitionActionViewModel : BaseViewModel
    {
        private readonly TransitionAction _action;
        private readonly SceneTransitionViewModel _parent;

        public TransitionActionViewModel(TransitionAction action, SceneTransitionViewModel parent)
        {
            _action = action;
            _parent = parent;

            ToggleCompletedCommand = new RelayCommand(ToggleCompleted);
        }

        public TransitionAction Action => _action;

        public string Description => _action.Description;
        public string StatusIcon => _action.StatusIcon;
        public string StatusColor => _action.StatusColor;

        // Use smart completion instead of manual
        public bool IsCompleted => _action.IsSmartCompleted;
        public bool IsSmartCompleted => _action.IsSmartCompleted;
        public bool IsVisible => _action.IsVisible;

        public ICommand ToggleCompletedCommand { get; }

        private void ToggleCompleted()
        {
            // With smart tracking, manual toggle might not be needed
            // But keep for compatibility
            _action.IsCompleted = !_action.IsCompleted;
            OnPropertyChanged(nameof(IsCompleted));
            _parent.RefreshProgress();
        }

        public void MarkCompleted()
        {
            _action.IsCompleted = true;
            OnPropertyChanged(nameof(IsCompleted));
        }

        // Method to refresh all status-dependent properties
        public void RefreshStatus()
        {
            _action.RefreshStatus();
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(StatusIcon));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(IsSmartCompleted));
            OnPropertyChanged(nameof(IsVisible));
        }
    }
}
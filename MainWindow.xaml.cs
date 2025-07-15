// MainWindow.xaml.cs - Fixed version with damaged->lost progression
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Pack_Track.ViewModels;
using Pack_Track.Services;
using Pack_Track.Models;

namespace Pack_Track
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private DispatcherTimer? _holdTimer;
        private int _holdStage = 0;
        private AssetItemViewModel? _currentHoldItem;
        private Button? _currentHoldButton;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the data service and view model
            var dataService = new JsonDataService();
            _viewModel = new MainViewModel(dataService);

            DataContext = _viewModel;

            // Subscribe to scene selection changes for jumping
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedSceneFromButtons))
            {
                if (_viewModel.SelectedSceneFromButtons != null)
                {
                    System.Diagnostics.Debug.WriteLine($"SelectedSceneFromButtons changed to: {_viewModel.SelectedSceneFromButtons.Name}");
                    ScrollToScene(_viewModel.SelectedSceneFromButtons);
                }
            }
        }

        private void Button_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is AssetItemViewModel assetItem)
                {
                    // Start hold timer for checked out items or damaged items
                    if ((assetItem.Status == EquipmentStatus.CheckedOut && assetItem.IsAssignedToThisActor) ||
                        assetItem.Status == EquipmentStatus.Damaged)
                    {
                        _currentHoldItem = assetItem;
                        _currentHoldButton = button;
                        _holdStage = 0;
                        _holdTimer = new DispatcherTimer();
                        _holdTimer.Interval = TimeSpan.FromSeconds(3);
                        _holdTimer.Tick += HoldTimer_Tick;
                        _holdTimer.Start();

                        // Visual feedback - change button color to indicate hold is active
                        button.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Warning yellow
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Button_PreviewMouseLeftButtonDown: {ex.Message}");
                StopHoldTimer();
            }
        }

        private void Button_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                StopHoldTimer();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Button_PreviewMouseLeftButtonUp: {ex.Message}");
            }
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                // Stop hold timer if mouse leaves the button
                StopHoldTimer();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Button_MouseLeave: {ex.Message}");
            }
        }

        private void StopHoldTimer()
        {
            try
            {
                if (_holdTimer != null)
                {
                    _holdTimer.Stop();
                    _holdTimer = null;
                }

                _holdStage = 0;

                // Reset button color safely
                if (_currentHoldButton != null && _currentHoldItem != null)
                {
                    try
                    {
                        // Reset to the correct button color
                        var colorBrush = new SolidColorBrush();
                        var color = _currentHoldItem.ButtonColor;

                        // Parse the hex color
                        if (color.StartsWith("#"))
                        {
                            colorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                        }
                        else
                        {
                            colorBrush = new SolidColorBrush(Colors.Gray);
                        }

                        _currentHoldButton.Background = colorBrush;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error resetting button color: {ex.Message}");
                    }
                }

                _currentHoldItem = null;
                _currentHoldButton = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in StopHoldTimer: {ex.Message}");
                // Force clear everything
                _holdTimer = null;
                _holdStage = 0;
                _currentHoldItem = null;
                _currentHoldButton = null;
            }
        }

        private void HoldTimer_Tick(object? sender, EventArgs e)
        {
            if (_currentHoldItem == null || _currentHoldButton == null)
            {
                StopHoldTimer();
                return;
            }

            _holdStage++;

            try
            {
                // Check current status to determine what action to take
                if (_currentHoldItem.Status == EquipmentStatus.CheckedOut && _holdStage == 1)
                {
                    // First hold on checked out item - mark as damaged
                    MarkAsDamaged(_currentHoldItem);
                    if (_currentHoldButton != null)
                    {
                        _currentHoldButton.Background = new SolidColorBrush(Color.FromRgb(233, 30, 99)); // Pink for damaged
                    }

                    // Reset for next stage (damaged -> lost)
                    _holdStage = 0;
                    _holdTimer?.Stop();
                    _holdTimer = new DispatcherTimer();
                    _holdTimer.Interval = TimeSpan.FromSeconds(3);
                    _holdTimer.Tick += HoldTimer_Tick;
                    _holdTimer.Start();
                }
                else if (_currentHoldItem.Status == EquipmentStatus.Damaged && _holdStage == 1)
                {
                    // Second hold on damaged item - mark as lost
                    MarkAsLost(_currentHoldItem);
                    if (_currentHoldButton != null)
                    {
                        _currentHoldButton.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red for lost
                    }
                    StopHoldTimer();
                }
                else
                {
                    // Shouldn't happen, but stop timer
                    StopHoldTimer();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HoldTimer_Tick: {ex.Message}");
                StopHoldTimer();
            }
        }

        private void MarkAsDamaged(AssetItemViewModel assetItem)
        {
            assetItem.MarkAsDamaged();
            MessageBox.Show($"{assetItem.ProductName} marked as DAMAGED\n\nHold again for 3 seconds to mark as LOST", "Equipment Status Changed",
                MessageBoxButton.OK, MessageBoxImage.Warning);

            // Refresh run summaries
            _viewModel.RefreshRunSummaries();
        }

        private void MarkAsLost(AssetItemViewModel assetItem)
        {
            assetItem.MarkAsLost();
            MessageBox.Show($"{assetItem.ProductName} marked as LOST/MISSING", "Equipment Status Changed",
                MessageBoxButton.OK, MessageBoxImage.Error);

            // Refresh run summaries
            _viewModel.RefreshRunSummaries();
        }

        // Public method to refresh runs dropdown when called from show setup
        public void RefreshRunsDropdown()
        {
            _viewModel.RefreshRunsDropdown();
        }

        private void ScrollToScene(Scene? targetScene)
        {
            if (targetScene == null) return;

            System.Diagnostics.Debug.WriteLine($"ScrollToScene called for: {targetScene.Name}");

            // Use Dispatcher to ensure UI is updated
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Find the main ScrollViewer
                    var scrollViewer = MainScrollViewer; // Direct reference to named element
                    if (scrollViewer == null)
                    {
                        System.Diagnostics.Debug.WriteLine("MainScrollViewer not found");
                        return;
                    }

                    // Find the ItemsControl inside the ScrollViewer
                    var itemsControl = FindVisualChild<ItemsControl>(scrollViewer);
                    if (itemsControl == null)
                    {
                        System.Diagnostics.Debug.WriteLine("ItemsControl not found");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"Found ItemsControl with {itemsControl.Items.Count} items");

                    // Look for the scene in the visual tree
                    for (int i = 0; i < itemsControl.Items.Count; i++)
                    {
                        var item = itemsControl.Items[i];
                        System.Diagnostics.Debug.WriteLine($"Item {i}: {item?.GetType().Name}");

                        if (item is SceneOperationViewModel sceneOp &&
                            sceneOp.Scene.Id == targetScene.Id)
                        {
                            System.Diagnostics.Debug.WriteLine($"Found matching scene at index {i}");

                            // Get the container for this item
                            var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                            if (container != null)
                            {
                                System.Diagnostics.Debug.WriteLine("Found container, bringing into view");

                                // Scroll to the container
                                container.BringIntoView();

                                // Add a highlight effect
                                HighlightSceneCard(container);
                                return;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("Container not found for scene");
                            }
                        }
                    }

                    System.Diagnostics.Debug.WriteLine("Target scene not found in items");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in ScrollToScene: {ex.Message}");
                }
            }), DispatcherPriority.Background);
        }

        private void HighlightSceneCard(FrameworkElement container)
        {
            // Find the Border inside the container
            var border = FindVisualChild<Border>(container);
            if (border != null)
            {
                // Create highlight animation
                var highlightBrush = new SolidColorBrush(Color.FromRgb(227, 242, 253)); // Light blue

                // Animate to highlight color and back
                var colorAnimation1 = new ColorAnimation
                {
                    To = highlightBrush.Color,
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                var colorAnimation2 = new ColorAnimation
                {
                    To = Colors.White,
                    Duration = TimeSpan.FromMilliseconds(500),
                    BeginTime = TimeSpan.FromMilliseconds(800)
                };

                var storyboard = new Storyboard();
                storyboard.Children.Add(colorAnimation1);
                storyboard.Children.Add(colorAnimation2);

                Storyboard.SetTarget(colorAnimation1, border);
                Storyboard.SetTarget(colorAnimation2, border);
                Storyboard.SetTargetProperty(colorAnimation1, new PropertyPath("Background.Color"));
                Storyboard.SetTargetProperty(colorAnimation2, new PropertyPath("Background.Color"));

                storyboard.Begin();
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T foundChild)
                {
                    return foundChild;
                }

                T? childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }

    // Template selector for scenes vs transitions
    public class SceneTransitionTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? SceneTemplate { get; set; }
        public DataTemplate? TransitionTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                SceneOperationViewModel => SceneTemplate,
                TransitionOperationViewModel => TransitionTemplate,
                _ => base.SelectTemplate(item, container)
            };
        }
    }
}
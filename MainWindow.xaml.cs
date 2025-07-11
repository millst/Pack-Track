﻿// MainWindow.xaml.cs - Fixed hold timer functionality
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
                ScrollToScene(_viewModel.SelectedSceneFromButtons);
            }
        }

        private void Button_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is AssetItemViewModel assetItem)
                {
                    // Only start hold timer for checked out items (when showing "Check In")
                    if (assetItem.Status != EquipmentStatus.CheckedOut) return;

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
                        var colorBrush = new SolidColorBrush();
                        switch (_currentHoldItem.Status)
                        {
                            case EquipmentStatus.CheckedOut:
                                colorBrush.Color = Color.FromRgb(255, 152, 0); // Orange
                                break;
                            case EquipmentStatus.CheckedIn:
                                colorBrush.Color = Color.FromRgb(76, 175, 80); // Green
                                break;
                            case EquipmentStatus.Missing:
                                colorBrush.Color = Color.FromRgb(244, 67, 54); // Red
                                break;
                            case EquipmentStatus.Damaged:
                                colorBrush.Color = Color.FromRgb(233, 30, 99); // Pink
                                break;
                            default:
                                colorBrush.Color = Color.FromRgb(158, 158, 158); // Gray
                                break;
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

            switch (_holdStage)
            {
                case 1: // First 3 seconds - mark as damaged
                    try
                    {
                        MarkAsDamaged(_currentHoldItem);
                        if (_currentHoldButton != null)
                        {
                            _currentHoldButton.Background = new SolidColorBrush(Color.FromRgb(233, 30, 99)); // Pink for damaged
                        }
                        _holdTimer?.Stop();
                        // Start timer for lost
                        _holdTimer = new DispatcherTimer();
                        _holdTimer.Interval = TimeSpan.FromSeconds(3);
                        _holdTimer.Tick += HoldTimer_Tick;
                        _holdTimer.Start();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in HoldTimer stage 1: {ex.Message}");
                        StopHoldTimer();
                    }
                    break;

                case 2: // Second 3 seconds - mark as lost
                    try
                    {
                        MarkAsLost(_currentHoldItem);
                        if (_currentHoldButton != null)
                        {
                            _currentHoldButton.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red for lost
                        }
                        StopHoldTimer();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in HoldTimer stage 2: {ex.Message}");
                        StopHoldTimer();
                    }
                    break;
            }
        }

        private void MarkAsDamaged(AssetItemViewModel assetItem)
        {
            assetItem.MarkAsDamaged();
            MessageBox.Show($"{assetItem.ProductName} marked as DAMAGED", "Equipment Status Changed",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void MarkAsLost(AssetItemViewModel assetItem)
        {
            assetItem.MarkAsLost();
            MessageBox.Show($"{assetItem.ProductName} marked as LOST/MISSING", "Equipment Status Changed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ScrollToScene(Scene? targetScene)
        {
            if (targetScene == null) return;

            // Use Dispatcher to ensure UI is updated
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Find the ItemsControl
                var itemsControl = FindVisualChild<ItemsControl>(MainScrollViewer);
                if (itemsControl == null) return;

                // Look for the scene in the visual tree
                for (int i = 0; i < itemsControl.Items.Count; i++)
                {
                    if (itemsControl.Items[i] is SceneOperationViewModel sceneOp &&
                        sceneOp.Scene.Id == targetScene.Id)
                    {
                        // Get the container for this item
                        var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                        if (container != null)
                        {
                            // Scroll to the container
                            container.BringIntoView();

                            // Add a highlight effect
                            HighlightSceneCard(container);
                        }
                        break;
                    }
                }
            }), DispatcherPriority.Loaded);
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
}
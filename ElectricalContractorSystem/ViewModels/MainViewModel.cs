using System;
using System.Windows;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Views;

namespace ElectricalContractorSystem.ViewModels
{
    /// <summary>
    /// Main ViewModel for the application, handles navigation between views
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private object _currentView;

        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        // Navigation commands
        public ICommand NavigateToJobManagementCommand { get; }
        public ICommand NavigateToWeeklyLaborEntryCommand { get; }
        public ICommand NavigateToMaterialEntryCommand { get; }
        public ICommand NavigateToJobCostTrackingCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }

        public MainViewModel()
        {
            // Initialize database service
            _databaseService = new DatabaseService();

            // Initialize navigation commands with error handling
            NavigateToJobManagementCommand = new RelayCommand(() => SafeNavigate(NavigateToJobManagement));
            NavigateToWeeklyLaborEntryCommand = new RelayCommand(() => SafeNavigate(NavigateToWeeklyLaborEntry));
            NavigateToMaterialEntryCommand = new RelayCommand(() => SafeNavigate(NavigateToMaterialEntry));
            NavigateToJobCostTrackingCommand = new RelayCommand(() => SafeNavigate(NavigateToJobCostTracking));
            NavigateToSettingsCommand = new RelayCommand(() => SafeNavigate(NavigateToSettings));

            // Default view - start with Job Management
            SafeNavigate(NavigateToJobManagement);
        }

        /// <summary>
        /// Safe navigation wrapper that handles exceptions
        /// </summary>
        private void SafeNavigate(Action navigationAction)
        {
            try
            {
                navigationAction?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Navigation error: {ex.Message}\n\nPlease try again. If the problem persists, restart the application.", 
                    "Navigation Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                    
                // Try to fallback to a safe view
                try
                {
                    NavigateToJobManagement();
                }
                catch
                {
                    // If even the fallback fails, show a minimal error view
                    CurrentView = new System.Windows.Controls.TextBlock 
                    { 
                        Text = "Error loading views. Please restart the application.", 
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        FontSize = 16
                    };
                }
            }
        }

        private void NavigateToJobManagement()
        {
            var view = new JobManagementView();
            view.DataContext = new JobManagementViewModel(_databaseService);
            CurrentView = view;
        }

        private void NavigateToWeeklyLaborEntry()
        {
            var view = new WeeklyLaborEntryView();
            view.DataContext = new WeeklyLaborEntryViewModel(_databaseService);
            CurrentView = view;
        }

        private void NavigateToMaterialEntry()
        {
            var view = new MaterialEntryView();
            view.DataContext = new MaterialEntryViewModel(_databaseService);
            CurrentView = view;
        }

        private void NavigateToJobCostTracking()
        {
            var view = new JobCostTrackingView();
            view.DataContext = new JobCostTrackingViewModel(_databaseService);
            CurrentView = view;
        }

        private void NavigateToSettings()
        {
            // TODO: Implement settings view
            MessageBox.Show("Settings functionality will be implemented in a future update.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

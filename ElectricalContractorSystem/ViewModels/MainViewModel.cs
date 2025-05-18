using System;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Services;

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

            // Initialize navigation commands
            NavigateToJobManagementCommand = new RelayCommand(NavigateToJobManagement);
            NavigateToWeeklyLaborEntryCommand = new RelayCommand(NavigateToWeeklyLaborEntry);
            NavigateToMaterialEntryCommand = new RelayCommand(NavigateToMaterialEntry);
            NavigateToJobCostTrackingCommand = new RelayCommand(NavigateToJobCostTracking);
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettings);

            // Default view
            NavigateToJobManagement(null);
        }

        private void NavigateToJobManagement(object parameter)
        {
            CurrentView = new JobManagementViewModel(_databaseService);
        }

        private void NavigateToWeeklyLaborEntry(object parameter)
        {
            CurrentView = new WeeklyLaborEntryViewModel(_databaseService);
        }

        private void NavigateToMaterialEntry(object parameter)
        {
            CurrentView = new MaterialEntryViewModel(_databaseService);
        }

        private void NavigateToJobCostTracking(object parameter)
        {
            CurrentView = new JobCostTrackingViewModel(_databaseService);
        }

        private void NavigateToSettings(object parameter)
        {
            // TODO: Implement settings view
            // CurrentView = new SettingsViewModel(_databaseService);
        }
    }
}

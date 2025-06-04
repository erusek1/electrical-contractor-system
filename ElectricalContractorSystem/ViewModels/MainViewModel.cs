using System;
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

            // Initialize navigation commands
            NavigateToJobManagementCommand = new RelayCommand(() => NavigateToJobManagement());
            NavigateToWeeklyLaborEntryCommand = new RelayCommand(() => NavigateToWeeklyLaborEntry());
            NavigateToMaterialEntryCommand = new RelayCommand(() => NavigateToMaterialEntry());
            NavigateToJobCostTrackingCommand = new RelayCommand(() => NavigateToJobCostTracking());
            NavigateToSettingsCommand = new RelayCommand(() => NavigateToSettings());

            // Default view
            NavigateToJobManagement();
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
            // var view = new SettingsView();
            // view.DataContext = new SettingsViewModel(_databaseService);
            // CurrentView = view;
        }
    }
}

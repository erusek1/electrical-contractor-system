using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.ViewModels
{
    public class JobCostTrackingViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private string _selectedJobNumber;
        private Job _selectedJob;
        private ObservableCollection<JobStage> _stageComparisonList;
        private ObservableCollection<LaborEntry> _laborEntries;
        private ObservableCollection<MaterialEntry> _materialEntries;
        private ObservableCollection<RoomSpecification> _roomSpecifications;
        private string _activeTab = "labor";
        private decimal _laborRate = 75.00M; // Default labor rate

        // Summary stats
        private decimal _estimatedTotal;
        private decimal _actualTotal;
        private decimal _profit;
        private decimal _profitPercentage;
        private decimal _estimatedHoursTotal;
        private decimal _actualHoursTotal;
        private decimal _estimatedMaterialTotal;
        private decimal _actualMaterialTotal;
        private decimal _estimatedLaborCost;
        private decimal _actualLaborCost;

        public JobCostTrackingViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            JobsList = new ObservableCollection<Job>(_databaseService.GetAllJobs());
            
            if (JobsList.Any())
            {
                SelectedJobNumber = JobsList.First().JobNumber;
            }

            PrintReportCommand = new RelayCommand(ExecutePrintReport);
            ChangeTabCommand = new RelayCommand<string>(ExecuteChangeTab);
        }

        public ObservableCollection<Job> JobsList { get; private set; }

        public string SelectedJobNumber
        {
            get => _selectedJobNumber;
            set
            {
                if (SetProperty(ref _selectedJobNumber, value))
                {
                    LoadJobDetails();
                }
            }
        }

        public Job SelectedJob
        {
            get => _selectedJob;
            set => SetProperty(ref _selectedJob, value);
        }

        public ObservableCollection<JobStage> StageComparisonList
        {
            get => _stageComparisonList;
            set => SetProperty(ref _stageComparisonList, value);
        }

        public ObservableCollection<LaborEntry> LaborEntries
        {
            get => _laborEntries;
            set => SetProperty(ref _laborEntries, value);
        }

        public ObservableCollection<MaterialEntry> MaterialEntries
        {
            get => _materialEntries;
            set => SetProperty(ref _materialEntries, value);
        }

        public ObservableCollection<RoomSpecification> RoomSpecifications
        {
            get => _roomSpecifications;
            set => SetProperty(ref _roomSpecifications, value);
        }

        public string ActiveTab
        {
            get => _activeTab;
            set => SetProperty(ref _activeTab, value);
        }

        public decimal EstimatedTotal
        {
            get => _estimatedTotal;
            set => SetProperty(ref _estimatedTotal, value);
        }

        public decimal ActualTotal
        {
            get => _actualTotal;
            set => SetProperty(ref _actualTotal, value);
        }

        public decimal Profit
        {
            get => _profit;
            set => SetProperty(ref _profit, value);
        }

        public decimal ProfitPercentage
        {
            get => _profitPercentage;
            set => SetProperty(ref _profitPercentage, value);
        }

        public decimal EstimatedHoursTotal
        {
            get => _estimatedHoursTotal;
            set => SetProperty(ref _estimatedHoursTotal, value);
        }

        public decimal ActualHoursTotal
        {
            get => _actualHoursTotal;
            set => SetProperty(ref _actualHoursTotal, value);
        }

        public decimal EstimatedMaterialTotal
        {
            get => _estimatedMaterialTotal;
            set => SetProperty(ref _estimatedMaterialTotal, value);
        }

        public decimal ActualMaterialTotal
        {
            get => _actualMaterialTotal;
            set => SetProperty(ref _actualMaterialTotal, value);
        }

        public decimal EstimatedLaborCost
        {
            get => _estimatedLaborCost;
            set => SetProperty(ref _estimatedLaborCost, value);
        }

        public decimal ActualLaborCost
        {
            get => _actualLaborCost;
            set => SetProperty(ref _actualLaborCost, value);
        }

        public ICommand PrintReportCommand { get; }
        public ICommand ChangeTabCommand { get; }

        private void LoadJobDetails()
        {
            // Get the job from the database
            SelectedJob = _databaseService.GetJobByNumber(SelectedJobNumber);
            if (SelectedJob == null) return;

            // Load job stages with estimated vs actual data
            StageComparisonList = new ObservableCollection<JobStage>(
                _databaseService.GetJobStages(SelectedJob.JobId));

            // Load labor entries
            LaborEntries = new ObservableCollection<LaborEntry>(
                _databaseService.GetLaborEntriesByJob(SelectedJob.JobId));

            // Load material entries
            MaterialEntries = new ObservableCollection<MaterialEntry>(
                _databaseService.GetMaterialEntriesByJob(SelectedJob.JobId));

            // Load room specifications
            RoomSpecifications = new ObservableCollection<RoomSpecification>(
                _databaseService.GetRoomSpecifications(SelectedJob.JobId));

            // Calculate summary statistics
            CalculateSummaryStatistics();
        }

        private void CalculateSummaryStatistics()
        {
            // Calculate hours and material totals
            EstimatedHoursTotal = StageComparisonList.Sum(s => s.EstimatedHours);
            ActualHoursTotal = StageComparisonList.Sum(s => s.ActualHours);
            EstimatedMaterialTotal = StageComparisonList.Sum(s => s.EstimatedMaterialCost);
            ActualMaterialTotal = StageComparisonList.Sum(s => s.ActualMaterialCost);

            // Calculate labor costs
            EstimatedLaborCost = EstimatedHoursTotal * _laborRate;
            ActualLaborCost = ActualHoursTotal * _laborRate;

            // Calculate totals and profit
            EstimatedTotal = EstimatedLaborCost + EstimatedMaterialTotal;
            ActualTotal = ActualLaborCost + ActualMaterialTotal;
            Profit = EstimatedTotal - ActualTotal;
            
            // Calculate profit percentage
            ProfitPercentage = EstimatedTotal > 0 
                ? (Profit / EstimatedTotal) * 100 
                : 0;
        }

        private void ExecutePrintReport()
        {
            // This would be implemented to generate and print a report
            // For now, it's a placeholder for future functionality
            System.Windows.MessageBox.Show("Report printing feature will be implemented in a future update.", 
                "Print Report", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void ExecuteChangeTab(string tabName)
        {
            ActiveTab = tabName;
        }
    }
}

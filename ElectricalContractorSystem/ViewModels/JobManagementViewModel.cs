using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Views;

namespace ElectricalContractorSystem.ViewModels
{
    /// <summary>
    /// ViewModel for the Job Management screen
    /// </summary>
    public class JobManagementViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Job> _jobs;
        private ObservableCollection<Job> _filteredJobs;
        private string _searchText = string.Empty;
        private string _activeFilter = "active";
        private Job _selectedJob;
        private bool _isLoading = false;
        private string _errorMessage;

        // Summary statistics
        private int _activeJobCount;
        private decimal _totalEstimate;
        private decimal _totalActual;

        #region Properties

        /// <summary>
        /// List of all jobs
        /// </summary>
        public ObservableCollection<Job> Jobs
        {
            get => _jobs;
            set => SetProperty(ref _jobs, value);
        }

        /// <summary>
        /// Filtered list of jobs based on search and filters
        /// </summary>
        public ObservableCollection<Job> FilteredJobs
        {
            get => _filteredJobs;
            set => SetProperty(ref _filteredJobs, value);
        }

        /// <summary>
        /// Currently selected job
        /// </summary>
        public Job SelectedJob
        {
            get => _selectedJob;
            set => SetProperty(ref _selectedJob, value);
        }

        /// <summary>
        /// Search text for filtering jobs
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// Active filter (active, completed, all)
        /// </summary>
        public string ActiveFilter
        {
            get => _activeFilter;
            set
            {
                if (SetProperty(ref _active
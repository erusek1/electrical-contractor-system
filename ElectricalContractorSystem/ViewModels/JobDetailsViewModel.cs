using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.ViewModels
{
    public class JobDetailsViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private Job _currentJob;
        private Customer _selectedCustomer;
        private ObservableCollection<Customer> _customers;
        private ObservableCollection<JobStage> _jobStages;
        private ObservableCollection<RoomSpecification> _roomSpecifications;
        private ObservableCollection<PermitItem> _permitItems;
        private bool _isNewJob;
        private string _activeTab = "details";
        private JobStage _selectedStage;
        private RoomSpecification _selectedRoomSpec;
        private PermitItem _selectedPermitItem;
        private decimal _totalEstimate;

        // For adding/editing stages
        private string _stageName;
        private decimal _estimatedHours;
        private decimal _estimatedMaterialCost;

        // For adding/editing room specs
        private string _roomName;
        private string _itemDescription;
        private int _quantity = 1;
        private string _itemCode;
        private decimal _unitPrice;

        // For adding/editing permit items
        private string _permitCategory;
        private int _permitQuantity = 1;
        private string _permitDescription;

        public JobDetailsViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;

            // Initialize empty job for a new job
            _currentJob = new Job
            {
                CreateDate = DateTime.Today,
                Status = "Estimate"
            };
            _isNewJob = true;

            LoadCustomers();
            InitializeCollections();
            InitializeCommands();
        }

        public JobDetailsViewModel(DatabaseService databaseService, int jobId)
        {
            _databaseService = databaseService;
            LoadJobById(jobId);
            LoadCustomers();
            InitializeCommands();
            _isNewJob = false;
        }

        private void InitializeCollections()
        {
            JobStages = new ObservableCollection<JobStage>();
            RoomSpecifications = new ObservableCollection<RoomSpecification>();
            PermitItems = new ObservableCollection<PermitItem>();
        }

        private void InitializeCommands()
        {
            SaveJobCommand = new RelayCommand(ExecuteSaveJob, CanExecuteSaveJob);
            ChangeTabCommand = new RelayCommand<string>(ExecuteChangeTab);
            AddStageCommand = new RelayCommand(ExecuteAddStage, CanExecuteAddStage);
            EditStageCommand = new RelayCommand(ExecuteEditStage, CanExecuteEditStage);
            DeleteStageCommand = new RelayCommand(ExecuteDeleteStage, CanExecuteDeleteStage);
            AddRoomSpecCommand = new RelayCommand(ExecuteAddRoomSpec, CanExecuteAddRoomSpec);
            EditRoomSpecCommand = new RelayCommand(ExecuteEditRoomSpec, CanExecuteEditRoomSpec);
            DeleteRoomSpecCommand = new RelayCommand(ExecuteDeleteRoomSpec, CanExecuteDeleteRoomSpec);
            AddPermitItemCommand = new RelayCommand(ExecuteAddPermitItem, CanExecuteAddPermitItem);
            EditPermitItemCommand = new RelayCommand(ExecuteEditPermitItem, CanExecuteEditPermitItem);
            DeletePermitItemCommand = new RelayCommand(ExecuteDeletePermitItem, CanExecuteDeletePermitItem);
            ClearStageCommand = new RelayCommand(ExecuteClearStage);
            ClearRoomSpecCommand = new RelayCommand(ExecuteClearRoomSpec);
            ClearPermitItemCommand = new RelayCommand(ExecuteClearPermitItem);
            GenerateNextJobNumberCommand = new RelayCommand(ExecuteGenerateNextJobNumber);
        }

        private void LoadJobById(int jobId)
        {
            CurrentJob = _databaseService.GetJob(jobId);
            
            if (CurrentJob != null)
            {
                LoadJobStages();
                LoadRoomSpecifications();
                LoadPermitItems();
                CalculateTotalEstimate();
            }
        }

        private void LoadCustomers()
        {
            Customers = new ObservableCollection<Customer>(_databaseService.GetAllCustomers());
            
            if (CurrentJob?.CustomerId > 0)
            {
                SelectedCustomer = Customers.FirstOrDefault(c => c.CustomerId == CurrentJob.CustomerId);
            }
        }

        private void LoadJobStages()
        {
            JobStages = new ObservableCollection<JobStage>(_databaseService.GetJobStages(CurrentJob.JobId));
        }

        private void LoadRoomSpecifications()
        {
            RoomSpecifications = new ObservableCollection<RoomSpecification>(_databaseService.GetRoomSpecifications(CurrentJob.JobId));
        }

        private void LoadPermitItems()
        {
            PermitItems = new ObservableCollection<PermitItem>(_databaseService.GetPermitItems(CurrentJob.JobId));
        }

        private void CalculateTotalEstimate()
        {
            // Calculate labor cost (using a standard rate of $75/hour)
            decimal laborCost = JobStages.Sum(s => s.EstimatedHours) * 75;
            
            // Add material costs
            decimal materialCost = JobStages.Sum(s => s.EstimatedMaterialCost);
            
            // Add room specification costs
            decimal roomCost = RoomSpecifications.Sum(r => r.TotalPrice);
            
            TotalEstimate = laborCost + materialCost + roomCost;
            CurrentJob.TotalEstimate = TotalEstimate;
        }

        #region Properties

        public Job CurrentJob
        {
            get => _currentJob;
            set => SetProperty(ref _currentJob, value);
        }

        public bool IsNewJob
        {
            get => _isNewJob;
            set => SetProperty(ref _isNewJob, value);
        }

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (SetProperty(ref _selectedCustomer, value) && value != null)
                {
                    CurrentJob.CustomerId = value.CustomerId;
                    CurrentJob.Customer = value;
                }
            }
        }

        public ObservableCollection<JobStage> JobStages
        {
            get => _jobStages;
            set => SetProperty(ref _jobStages, value);
        }

        public ObservableCollection<RoomSpecification> RoomSpecifications
        {
            get => _roomSpecifications;
            set => SetProperty(ref _roomSpecifications, value);
        }

        public ObservableCollection<PermitItem> PermitItems
        {
            get => _permitItems;
            set => SetProperty(ref _permitItems, value);
        }

        public string ActiveTab
        {
            get => _activeTab;
            set => SetProperty(ref _activeTab, value);
        }

        public JobStage SelectedStage
        {
            get => _selectedStage;
            set
            {
                if (SetProperty(ref _selectedStage, value) && value != null)
                {
                    StageName = value.StageName;
                    EstimatedHours = value.EstimatedHours;
                    EstimatedMaterialCost = value.EstimatedMaterialCost;
                }
            }
        }

        public RoomSpecification SelectedRoomSpec
        {
            get => _selectedRoomSpec;
            set
            {
                if (SetProperty(ref _selectedRoomSpec, value) && value != null)
                {
                    RoomName = value.RoomName;
                    ItemDescription = value.ItemDescription;
                    Quantity = value.Quantity;
                    ItemCode = value.ItemCode;
                    UnitPrice = value.UnitPrice;
                }
            }
        }

        public PermitItem SelectedPermitItem
        {
            get => _selectedPermitItem;
            set
            {
                if (SetProperty(ref _selectedPermitItem, value) && value != null)
                {
                    PermitCategory = value.Category;
                    PermitQuantity = value.Quantity;
                    PermitDescription = value.Description;
                }
            }
        }

        public decimal TotalEstimate
        {
            get => _totalEstimate;
            set => SetProperty(ref _totalEstimate, value);
        }

        #region Stage Properties

        public string StageName
        {
            get => _stageName;
            set => SetProperty(ref _stageName, value);
        }

        public decimal EstimatedHours
        {
            get => _estimatedHours;
            set => SetProperty(ref _estimatedHours, value);
        }

        public decimal EstimatedMaterialCost
        {
            get => _estimatedMaterialCost;
            set => SetProperty(ref _estimatedMaterialCost, value);
        }

        #endregion

        #region Room Spec Properties

        public string RoomName
        {
            get => _roomName;
            set => SetProperty(ref _roomName, value);
        }

        public string ItemDescription
        {
            get => _itemDescription;
            set => SetProperty(ref _itemDescription, value);
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    // Update UI to show calculated total price
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public string ItemCode
        {
            get => _itemCode;
            set => SetProperty(ref _itemCode, value);
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (SetProperty(ref _unitPrice, value))
                {
                    // Update UI to show calculated total price
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public decimal TotalPrice => Quantity * UnitPrice;

        #endregion

        #region Permit Item Properties

        public string PermitCategory
        {
            get => _permitCategory;
            set => SetProperty(ref _permitCategory, value);
        }

        public int PermitQuantity
        {
            get => _permitQuantity;
            set => SetProperty(ref _permitQuantity, value);
        }

        public string PermitDescription
        {
            get => _permitDescription;
            set => SetProperty(ref _permitDescription, value);
        }

        #endregion

        #endregion

        #region Commands

        public ICommand SaveJobCommand { get; private set; }
        public ICommand ChangeTabCommand { get; private set; }
        public ICommand AddStageCommand { get; private set; }
        public ICommand EditStageCommand { get; private set; }
        public ICommand DeleteStageCommand { get; private set; }
        public ICommand AddRoomSpecCommand { get; private set; }
        public ICommand EditRoomSpecCommand { get; private set; }
        public ICommand DeleteRoomSpecCommand { get; private set; }
        public ICommand AddPermitItemCommand { get; private set; }
        public ICommand EditPermitItemCommand { get; private set; }
        public ICommand DeletePermitItemCommand { get; private set; }
        public ICommand ClearStageCommand { get; private set; }
        public ICommand ClearRoomSpecCommand { get; private set; }
        public ICommand ClearPermitItemCommand { get; private set; }
        public ICommand GenerateNextJobNumberCommand { get; private set; }

        #endregion

        #region Command Implementations

        private bool CanExecuteSaveJob()
        {
            return !string.IsNullOrEmpty(CurrentJob.JobNumber) && 
                   SelectedCustomer != null;
        }

        private void ExecuteSaveJob()
        {
            // Set customer information
            if (SelectedCustomer != null)
            {
                CurrentJob.CustomerId = SelectedCustomer.CustomerId;
                CurrentJob.Customer = SelectedCustomer;
            }

            // Save job
            if (IsNewJob)
            {
                CurrentJob.JobId = _databaseService.AddJob(CurrentJob);
                IsNewJob = false;
            }
            else
            {
                _databaseService.UpdateJob(CurrentJob);
            }

            // Save stages
            foreach (var stage in JobStages)
            {
                if (stage.JobId == 0)
                {
                    stage.JobId = CurrentJob.JobId;
                    stage.StageId = _databaseService.AddJobStage(stage);
                }
                else
                {
                    _databaseService.UpdateJobStage(stage);
                }
            }

            // Save room specifications
            foreach (var spec in RoomSpecifications)
            {
                if (spec.SpecId == 0)
                {
                    spec.JobId = CurrentJob.JobId;
                    spec.SpecId = _databaseService.AddRoomSpecification(spec);
                }
                else
                {
                    _databaseService.UpdateRoomSpecification(spec);
                }
            }

            // Save permit items
            foreach (var item in PermitItems)
            {
                if (item.PermitId == 0)
                {
                    item.JobId = CurrentJob.JobId;
                    item.PermitId = _databaseService.AddPermitItem(item);
                }
                else
                {
                    _databaseService.UpdatePermitItem(item);
                }
            }

            // Recalculate estimate and update
            CalculateTotalEstimate();
            CurrentJob.TotalEstimate = TotalEstimate;
            _databaseService.UpdateJob(CurrentJob);
        }

        private void ExecuteChangeTab(string tabName)
        {
            ActiveTab = tabName;
        }

        private bool CanExecuteAddStage()
        {
            return !string.IsNullOrEmpty(StageName);
        }

        private void ExecuteAddStage()
        {
            var stage = new JobStage
            {
                JobId = CurrentJob.JobId,
                StageName = StageName,
                EstimatedHours = EstimatedHours,
                EstimatedMaterialCost = EstimatedMaterialCost,
                ActualHours = 0,
                ActualMaterialCost = 0
            };

            JobStages.Add(stage);
            ExecuteClearStage();
            CalculateTotalEstimate();
        }

        private bool CanExecuteEditStage()
        {
            return SelectedStage != null && !string.IsNullOrEmpty(StageName);
        }

        private void ExecuteEditStage()
        {
            if (SelectedStage != null)
            {
                SelectedStage.StageName = StageName;
                SelectedStage.EstimatedHours = EstimatedHours;
                SelectedStage.EstimatedMaterialCost = EstimatedMaterialCost;

                // Refresh the list
                OnPropertyChanged(nameof(JobStages));
                ExecuteClearStage();
                CalculateTotalEstimate();
            }
        }

        private bool CanExecuteDeleteStage()
        {
            return SelectedStage != null;
        }

        private void ExecuteDeleteStage()
        {
            if (SelectedStage != null)
            {
                if (SelectedStage.StageId > 0)
                {
                    _databaseService.DeleteJobStage(SelectedStage.StageId);
                }

                JobStages.Remove(SelectedStage);
                ExecuteClearStage();
                CalculateTotalEstimate();
            }
        }

        private void ExecuteClearStage()
        {
            SelectedStage = null;
            StageName = string.Empty;
            EstimatedHours = 0;
            EstimatedMaterialCost = 0;
        }

        private bool CanExecuteAddRoomSpec()
        {
            return !string.IsNullOrEmpty(RoomName) && !string.IsNullOrEmpty(ItemDescription) && Quantity > 0;
        }

        private void ExecuteAddRoomSpec()
        {
            var roomSpec = new RoomSpecification
            {
                JobId = CurrentJob.JobId,
                RoomName = RoomName,
                ItemDescription = ItemDescription,
                Quantity = Quantity,
                ItemCode = ItemCode,
                UnitPrice = UnitPrice,
                TotalPrice = Quantity * UnitPrice
            };

            RoomSpecifications.Add(roomSpec);
            ExecuteClearRoomSpec();
            CalculateTotalEstimate();
        }

        private bool CanExecuteEditRoomSpec()
        {
            return SelectedRoomSpec != null && !string.IsNullOrEmpty(RoomName) && !string.IsNullOrEmpty(ItemDescription) && Quantity > 0;
        }

        private void ExecuteEditRoomSpec()
        {
            if (SelectedRoomSpec != null)
            {
                SelectedRoomSpec.RoomName = RoomName;
                SelectedRoomSpec.ItemDescription = ItemDescription;
                SelectedRoomSpec.Quantity = Quantity;
                SelectedRoomSpec.ItemCode = ItemCode;
                SelectedRoomSpec.UnitPrice = UnitPrice;
                SelectedRoomSpec.TotalPrice = Quantity * UnitPrice;

                // Refresh the list
                OnPropertyChanged(nameof(RoomSpecifications));
                ExecuteClearRoomSpec();
                CalculateTotalEstimate();
            }
        }

        private bool CanExecuteDeleteRoomSpec()
        {
            return SelectedRoomSpec != null;
        }

        private void ExecuteDeleteRoomSpec()
        {
            if (SelectedRoomSpec != null)
            {
                if (SelectedRoomSpec.SpecId > 0)
                {
                    _databaseService.DeleteRoomSpecification(SelectedRoomSpec.SpecId);
                }

                RoomSpecifications.Remove(SelectedRoomSpec);
                ExecuteClearRoomSpec();
                CalculateTotalEstimate();
            }
        }

        private void ExecuteClearRoomSpec()
        {
            SelectedRoomSpec = null;
            RoomName = string.Empty;
            ItemDescription = string.Empty;
            Quantity = 1;
            ItemCode = string.Empty;
            UnitPrice = 0;
        }

        private bool CanExecuteAddPermitItem()
        {
            return !string.IsNullOrEmpty(PermitCategory) && PermitQuantity > 0;
        }

        private void ExecuteAddPermitItem()
        {
            var permitItem = new PermitItem
            {
                JobId = CurrentJob.JobId,
                Category = PermitCategory,
                Quantity = PermitQuantity,
                Description = PermitDescription
            };

            PermitItems.Add(permitItem);
            ExecuteClearPermitItem();
        }

        private bool CanExecuteEditPermitItem()
        {
            return SelectedPermitItem != null && !string.IsNullOrEmpty(PermitCategory) && PermitQuantity > 0;
        }

        private void ExecuteEditPermitItem()
        {
            if (SelectedPermitItem != null)
            {
                SelectedPermitItem.Category = PermitCategory;
                SelectedPermitItem.Quantity = PermitQuantity;
                SelectedPermitItem.Description = PermitDescription;

                // Refresh the list
                OnPropertyChanged(nameof(PermitItems));
                ExecuteClearPermitItem();
            }
        }

        private bool CanExecuteDeletePermitItem()
        {
            return SelectedPermitItem != null;
        }

        private void ExecuteDeletePermitItem()
        {
            if (SelectedPermitItem != null)
            {
                if (SelectedPermitItem.PermitId > 0)
                {
                    _databaseService.DeletePermitItem(SelectedPermitItem.PermitId);
                }

                PermitItems.Remove(SelectedPermitItem);
                ExecuteClearPermitItem();
            }
        }

        private void ExecuteClearPermitItem()
        {
            SelectedPermitItem = null;
            PermitCategory = string.Empty;
            PermitQuantity = 1;
            PermitDescription = string.Empty;
        }

        private void ExecuteGenerateNextJobNumber()
        {
            // Get the next sequential job number from database
            string nextJobNumber = _databaseService.GetNextJobNumber();
            CurrentJob.JobNumber = nextJobNumber;
        }

        #endregion
    }
}

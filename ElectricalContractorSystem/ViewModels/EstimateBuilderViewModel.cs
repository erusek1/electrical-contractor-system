using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.ViewModels
{
    public class EstimateBuilderViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private readonly AssemblyService _assemblyService;
        private Estimate _currentEstimate;
        private EstimateRoom _selectedRoom;
        private PriceListItem _selectedPriceListItem;
        private AssemblyTemplate _selectedAssembly;
        private string _searchText;
        private bool _isNewEstimate;
        private bool _showAssemblies = true;
        private bool _showPriceList = false;
        
        // Quick entry properties
        private string _quickEntryQuantity = "1";
        private string _quickEntryCode = "";
        private string _quickEntryLaborHours = "";
        private object _selectedQuickEntry;
        
        public EstimateBuilderViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _assemblyService = new AssemblyService(databaseService);
            
            // Initialize collections
            Rooms = new ObservableCollection<EstimateRoom>();
            PriceListItems = new ObservableCollection<PriceListItem>();
            FilteredPriceListItems = new ObservableCollection<PriceListItem>();
            Assemblies = new ObservableCollection<AssemblyTemplate>();
            FilteredAssemblies = new ObservableCollection<AssemblyTemplate>();
            QuickEntryMatches = new ObservableCollection<dynamic>();
            
            // Initialize commands
            AddRoomCommand = new RelayCommand(ExecuteAddRoom);
            DeleteRoomCommand = new RelayCommand(ExecuteDeleteRoom, CanExecuteDeleteRoom);
            AddItemCommand = new RelayCommand(ExecuteAddItem, CanExecuteAddItem);
            RemoveItemCommand = new RelayCommand(ExecuteRemoveItem);
            SaveEstimateCommand = new RelayCommand(ExecuteSaveEstimate);
            DuplicateRoomCommand = new RelayCommand(ExecuteDuplicateRoom, CanExecuteDuplicateRoom);
            MoveRoomUpCommand = new RelayCommand(ExecuteMoveRoomUp, CanExecuteMoveRoomUp);
            MoveRoomDownCommand = new RelayCommand(ExecuteMoveRoomDown, CanExecuteMoveRoomDown);
            QuickAddCommand = new RelayCommand(ExecuteQuickAdd, CanExecuteQuickAdd);
            
            LoadPriceListItems();
            LoadAssemblies();
        }
        
        #region Properties
        
        public Estimate CurrentEstimate
        {
            get => _currentEstimate;
            set
            {
                _currentEstimate = value;
                OnPropertyChanged();
                LoadEstimateData();
            }
        }
        
        public EstimateRoom SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (_selectedRoom != null)
                {
                    _selectedRoom.PropertyChanged -= OnRoomPropertyChanged;
                }
                
                _selectedRoom = value;
                
                if (_selectedRoom != null)
                {
                    _selectedRoom.PropertyChanged += OnRoomPropertyChanged;
                }
                
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedRoomItems));
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public ObservableCollection<EstimateRoom> Rooms { get; }
        
        public ObservableCollection<EstimateLineItem> SelectedRoomItems => 
            _selectedRoom != null ? 
            new ObservableCollection<EstimateLineItem>(_selectedRoom.LineItems.OrderBy(i => i.LineOrder)) : 
            new ObservableCollection<EstimateLineItem>();
        
        public ObservableCollection<PriceListItem> PriceListItems { get; }
        public ObservableCollection<PriceListItem> FilteredPriceListItems { get; }
        public ObservableCollection<AssemblyTemplate> Assemblies { get; }
        public ObservableCollection<AssemblyTemplate> FilteredAssemblies { get; }
        public ObservableCollection<dynamic> QuickEntryMatches { get; }
        
        public PriceListItem SelectedPriceListItem
        {
            get => _selectedPriceListItem;
            set
            {
                _selectedPriceListItem = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public AssemblyTemplate SelectedAssembly
        {
            get => _selectedAssembly;
            set
            {
                _selectedAssembly = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterItems();
            }
        }
        
        public bool IsNewEstimate
        {
            get => _isNewEstimate;
            set
            {
                _isNewEstimate = value;
                OnPropertyChanged();
            }
        }
        
        public bool ShowAssemblies
        {
            get => _showAssemblies;
            set
            {
                _showAssemblies = value;
                _showPriceList = !value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowPriceList));
                FilterItems();
            }
        }
        
        public bool ShowPriceList
        {
            get => _showPriceList;
            set
            {
                _showPriceList = value;
                _showAssemblies = !value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowAssemblies));
                FilterItems();
            }
        }
        
        // Quick entry properties
        public string QuickEntryQuantity
        {
            get => _quickEntryQuantity;
            set
            {
                _quickEntryQuantity = value;
                OnPropertyChanged();
            }
        }
        
        public string QuickEntryCode
        {
            get => _quickEntryCode;
            set
            {
                _quickEntryCode = value;
                OnPropertyChanged();
                UpdateQuickEntryMatches();
            }
        }
        
        public string QuickEntryLaborHours
        {
            get => _quickEntryLaborHours;
            set
            {
                _quickEntryLaborHours = value;
                OnPropertyChanged();
            }
        }
        
        public object SelectedQuickEntry
        {
            get => _selectedQuickEntry;
            set
            {
                _selectedQuickEntry = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowManualLaborEntry));
            }
        }
        
        public bool HasQuickEntryMatches => QuickEntryMatches.Count > 0;
        public bool ShowManualLaborEntry => SelectedQuickEntry is PriceListItem;
        
        // Calculated properties
        public decimal TotalLaborHours => CurrentEstimate?.TotalLaborHours ?? 0;
        public decimal TotalMaterialCost => CurrentEstimate?.TotalMaterialCost ?? 0;
        public decimal TotalCost => CurrentEstimate?.TotalPrice ?? 0;
        
        // Labor breakdown by stage
        public decimal RoughHours => CalculateStageHours("Rough");
        public decimal FinishHours => CalculateStageHours("Finish");
        public decimal ServiceHours => CalculateStageHours("Service");
        public decimal ExtraHours => CalculateStageHours("Extra");
        public bool ShowStageBreakdown => CurrentEstimate?.LineItems.Any(i => i.Mode == EstimateLineItem.EntryMode.Assembly) ?? false;
        
        public string EstimateTitle => IsNewEstimate ? 
            "New Estimate" : 
            $"Estimate {CurrentEstimate?.EstimateNumber} v{CurrentEstimate?.Version}";
        
        #endregion
        
        #region Commands
        
        public ICommand AddRoomCommand { get; }
        public ICommand DeleteRoomCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand SaveEstimateCommand { get; }
        public ICommand DuplicateRoomCommand { get; }
        public ICommand MoveRoomUpCommand { get; }
        public ICommand MoveRoomDownCommand { get; }
        public ICommand QuickAddCommand { get; }
        
        #endregion
        
        #region Command Implementations
        
        private void ExecuteAddRoom(object parameter)
        {
            var newRoom = new EstimateRoom
            {
                EstimateId = CurrentEstimate.EstimateId,
                RoomName = "New Room",
                RoomOrder = Rooms.Count
            };
            
            // Subscribe to property changes
            newRoom.PropertyChanged += OnRoomPropertyChanged;
            
            Rooms.Add(newRoom);
            CurrentEstimate.Rooms.Add(newRoom);
            SelectedRoom = newRoom;
        }
        
        private bool CanExecuteDeleteRoom(object parameter)
        {
            return SelectedRoom != null;
        }
        
        private void ExecuteDeleteRoom(object parameter)
        {
            if (SelectedRoom != null)
            {
                var roomToDelete = SelectedRoom;
                var index = Rooms.IndexOf(roomToDelete);
                
                // Unsubscribe from events
                roomToDelete.PropertyChanged -= OnRoomPropertyChanged;
                
                Rooms.Remove(roomToDelete);
                CurrentEstimate.Rooms.Remove(roomToDelete);
                
                // Reorder remaining rooms
                for (int i = 0; i < Rooms.Count; i++)
                {
                    Rooms[i].RoomOrder = i;
                }
                
                // Select adjacent room
                if (Rooms.Count > 0)
                {
                    SelectedRoom = Rooms[Math.Min(index, Rooms.Count - 1)];
                }
                
                UpdateTotals();
            }
        }
        
        private bool CanExecuteAddItem(object parameter)
        {
            return SelectedRoom != null && 
                   ((ShowAssemblies && SelectedAssembly != null) || 
                    (ShowPriceList && SelectedPriceListItem != null));
        }
        
        private void ExecuteAddItem(object parameter)
        {
            if (SelectedRoom == null) return;
            
            EstimateLineItem lineItem = null;
            
            if (ShowAssemblies && SelectedAssembly != null)
            {
                lineItem = EstimateLineItem.CreateFromAssembly(SelectedAssembly, CurrentEstimate.LaborRate);
            }
            else if (ShowPriceList && SelectedPriceListItem != null)
            {
                lineItem = EstimateLineItem.CreateFromPriceListItem(SelectedPriceListItem);
            }
            
            if (lineItem != null)
            {
                lineItem.EstimateId = CurrentEstimate.EstimateId;
                lineItem.RoomId = SelectedRoom.RoomId;
                lineItem.LineOrder = SelectedRoom.LineItems.Count;
                
                SelectedRoom.LineItems.Add(lineItem);
                CurrentEstimate.LineItems.Add(lineItem);
                
                OnPropertyChanged(nameof(SelectedRoomItems));
                
                // Notify room of changes
                SelectedRoom.OnPropertyChanged(nameof(EstimateRoom.ItemCount));
                SelectedRoom.OnPropertyChanged(nameof(EstimateRoom.RoomTotal));
                
                UpdateTotals();
            }
        }
        
        private bool CanExecuteQuickAdd(object parameter)
        {
            return SelectedRoom != null && 
                   !string.IsNullOrWhiteSpace(QuickEntryQuantity) && 
                   SelectedQuickEntry != null;
        }
        
        private void ExecuteQuickAdd(object parameter)
        {
            if (!CanExecuteQuickAdd(null)) return;
            
            if (!int.TryParse(QuickEntryQuantity, out int quantity) || quantity <= 0)
            {
                quantity = 1;
            }
            
            EstimateLineItem lineItem = null;
            
            if (SelectedQuickEntry is AssemblyTemplate assembly)
            {
                lineItem = EstimateLineItem.CreateFromAssembly(assembly, CurrentEstimate.LaborRate);
            }
            else if (SelectedQuickEntry is PriceListItem priceItem)
            {
                lineItem = EstimateLineItem.CreateFromPriceListItem(priceItem);
                
                // If manual labor hours entered, use those
                if (!string.IsNullOrWhiteSpace(QuickEntryLaborHours) && 
                    decimal.TryParse(QuickEntryLaborHours, out decimal hours))
                {
                    lineItem.LaborMinutes = (int)(hours * 60);
                }
            }
            
            if (lineItem != null)
            {
                lineItem.Quantity = quantity;
                lineItem.EstimateId = CurrentEstimate.EstimateId;
                lineItem.RoomId = SelectedRoom.RoomId;
                lineItem.LineOrder = SelectedRoom.LineItems.Count;
                
                SelectedRoom.LineItems.Add(lineItem);
                CurrentEstimate.LineItems.Add(lineItem);
                
                OnPropertyChanged(nameof(SelectedRoomItems));
                SelectedRoom.OnPropertyChanged(nameof(EstimateRoom.ItemCount));
                SelectedRoom.OnPropertyChanged(nameof(EstimateRoom.RoomTotal));
                
                UpdateTotals();
                
                // Clear quick entry fields
                QuickEntryQuantity = "1";
                QuickEntryCode = "";
                QuickEntryLaborHours = "";
                SelectedQuickEntry = null;
            }
        }
        
        private void ExecuteRemoveItem(object parameter)
        {
            if (parameter is EstimateLineItem item && SelectedRoom != null)
            {
                SelectedRoom.LineItems.Remove(item);
                CurrentEstimate.LineItems.Remove(item);
                
                // Reorder remaining items
                var remainingItems = SelectedRoom.LineItems.OrderBy(i => i.LineOrder).ToList();
                for (int i = 0; i < remainingItems.Count; i++)
                {
                    remainingItems[i].LineOrder = i;
                }
                
                OnPropertyChanged(nameof(SelectedRoomItems));
                
                // Notify room of changes
                SelectedRoom.OnPropertyChanged(nameof(EstimateRoom.ItemCount));
                SelectedRoom.OnPropertyChanged(nameof(EstimateRoom.RoomTotal));
                
                UpdateTotals();
            }
        }
        
        private void ExecuteSaveEstimate(object parameter)
        {
            CurrentEstimate.CalculateTotals();
            _databaseService.SaveEstimate(CurrentEstimate);
            IsNewEstimate = false;
            OnPropertyChanged(nameof(EstimateTitle));
        }
        
        private bool CanExecuteDuplicateRoom(object parameter)
        {
            return SelectedRoom != null;
        }
        
        private void ExecuteDuplicateRoom(object parameter)
        {
            if (SelectedRoom != null)
            {
                var clonedRoom = SelectedRoom.Clone();
                clonedRoom.EstimateId = CurrentEstimate.EstimateId;
                clonedRoom.RoomName = $"{SelectedRoom.RoomName} (Copy)";
                clonedRoom.RoomOrder = Rooms.Count;
                
                // Subscribe to property changes
                clonedRoom.PropertyChanged += OnRoomPropertyChanged;
                
                Rooms.Add(clonedRoom);
                CurrentEstimate.Rooms.Add(clonedRoom);
                SelectedRoom = clonedRoom;
                
                UpdateTotals();
            }
        }
        
        private bool CanExecuteMoveRoomUp(object parameter)
        {
            return SelectedRoom != null && SelectedRoom.RoomOrder > 0;
        }
        
        private void ExecuteMoveRoomUp(object parameter)
        {
            if (SelectedRoom != null && SelectedRoom.RoomOrder > 0)
            {
                var currentIndex = SelectedRoom.RoomOrder;
                var roomAbove = Rooms.FirstOrDefault(r => r.RoomOrder == currentIndex - 1);
                
                if (roomAbove != null)
                {
                    roomAbove.RoomOrder = currentIndex;
                    SelectedRoom.RoomOrder = currentIndex - 1;
                    
                    // Refresh the rooms collection
                    var tempRooms = Rooms.OrderBy(r => r.RoomOrder).ToList();
                    Rooms.Clear();
                    tempRooms.ForEach(r => Rooms.Add(r));
                }
            }
        }
        
        private bool CanExecuteMoveRoomDown(object parameter)
        {
            return SelectedRoom != null && SelectedRoom.RoomOrder < Rooms.Count - 1;
        }
        
        private void ExecuteMoveRoomDown(object parameter)
        {
            if (SelectedRoom != null && SelectedRoom.RoomOrder < Rooms.Count - 1)
            {
                var currentIndex = SelectedRoom.RoomOrder;
                var roomBelow = Rooms.FirstOrDefault(r => r.RoomOrder == currentIndex + 1);
                
                if (roomBelow != null)
                {
                    roomBelow.RoomOrder = currentIndex;
                    SelectedRoom.RoomOrder = currentIndex + 1;
                    
                    // Refresh the rooms collection
                    var tempRooms = Rooms.OrderBy(r => r.RoomOrder).ToList();
                    Rooms.Clear();
                    tempRooms.ForEach(r => Rooms.Add(r));
                }
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void LoadPriceListItems()
        {
            var items = _databaseService.GetActivePriceListItems();
            PriceListItems.Clear();
            foreach (var item in items.OrderBy(i => i.Category).ThenBy(i => i.Name))
            {
                PriceListItems.Add(item);
            }
            FilterItems();
        }
        
        private void LoadAssemblies()
        {
            var assemblies = _assemblyService.GetActiveAssemblies();
            Assemblies.Clear();
            foreach (var assembly in assemblies.OrderBy(a => a.Category).ThenBy(a => a.AssemblyCode))
            {
                Assemblies.Add(assembly);
            }
            FilterItems();
        }
        
        private void FilterItems()
        {
            if (ShowAssemblies)
            {
                FilterAssemblies();
            }
            else
            {
                FilterPriceList();
            }
        }
        
        private void FilterPriceList()
        {
            FilteredPriceListItems.Clear();
            
            var query = PriceListItems.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(i => 
                    i.ItemCode.ToLower().Contains(searchLower) ||
                    i.Name.ToLower().Contains(searchLower) ||
                    (i.Description?.ToLower().Contains(searchLower) ?? false));
            }
            
            foreach (var item in query)
            {
                FilteredPriceListItems.Add(item);
            }
        }
        
        private void FilterAssemblies()
        {
            FilteredAssemblies.Clear();
            
            var query = Assemblies.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(a => 
                    a.AssemblyCode.ToLower().Contains(searchLower) ||
                    a.Name.ToLower().Contains(searchLower) ||
                    (a.Description?.ToLower().Contains(searchLower) ?? false));
            }
            
            foreach (var assembly in query)
            {
                FilteredAssemblies.Add(assembly);
            }
        }
        
        private void UpdateQuickEntryMatches()
        {
            QuickEntryMatches.Clear();
            
            if (string.IsNullOrWhiteSpace(QuickEntryCode)) return;
            
            var codeLower = QuickEntryCode.ToLower();
            
            // Find matching assemblies
            var matchingAssemblies = Assemblies
                .Where(a => a.AssemblyCode.ToLower().StartsWith(codeLower))
                .Select(a => new { 
                    Item = a, 
                    DisplayText = $"{a.AssemblyCode} - {a.Name} (Assembly)",
                    IsAssembly = true 
                });
            
            // Find matching price list items
            var matchingPriceItems = PriceListItems
                .Where(p => p.ItemCode.ToLower().StartsWith(codeLower))
                .Select(p => new { 
                    Item = p, 
                    DisplayText = $"{p.ItemCode} - {p.Name} (Price List)",
                    IsAssembly = false 
                });
            
            // Add all matches
            foreach (var match in matchingAssemblies.Concat(matchingPriceItems))
            {
                QuickEntryMatches.Add(match);
            }
            
            // Auto-select if only one match
            if (QuickEntryMatches.Count == 1)
            {
                var match = QuickEntryMatches[0];
                SelectedQuickEntry = match.IsAssembly ? (object)match.Item : match.Item;
            }
            
            OnPropertyChanged(nameof(HasQuickEntryMatches));
        }
        
        private void LoadEstimateData()
        {
            // Unsubscribe from all room events
            foreach (var room in Rooms)
            {
                room.PropertyChanged -= OnRoomPropertyChanged;
            }
            
            Rooms.Clear();
            
            if (CurrentEstimate != null)
            {
                foreach (var room in CurrentEstimate.Rooms.OrderBy(r => r.RoomOrder))
                {
                    room.PropertyChanged += OnRoomPropertyChanged;
                    Rooms.Add(room);
                }
                
                if (Rooms.Count > 0)
                {
                    SelectedRoom = Rooms[0];
                }
                
                UpdateTotals();
            }
        }
        
        private void UpdateTotals()
        {
            CurrentEstimate?.CalculateTotals();
            OnPropertyChanged(nameof(TotalLaborHours));
            OnPropertyChanged(nameof(TotalMaterialCost));
            OnPropertyChanged(nameof(TotalCost));
            OnPropertyChanged(nameof(RoughHours));
            OnPropertyChanged(nameof(FinishHours));
            OnPropertyChanged(nameof(ServiceHours));
            OnPropertyChanged(nameof(ExtraHours));
            OnPropertyChanged(nameof(ShowStageBreakdown));
        }
        
        private decimal CalculateStageHours(string stage)
        {
            if (CurrentEstimate == null) return 0;
            
            var minutes = CurrentEstimate.LineItems
                .Where(i => i.Mode == EstimateLineItem.EntryMode.Assembly)
                .Sum(i => {
                    switch (stage)
                    {
                        case "Rough": return (i.RoughLaborMinutes ?? 0) * i.Quantity;
                        case "Finish": return (i.FinishLaborMinutes ?? 0) * i.Quantity;
                        case "Service": return (i.ServiceLaborMinutes ?? 0) * i.Quantity;
                        case "Extra": return (i.ExtraLaborMinutes ?? 0) * i.Quantity;
                        default: return 0;
                    }
                });
            
            return minutes / 60m;
        }
        
        private void OnRoomPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Refresh the room list when room properties change
            if (e.PropertyName == nameof(EstimateRoom.RoomName))
            {
                // Force the UI to update
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var temp = Rooms.ToList();
                    Rooms.Clear();
                    temp.ForEach(r => Rooms.Add(r));
                });
            }
        }
        
        #endregion
        
        #region Public Methods
        
        public void CreateNewEstimate(Customer customer)
        {
            CurrentEstimate = new Estimate
            {
                CustomerId = customer.CustomerId,
                Customer = customer,
                EstimateNumber = GenerateEstimateNumber(),
                Version = 1,
                JobName = $"{customer.Name} - New Project",
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Zip = customer.Zip,
                Status = EstimateStatus.Draft,
                CreatedDate = DateTime.Now,
                CreatedBy = Environment.UserName,
                LaborRate = 85m // Default labor rate
            };
            
            IsNewEstimate = true;
            
            // Add default room
            ExecuteAddRoom(null);
        }
        
        private string GenerateEstimateNumber()
        {
            // This would typically query the database for the next available number
            var lastEstimate = _databaseService.GetLastEstimateNumber();
            var lastNumber = 1000;
            
            if (!string.IsNullOrEmpty(lastEstimate))
            {
                if (int.TryParse(lastEstimate.Replace("EST-", ""), out int num))
                {
                    lastNumber = num;
                }
            }
            
            return $"EST-{lastNumber + 1}";
        }
        
        #endregion
    }
}

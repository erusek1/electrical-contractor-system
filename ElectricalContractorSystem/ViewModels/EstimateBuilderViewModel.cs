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
        private Estimate _currentEstimate;
        private EstimateRoom _selectedRoom;
        private PriceListItem _selectedPriceListItem;
        private string _searchText;
        private bool _isNewEstimate;
        
        public EstimateBuilderViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            
            // Initialize collections
            Rooms = new ObservableCollection<EstimateRoom>();
            PriceListItems = new ObservableCollection<PriceListItem>();
            FilteredPriceListItems = new ObservableCollection<PriceListItem>();
            
            // Initialize commands
            AddRoomCommand = new RelayCommand(ExecuteAddRoom);
            DeleteRoomCommand = new RelayCommand(ExecuteDeleteRoom, CanExecuteDeleteRoom);
            AddItemCommand = new RelayCommand(ExecuteAddItem, CanExecuteAddItem);
            RemoveItemCommand = new RelayCommand(ExecuteRemoveItem);
            SaveEstimateCommand = new RelayCommand(ExecuteSaveEstimate);
            DuplicateRoomCommand = new RelayCommand(ExecuteDuplicateRoom, CanExecuteDuplicateRoom);
            MoveRoomUpCommand = new RelayCommand(ExecuteMoveRoomUp, CanExecuteMoveRoomUp);
            MoveRoomDownCommand = new RelayCommand(ExecuteMoveRoomDown, CanExecuteMoveRoomDown);
            
            LoadPriceListItems();
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
                _selectedRoom = value;
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
        
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterPriceList();
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
        
        // Calculated properties
        public decimal TotalLaborHours => CurrentEstimate?.TotalLaborHours ?? 0;
        public decimal TotalMaterialCost => CurrentEstimate?.TotalMaterialCost ?? 0;
        public decimal TotalCost => CurrentEstimate?.TotalCost ?? 0;
        
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
            }
        }
        
        private bool CanExecuteAddItem(object parameter)
        {
            return SelectedRoom != null && SelectedPriceListItem != null;
        }
        
        private void ExecuteAddItem(object parameter)
        {
            if (SelectedRoom != null && SelectedPriceListItem != null)
            {
                var lineItem = EstimateLineItem.CreateFromPriceListItem(SelectedPriceListItem);
                lineItem.EstimateId = CurrentEstimate.EstimateId;
                lineItem.RoomId = SelectedRoom.RoomId;
                lineItem.LineOrder = SelectedRoom.LineItems.Count;
                
                SelectedRoom.LineItems.Add(lineItem);
                CurrentEstimate.LineItems.Add(lineItem);
                
                OnPropertyChanged(nameof(SelectedRoomItems));
                UpdateTotals();
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
                
                Rooms.Add(clonedRoom);
                CurrentEstimate.Rooms.Add(clonedRoom);
                SelectedRoom = clonedRoom;
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
            FilterPriceList();
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
        
        private void LoadEstimateData()
        {
            Rooms.Clear();
            
            if (CurrentEstimate != null)
            {
                foreach (var room in CurrentEstimate.Rooms.OrderBy(r => r.RoomOrder))
                {
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
                CreatedBy = Environment.UserName
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
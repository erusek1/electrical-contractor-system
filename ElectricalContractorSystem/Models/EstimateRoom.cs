using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ElectricalContractorSystem.Models
{
    public class EstimateRoom : INotifyPropertyChanged
    {
        private string _roomName;
        private int _roomOrder;
        private string _notes;
        
        public int RoomId { get; set; }
        public int EstimateId { get; set; }
        
        public string RoomName 
        { 
            get => _roomName;
            set
            {
                if (_roomName != value)
                {
                    _roomName = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public int RoomOrder 
        { 
            get => _roomOrder;
            set
            {
                if (_roomOrder != value)
                {
                    _roomOrder = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string Notes 
        { 
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public List<EstimateLineItem> Items { get; set; }
        
        // Alias for compatibility
        public List<EstimateLineItem> LineItems 
        { 
            get => Items; 
            set => Items = value; 
        }
        
        // Calculated properties
        public int ItemCount => Items?.Count ?? 0;
        
        public decimal RoomTotal => Items?.Sum(i => i.TotalPrice) ?? 0;

        public EstimateRoom()
        {
            Items = new List<EstimateLineItem>();
        }
        
        public EstimateRoom Clone()
        {
            var clone = new EstimateRoom
            {
                RoomName = RoomName,
                RoomOrder = RoomOrder,
                Notes = Notes,
                Items = new List<EstimateLineItem>()
            };
            
            foreach (var item in Items)
            {
                clone.Items.Add(new EstimateLineItem
                {
                    ItemCode = item.ItemCode,
                    ItemDescription = item.ItemDescription,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    MaterialCost = item.MaterialCost,
                    LaborMinutes = item.LaborMinutes,
                    LineOrder = item.LineOrder,
                    Notes = item.Notes
                });
            }
            
            return clone;
        }
        
        #region INotifyPropertyChanged Implementation
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }
}

using System.Collections.Generic;
using System.Linq;

namespace ElectricalContractorSystem.Models
{
    public class EstimateRoom
    {
        public int RoomId { get; set; }
        public int EstimateId { get; set; }
        public string RoomName { get; set; }
        public int RoomOrder { get; set; }
        public string Notes { get; set; }
        public List<EstimateLineItem> Items { get; set; }
        
        // Alias for compatibility
        public List<EstimateLineItem> LineItems 
        { 
            get => Items; 
            set => Items = value; 
        }

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
    }
}

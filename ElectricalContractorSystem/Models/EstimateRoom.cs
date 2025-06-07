using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ElectricalContractorSystem.Models
{
    public class EstimateRoom
    {
        public int RoomId { get; set; }
        
        [Required]
        public int EstimateId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string RoomName { get; set; }
        
        public int RoomOrder { get; set; }
        
        public string Notes { get; set; }
        
        // Navigation properties
        public virtual Estimate Estimate { get; set; }
        public virtual ICollection<EstimateLineItem> LineItems { get; set; }
        
        public EstimateRoom()
        {
            LineItems = new HashSet<EstimateLineItem>();
        }
        
        // Calculate room total
        public decimal RoomTotal => LineItems?.Sum(li => li.TotalPrice) ?? 0;
        
        // Get item count
        public int ItemCount => LineItems?.Count ?? 0;
        
        // Clone room with items
        public EstimateRoom Clone()
        {
            var clone = new EstimateRoom
            {
                RoomName = this.RoomName,
                RoomOrder = this.RoomOrder,
                Notes = this.Notes
            };
            
            foreach (var item in this.LineItems.OrderBy(i => i.LineOrder))
            {
                clone.LineItems.Add(new EstimateLineItem
                {
                    ItemId = item.ItemId,
                    Quantity = item.Quantity,
                    ItemCode = item.ItemCode,
                    Description = item.Description,
                    UnitPrice = item.UnitPrice,
                    LineOrder = item.LineOrder,
                    Notes = item.Notes
                });
            }
            
            return clone;
        }
    }
}
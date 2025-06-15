using System;

namespace ElectricalContractorSystem.Models
{
    public class Inventory
    {
        public int InventoryId { get; set; }
        
        public int ItemId { get; set; } // References PriceList.item_id
        
        public int? LocationId { get; set; } // References StorageLocation
        
        public decimal QuantityOnHand { get; set; } = 0;
        
        public decimal QuantityAllocated { get; set; } = 0;
        
        public decimal QuantityAvailable => QuantityOnHand - QuantityAllocated;
        
        public decimal? ReorderPoint { get; set; }
        
        public decimal? ReorderQuantity { get; set; }
        
        public DateTime? LastCountDate { get; set; }
        
        public string LastCountBy { get; set; }
        
        public string Notes { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedDate { get; set; }
        
        // Navigation properties
        public virtual PriceListItem PriceListItem { get; set; }
        public virtual StorageLocation Location { get; set; }
        
        // Helper methods
        public bool NeedsReorder()
        {
            return ReorderPoint.HasValue && QuantityAvailable <= ReorderPoint.Value;
        }
        
        public void AllocateQuantity(decimal quantity, int jobId)
        {
            if (quantity > QuantityAvailable)
                throw new InvalidOperationException($"Cannot allocate {quantity}. Only {QuantityAvailable} available.");
                
            QuantityAllocated += quantity;
            UpdatedDate = DateTime.Now;
        }
        
        public void ReleaseQuantity(decimal quantity)
        {
            if (quantity > QuantityAllocated)
                throw new InvalidOperationException($"Cannot release {quantity}. Only {QuantityAllocated} allocated.");
                
            QuantityAllocated -= quantity;
            UpdatedDate = DateTime.Now;
        }
        
        public void AdjustQuantity(decimal newQuantityOnHand, string adjustedBy, string reason)
        {
            QuantityOnHand = newQuantityOnHand;
            LastCountDate = DateTime.Now;
            LastCountBy = adjustedBy;
            Notes = $"Adjusted: {reason} - {DateTime.Now:yyyy-MM-dd HH:mm}";
            UpdatedDate = DateTime.Now;
        }
        
        public void ReceiveQuantity(decimal quantity)
        {
            QuantityOnHand += quantity;
            UpdatedDate = DateTime.Now;
        }
        
        public void IssueQuantity(decimal quantity)
        {
            if (quantity > QuantityAvailable)
                throw new InvalidOperationException($"Cannot issue {quantity}. Only {QuantityAvailable} available.");
                
            QuantityOnHand -= quantity;
            UpdatedDate = DateTime.Now;
        }
    }
}

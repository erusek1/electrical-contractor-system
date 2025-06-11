using System;

namespace ElectricalContractorSystem.Models
{
    public class Inventory
    {
        public int InventoryId { get; set; }
        public string ItemCode { get; set; }
        public int LocationId { get; set; }
        public int? SubLocationId { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal QuantityAllocated { get; set; }
        public decimal? MinQuantity { get; set; }
        public decimal? MaxQuantity { get; set; }
        public decimal? ReorderPoint { get; set; }
        public DateTime? LastCountedDate { get; set; }
        public string LastCountedBy { get; set; }
        public string Notes { get; set; }

        // Navigation properties
        public PriceListItem PriceListItem { get; set; }
        public StorageLocation Location { get; set; }
        public StorageSubLocation SubLocation { get; set; }

        // Calculated properties
        public decimal AvailableQuantity => QuantityOnHand - QuantityAllocated;
        
        public string StockStatus
        {
            get
            {
                if (QuantityOnHand <= (MinQuantity ?? 0))
                    return "Critical";
                else if (QuantityOnHand <= (ReorderPoint ?? 0))
                    return "Low";
                else
                    return "OK";
            }
        }

        public bool NeedsReorder => QuantityOnHand <= (ReorderPoint ?? 0);

        public Inventory()
        {
            QuantityOnHand = 0;
            QuantityAllocated = 0;
            MinQuantity = 0;
        }
    }
}

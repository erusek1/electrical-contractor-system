using System;
using System.Collections.Generic;

namespace ElectricalContractorSystem.Models
{
    public class InventoryRequest
    {
        public int RequestId { get; set; }
        public DateTime RequestDate { get; set; }
        public string RequestedBy { get; set; }
        public string RequestType { get; set; } // Job, Stock, Van, Other
        public int? JobId { get; set; }
        public int? TargetLocationId { get; set; }
        public string Status { get; set; } // Pending, Partial, Fulfilled, Cancelled, Ordered
        public DateTime? NeedByDate { get; set; }
        public string Notes { get; set; }

        // Navigation properties
        public Job Job { get; set; }
        public StorageLocation TargetLocation { get; set; }
        public List<InventoryRequestItem> RequestItems { get; set; }

        public InventoryRequest()
        {
            RequestDate = DateTime.Now;
            Status = "Pending";
            RequestItems = new List<InventoryRequestItem>();
        }

        public bool IsUrgent => NeedByDate.HasValue && NeedByDate.Value <= DateTime.Now.AddDays(2);
        
        public string GetStatusColor()
        {
            switch (Status)
            {
                case "Pending": return "Orange";
                case "Partial": return "Yellow";
                case "Fulfilled": return "Green";
                case "Cancelled": return "Gray";
                case "Ordered": return "Blue";
                default: return "Black";
            }
        }
    }

    public class InventoryRequestItem
    {
        public int RequestItemId { get; set; }
        public int RequestId { get; set; }
        public string ItemCode { get; set; }
        public decimal QuantityRequested { get; set; }
        public decimal QuantityFulfilled { get; set; }
        public int? PreferredLocationId { get; set; }
        public string Notes { get; set; }

        // Navigation properties
        public InventoryRequest Request { get; set; }
        public PriceListItem PriceListItem { get; set; }
        public StorageLocation PreferredLocation { get; set; }

        public InventoryRequestItem()
        {
            QuantityFulfilled = 0;
        }

        public decimal QuantityRemaining => QuantityRequested - QuantityFulfilled;
        public bool IsFullyFulfilled => QuantityFulfilled >= QuantityRequested;
    }
}

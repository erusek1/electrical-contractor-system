using System;

namespace ElectricalContractorSystem.Models
{
    public class InventoryTransaction
    {
        public int TransactionId { get; set; }
        
        public int ItemId { get; set; } // References PriceList.item_id
        
        public TransactionType TransactionType { get; set; }
        
        public decimal Quantity { get; set; }
        
        public decimal? UnitCost { get; set; }
        
        public int? JobId { get; set; }
        
        public int? VendorId { get; set; }
        
        public string PurchaseOrderNumber { get; set; }
        
        public string InvoiceNumber { get; set; }
        
        public int? FromLocationId { get; set; }
        
        public int? ToLocationId { get; set; }
        
        public string Reason { get; set; }
        
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        
        public string CreatedBy { get; set; }
        
        // Navigation properties
        public virtual PriceListItem PriceListItem { get; set; }
        public virtual Job Job { get; set; }
        public virtual Vendor Vendor { get; set; }
        public virtual StorageLocation FromLocation { get; set; }
        public virtual StorageLocation ToLocation { get; set; }
        
        // Calculated properties
        public decimal TotalValue => Quantity * (UnitCost ?? 0);
        
        public string TransactionDescription
        {
            get
            {
                switch (TransactionType)
                {
                    case TransactionType.Receive:
                        return $"Received {Quantity} from {Vendor?.Name ?? "Unknown Vendor"}";
                    case TransactionType.Issue:
                        return $"Issued {Quantity} to Job #{Job?.JobNumber ?? "Unknown"}";
                    case TransactionType.Adjust:
                        return $"Adjusted by {Quantity} - {Reason}";
                    case TransactionType.Return:
                        return $"Returned {Quantity} from Job #{Job?.JobNumber ?? "Unknown"}";
                    case TransactionType.Transfer:
                        return $"Transferred {Quantity} from {FromLocation?.LocationName ?? "Unknown"} to {ToLocation?.LocationName ?? "Unknown"}";
                    default:
                        return $"{TransactionType} {Quantity}";
                }
            }
        }
    }
    
    public enum TransactionType
    {
        Receive,   // Receiving from vendor
        Issue,     // Issuing to job
        Adjust,    // Inventory adjustment
        Return,    // Return from job
        Transfer   // Transfer between locations
    }
}

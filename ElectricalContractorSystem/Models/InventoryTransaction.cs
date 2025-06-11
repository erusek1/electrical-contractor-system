using System;

namespace ElectricalContractorSystem.Models
{
    public class InventoryTransaction
    {
        public int TransactionId { get; set; }
        public string TransactionType { get; set; } // Receive, Issue, Transfer, Adjust, Count, Return, Waste
        public DateTime TransactionDate { get; set; }
        public string ItemCode { get; set; }
        public decimal Quantity { get; set; }
        public int? FromLocationId { get; set; }
        public int? FromSubLocationId { get; set; }
        public int? ToLocationId { get; set; }
        public int? ToSubLocationId { get; set; }
        public int? JobId { get; set; }
        public int? VendorId { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal? UnitCost { get; set; }
        public string CreatedBy { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }

        // Navigation properties
        public PriceListItem PriceListItem { get; set; }
        public StorageLocation FromLocation { get; set; }
        public StorageLocation ToLocation { get; set; }
        public StorageSubLocation FromSubLocation { get; set; }
        public StorageSubLocation ToSubLocation { get; set; }
        public Job Job { get; set; }
        public Vendor Vendor { get; set; }

        public InventoryTransaction()
        {
            TransactionDate = DateTime.Now;
        }

        public string GetTransactionDescription()
        {
            switch (TransactionType)
            {
                case "Receive":
                    return $"Received {Quantity} from {Vendor?.Name ?? "Unknown"}";
                case "Issue":
                    return $"Issued {Quantity} to {Job?.JobName ?? "Stock"}";
                case "Transfer":
                    return $"Transferred {Quantity} from {FromLocation?.LocationName} to {ToLocation?.LocationName}";
                case "Adjust":
                    return $"Adjusted quantity to {Quantity}";
                case "Count":
                    return $"Physical count: {Quantity}";
                case "Return":
                    return $"Returned {Quantity} to stock";
                case "Waste":
                    return $"Wasted {Quantity}";
                default:
                    return $"{TransactionType}: {Quantity}";
            }
        }
    }
}

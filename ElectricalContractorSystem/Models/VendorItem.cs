using System;

namespace ElectricalContractorSystem.Models
{
    public class VendorItem
    {
        public int VendorItemId { get; set; }
        public int VendorId { get; set; }
        public string ItemCode { get; set; }
        public string VendorPartNumber { get; set; }
        public string VendorDescription { get; set; }
        public decimal? UnitCost { get; set; }
        public int CaseQuantity { get; set; }
        public int LeadTimeDays { get; set; }
        public int MinOrderQuantity { get; set; }
        public bool IsPreferred { get; set; }
        public DateTime LastUpdated { get; set; }

        // Navigation properties
        public Vendor Vendor { get; set; }
        public PriceListItem PriceListItem { get; set; }

        public VendorItem()
        {
            CaseQuantity = 1;
            LeadTimeDays = 0;
            MinOrderQuantity = 1;
            IsPreferred = false;
            LastUpdated = DateTime.Now;
        }
    }
}

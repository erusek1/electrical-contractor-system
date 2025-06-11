namespace ElectricalContractorSystem.Models
{
    public class StorageSubLocation
    {
        public int SubLocationId { get; set; }
        public int LocationId { get; set; }
        public string SubLocationName { get; set; }
        public string SubLocationType { get; set; } // Bin, Rack, Shelf, Case
        public string Position { get; set; } // A1, Top Shelf, Front Bin
        public decimal? MaxCapacity { get; set; }
        public string CapacityUnit { get; set; }
        public string Notes { get; set; }

        // Navigation property
        public StorageLocation Location { get; set; }

        public string GetFullName()
        {
            if (!string.IsNullOrEmpty(Position))
            {
                return $"{SubLocationName} ({Position})";
            }
            return SubLocationName;
        }
    }
}

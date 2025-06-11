using System;

namespace ElectricalContractorSystem.Models
{
    public class StorageLocation
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; }
        public string LocationType { get; set; } // Warehouse, Van, Storage, Outdoor, Other
        public int? ParentLocationId { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public bool IsMobile { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation property
        public StorageLocation ParentLocation { get; set; }

        public StorageLocation()
        {
            IsActive = true;
            CreatedDate = DateTime.Now;
        }

        public string GetFullLocationPath()
        {
            if (ParentLocation != null)
            {
                return $"{ParentLocation.LocationName} > {LocationName}";
            }
            return LocationName;
        }
    }
}

using System.Collections.Generic;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents a vendor (supplier) in the electrical contracting business
    /// </summary>
    public class Vendor
    {
        /// <summary>
        /// Database ID of the vendor
        /// </summary>
        public int VendorId { get; set; }

        /// <summary>
        /// Vendor name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Street address
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// City
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// State (2-letter code)
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Zip code
        /// </summary>
        public string Zip { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Navigation property to collection of MaterialEntries
        /// </summary>
        public ICollection<MaterialEntry> MaterialEntries { get; set; }

        /// <summary>
        /// Gets the full address as a single string
        /// </summary>
        public string FullAddress
        {
            get
            {
                return $"{Address}, {City}, {State} {Zip}".Trim();
            }
        }

        /// <summary>
        /// Returns a string representation of the vendor
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }
}

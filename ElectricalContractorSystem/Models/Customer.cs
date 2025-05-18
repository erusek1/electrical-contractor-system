using System.Collections.Generic;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents a customer in the electrical contracting business
    /// </summary>
    public class Customer
    {
        /// <summary>
        /// Database ID of the customer
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Customer name (individual or business)
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
        /// Email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Navigation property to collection of Jobs
        /// </summary>
        public ICollection<Job> Jobs { get; set; }

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
        /// Returns a string representation of the customer (name + city)
        /// </summary>
        public override string ToString()
        {
            return $"{Name} ({City})";
        }
    }
}

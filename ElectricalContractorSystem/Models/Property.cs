using System;
using System.Collections.Generic;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents a property/location where jobs can be performed
    /// </summary>
    public class Property
    {
        /// <summary>
        /// Database ID of the property
        /// </summary>
        public int PropertyId { get; set; }

        /// <summary>
        /// Associated customer ID
        /// </summary>
        public int CustomerId { get; set; }

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
        /// Type of property (Residential, Commercial, Industrial, Other)
        /// </summary>
        public string PropertyType { get; set; } = "Residential";

        /// <summary>
        /// Square footage of the building
        /// </summary>
        public int? SquareFootage { get; set; }

        /// <summary>
        /// Number of floors in the building
        /// </summary>
        public int? NumFloors { get; set; }

        /// <summary>
        /// Year the building was constructed
        /// </summary>
        public int? YearBuilt { get; set; }

        /// <summary>
        /// Information about the electrical panel(s)
        /// </summary>
        public string ElectricalPanelInfo { get; set; }

        /// <summary>
        /// Additional notes about the property
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Date the property was added to the system
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Navigation property to Customer
        /// </summary>
        public Customer Customer { get; set; }

        /// <summary>
        /// Navigation property to collection of Jobs at this property
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
        /// Gets a display string showing address and customer
        /// </summary>
        public string DisplayString
        {
            get
            {
                return $"{Address} - {Customer?.Name ?? "Unknown Customer"}";
            }
        }

        /// <summary>
        /// Gets the total number of jobs at this property
        /// </summary>
        public int TotalJobCount
        {
            get
            {
                return Jobs?.Count ?? 0;
            }
        }

        /// <summary>
        /// Gets the number of active jobs at this property
        /// </summary>
        public int ActiveJobCount
        {
            get
            {
                if (Jobs == null) return 0;
                
                int count = 0;
                foreach (var job in Jobs)
                {
                    if (job.Status != "Complete")
                        count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets the age of the building
        /// </summary>
        public int? BuildingAge
        {
            get
            {
                if (YearBuilt.HasValue)
                {
                    return DateTime.Now.Year - YearBuilt.Value;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the date of the last job at this property
        /// </summary>
        public DateTime? LastJobDate
        {
            get
            {
                if (Jobs == null || Jobs.Count == 0) return null;
                
                DateTime? lastDate = null;
                foreach (var job in Jobs)
                {
                    if (!lastDate.HasValue || job.CreateDate > lastDate.Value)
                    {
                        lastDate = job.CreateDate;
                    }
                }
                return lastDate;
            }
        }
    }
}

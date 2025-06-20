using System;
using System.Collections.Generic;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents a job in the electrical contracting business
    /// </summary>
    public class Job
    {
        /// <summary>
        /// Database ID of the job
        /// </summary>
        public int JobId { get; set; }

        /// <summary>
        /// Job number (e.g., 619, 620)
        /// </summary>
        public string JobNumber { get; set; }

        /// <summary>
        /// Associated customer ID
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Associated property ID (optional - for jobs at existing properties)
        /// </summary>
        public int? PropertyId { get; set; }

        /// <summary>
        /// Job name/description
        /// </summary>
        public string JobName { get; set; }

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
        /// Square footage of the building
        /// </summary>
        public int? SquareFootage { get; set; }

        /// <summary>
        /// Number of floors in the building
        /// </summary>
        public int? NumFloors { get; set; }

        /// <summary>
        /// Job status (Estimate, In Progress, Complete)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Date the job was created
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Date the job was completed
        /// </summary>
        public DateTime? CompletionDate { get; set; }

        /// <summary>
        /// Total estimated cost
        /// </summary>
        public decimal? TotalEstimate { get; set; }

        /// <summary>
        /// Total actual cost
        /// </summary>
        public decimal? TotalActual { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string Notes { get; set; }
        
        /// <summary>
        /// Source estimate ID if this job was created from an estimate
        /// </summary>
        public int? EstimateId { get; set; }

        /// <summary>
        /// Navigation property to Customer
        /// </summary>
        public Customer Customer { get; set; }

        /// <summary>
        /// Navigation property to Property
        /// </summary>
        public Property Property { get; set; }
        
        /// <summary>
        /// Navigation property to source Estimate
        /// </summary>
        public virtual Estimate SourceEstimate { get; set; }

        /// <summary>
        /// Navigation property to collection of JobStages
        /// </summary>
        public ICollection<JobStage> JobStages { get; set; }

        /// <summary>
        /// Navigation property to collection of LaborEntries
        /// </summary>
        public ICollection<LaborEntry> LaborEntries { get; set; }

        /// <summary>
        /// Navigation property to collection of MaterialEntries
        /// </summary>
        public ICollection<MaterialEntry> MaterialEntries { get; set; }

        /// <summary>
        /// Navigation property to collection of RoomSpecifications
        /// </summary>
        public ICollection<RoomSpecification> RoomSpecifications { get; set; }

        /// <summary>
        /// Navigation property to collection of PermitItems
        /// </summary>
        public ICollection<PermitItem> PermitItems { get; set; }

        /// <summary>
        /// Gets the full address as a single string
        /// </summary>
        public string FullAddress
        {
            get
            {
                // Use property address if available, otherwise use job address
                if (Property != null)
                {
                    return Property.FullAddress;
                }
                return $"{Address}, {City}, {State} {Zip}".Trim();
            }
        }

        /// <summary>
        /// Gets the display address (prefers property address over job address)
        /// </summary>
        public string DisplayAddress
        {
            get
            {
                return Property?.Address ?? Address;
            }
        }

        /// <summary>
        /// Gets the display city (prefers property city over job city)
        /// </summary>
        public string DisplayCity
        {
            get
            {
                return Property?.City ?? City;
            }
        }

        /// <summary>
        /// Gets the display state (prefers property state over job state)
        /// </summary>
        public string DisplayState
        {
            get
            {
                return Property?.State ?? State;
            }
        }

        /// <summary>
        /// Gets the display zip (prefers property zip over job zip)
        /// </summary>
        public string DisplayZip
        {
            get
            {
                return Property?.Zip ?? Zip;
            }
        }

        /// <summary>
        /// Gets job number with description for display in combo boxes
        /// </summary>
        public string JobNumberWithDescription
        {
            get
            {
                if (string.IsNullOrEmpty(JobNumber))
                    return JobName ?? "";
                
                if (string.IsNullOrEmpty(JobName))
                    return JobNumber;
                    
                return $"{JobNumber} - {JobName}";
            }
        }

        /// <summary>
        /// Gets job display with property info
        /// </summary>
        public string JobDisplayWithProperty
        {
            get
            {
                var display = JobNumberWithDescription;
                if (Property != null)
                {
                    display += $" @ {Property.Address}";
                }
                return display;
            }
        }

        /// <summary>
        /// Calculate the profit (estimated - actual)
        /// </summary>
        public decimal? Profit
        {
            get
            {
                if (TotalEstimate.HasValue && TotalActual.HasValue)
                {
                    return TotalEstimate.Value - TotalActual.Value;
                }
                return null;
            }
        }

        /// <summary>
        /// Calculate the profit percentage
        /// </summary>
        public decimal? ProfitPercentage
        {
            get
            {
                if (TotalEstimate.HasValue && TotalEstimate.Value > 0 && TotalActual.HasValue)
                {
                    return ((TotalEstimate.Value - TotalActual.Value) / TotalEstimate.Value) * 100;
                }
                return null;
            }
        }

        /// <summary>
        /// Indicates if this job is at an existing property
        /// </summary>
        public bool IsAtExistingProperty
        {
            get
            {
                return PropertyId.HasValue;
            }
        }
    }
}

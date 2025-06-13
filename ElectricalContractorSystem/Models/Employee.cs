using System.Collections.Generic;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents an employee in the electrical contracting business
    /// </summary>
    public class Employee
    {
        /// <summary>
        /// Database ID of the employee
        /// </summary>
        public int EmployeeId { get; set; }

        /// <summary>
        /// Employee name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Hourly rate
        /// </summary>
        public decimal HourlyRate { get; set; }

        /// <summary>
        /// Burden rate (additional employment costs like taxes, insurance, benefits)
        /// </summary>
        public decimal? BurdenRate { get; set; }
        
        /// <summary>
        /// Vehicle cost per hour for this employee
        /// </summary>
        public decimal? VehicleCostPerHour { get; set; }
        
        /// <summary>
        /// Overhead percentage to apply to hourly rate
        /// </summary>
        public decimal? OverheadPercentage { get; set; }

        /// <summary>
        /// Employee status (Active, Inactive)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Navigation property to collection of LaborEntries
        /// </summary>
        public ICollection<LaborEntry> LaborEntries { get; set; }

        /// <summary>
        /// Calculates the full cost rate including burden
        /// </summary>
        public decimal FullCostRate
        {
            get
            {
                if (BurdenRate.HasValue)
                {
                    return HourlyRate * (1 + (BurdenRate.Value / 100));
                }
                return HourlyRate;
            }
        }
        
        /// <summary>
        /// Calculates the effective rate including all costs
        /// Effective Rate = Base + Burden + Vehicle + (Base × Overhead%)
        /// </summary>
        public decimal EffectiveRate
        {
            get
            {
                decimal rate = HourlyRate;
                
                // Add burden rate (as fixed amount, not percentage)
                if (BurdenRate.HasValue)
                    rate += BurdenRate.Value;
                    
                // Add vehicle cost per hour
                if (VehicleCostPerHour.HasValue)
                    rate += VehicleCostPerHour.Value;
                    
                // Add overhead as percentage of base hourly rate
                if (OverheadPercentage.HasValue)
                    rate += HourlyRate * (OverheadPercentage.Value / 100);
                    
                return rate;
            }
        }

        /// <summary>
        /// Returns a string representation of the employee
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }
}
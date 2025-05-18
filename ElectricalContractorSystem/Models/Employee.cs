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
        /// Burden rate (additional cost as percentage of hourly rate)
        /// </summary>
        public decimal? BurdenRate { get; set; }

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
        /// Returns a string representation of the employee
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }
}

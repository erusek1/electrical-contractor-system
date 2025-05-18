using System.Collections.Generic;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents a stage of a job (e.g., Demo, Rough, Service, Finish, etc.)
    /// </summary>
    public class JobStage
    {
        /// <summary>
        /// Database ID of the stage
        /// </summary>
        public int StageId { get; set; }

        /// <summary>
        /// Associated job ID
        /// </summary>
        public int JobId { get; set; }

        /// <summary>
        /// Stage name (Demo, Rough, Service, Finish, Extra, Temp Service, Inspection, Other)
        /// </summary>
        public string StageName { get; set; }

        /// <summary>
        /// Estimated hours for the stage
        /// </summary>
        public decimal EstimatedHours { get; set; }

        /// <summary>
        /// Estimated material cost for the stage
        /// </summary>
        public decimal EstimatedMaterialCost { get; set; }

        /// <summary>
        /// Actual hours spent on the stage
        /// </summary>
        public decimal ActualHours { get; set; }

        /// <summary>
        /// Actual material cost for the stage
        /// </summary>
        public decimal ActualMaterialCost { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Navigation property to Job
        /// </summary>
        public Job Job { get; set; }

        /// <summary>
        /// Navigation property to collection of LaborEntries
        /// </summary>
        public ICollection<LaborEntry> LaborEntries { get; set; }

        /// <summary>
        /// Navigation property to collection of MaterialEntries
        /// </summary>
        public ICollection<MaterialEntry> MaterialEntries { get; set; }

        /// <summary>
        /// Calculate the hours variance (estimated - actual)
        /// </summary>
        public decimal HoursVariance => EstimatedHours - ActualHours;

        /// <summary>
        /// Calculate the material cost variance (estimated - actual)
        /// </summary>
        public decimal MaterialCostVariance => EstimatedMaterialCost - ActualMaterialCost;

        /// <summary>
        /// Calculate the estimated total cost (assuming standard labor rate)
        /// </summary>
        public decimal EstimatedTotalCost(decimal laborRate = 75.0m)
        {
            return (EstimatedHours * laborRate) + EstimatedMaterialCost;
        }

        /// <summary>
        /// Calculate the actual total cost (assuming standard labor rate)
        /// </summary>
        public decimal ActualTotalCost(decimal laborRate = 75.0m)
        {
            return (ActualHours * laborRate) + ActualMaterialCost;
        }

        /// <summary>
        /// Calculate the total cost variance (estimated - actual)
        /// </summary>
        public decimal TotalCostVariance(decimal laborRate = 75.0m)
        {
            return EstimatedTotalCost(laborRate) - ActualTotalCost(laborRate);
        }
    }
}

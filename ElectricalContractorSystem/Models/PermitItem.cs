namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents an electrical permit item for a job
    /// </summary>
    public class PermitItem
    {
        /// <summary>
        /// Database ID of the permit item
        /// </summary>
        public int PermitId { get; set; }

        /// <summary>
        /// Associated job ID
        /// </summary>
        public int JobId { get; set; }

        /// <summary>
        /// Category of the permit item (e.g., Receptacles, Fixtures, Switches)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Quantity of the item
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Additional description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Navigation property to Job
        /// </summary>
        public Job Job { get; set; }

        /// <summary>
        /// Returns a formatted description including quantity
        /// </summary>
        public string FormattedDescription
        {
            get
            {
                if (string.IsNullOrEmpty(Description))
                {
                    return $"{Quantity} × {Category}";
                }
                return $"{Quantity} × {Category} ({Description})";
            }
        }

        /// <summary>
        /// Creates a copy of the permit item for another job
        /// </summary>
        public PermitItem CreateCopy(int newJobId)
        {
            return new PermitItem
            {
                JobId = newJobId,
                Category = this.Category,
                Quantity = this.Quantity,
                Description = this.Description,
                Notes = this.Notes
            };
        }
    }
}

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents the status of an estimate
    /// </summary>
    public enum EstimateStatus
    {
        /// <summary>
        /// Initial draft state
        /// </summary>
        Draft,
        
        /// <summary>
        /// Sent to customer for review
        /// </summary>
        Sent,
        
        /// <summary>
        /// Approved by customer
        /// </summary>
        Approved,
        
        /// <summary>
        /// Rejected by customer
        /// </summary>
        Rejected,
        
        /// <summary>
        /// Expired without action
        /// </summary>
        Expired,
        
        /// <summary>
        /// Converted to a job
        /// </summary>
        Converted
    }
}

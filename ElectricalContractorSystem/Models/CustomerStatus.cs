namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Enum representing the status of a customer
    /// </summary>
    public enum CustomerStatus
    {
        /// <summary>
        /// Customer is active and in good standing
        /// </summary>
        Active,

        /// <summary>
        /// Customer is inactive (no recent business)
        /// </summary>
        Inactive,

        /// <summary>
        /// Customer is a prospect (potential customer)
        /// </summary>
        Prospect,

        /// <summary>
        /// Customer account is on hold due to payment issues
        /// </summary>
        OnHold,

        /// <summary>
        /// Customer is preferred (priority customer)
        /// </summary>
        Preferred,

        /// <summary>
        /// Customer account has been closed
        /// </summary>
        Closed,

        /// <summary>
        /// Customer is blacklisted (do not work with)
        /// </summary>
        Blacklisted
    }
}
namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Enum representing the status of a vendor
    /// </summary>
    public enum VendorStatus
    {
        /// <summary>
        /// Vendor is active and can be used for purchases
        /// </summary>
        Active,

        /// <summary>
        /// Vendor is inactive and should not be used for new purchases
        /// </summary>
        Inactive,

        /// <summary>
        /// Vendor is pending approval for use
        /// </summary>
        Pending,

        /// <summary>
        /// Vendor has been suspended due to performance issues
        /// </summary>
        Suspended,

        /// <summary>
        /// Vendor is preferred for specific items or services
        /// </summary>
        Preferred,

        /// <summary>
        /// Vendor is blacklisted and should not be used
        /// </summary>
        Blacklisted
    }
}
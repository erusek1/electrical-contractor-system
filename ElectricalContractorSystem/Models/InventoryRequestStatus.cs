namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Enum representing the status of an inventory request
    /// </summary>
    public enum InventoryRequestStatus
    {
        /// <summary>
        /// Request is pending fulfillment
        /// </summary>
        Pending,

        /// <summary>
        /// Request has been partially fulfilled
        /// </summary>
        Partial,

        /// <summary>
        /// Request has been completely fulfilled
        /// </summary>
        Fulfilled,

        /// <summary>
        /// Request has been cancelled
        /// </summary>
        Cancelled,

        /// <summary>
        /// Items have been ordered from vendor
        /// </summary>
        Ordered
    }
}
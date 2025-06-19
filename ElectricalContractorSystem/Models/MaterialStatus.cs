namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Enum representing the status of a material/inventory item
    /// </summary>
    public enum MaterialStatus
    {
        /// <summary>
        /// Material is active and available for use
        /// </summary>
        Active,

        /// <summary>
        /// Material is inactive and not available for new orders
        /// </summary>
        Inactive,

        /// <summary>
        /// Material has been discontinued by manufacturer
        /// </summary>
        Discontinued,

        /// <summary>
        /// Material is temporarily out of stock
        /// </summary>
        OutOfStock,

        /// <summary>
        /// Material stock is low and needs reordering
        /// </summary>
        LowStock,

        /// <summary>
        /// Material is on backorder from vendor
        /// </summary>
        BackOrdered,

        /// <summary>
        /// Material is obsolete and should be phased out
        /// </summary>
        Obsolete,

        /// <summary>
        /// Material is damaged and cannot be used
        /// </summary>
        Damaged
    }
}
namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Enum representing the type of inventory transaction
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// Receiving inventory from vendor
        /// </summary>
        Receive,

        /// <summary>
        /// Issuing inventory to a job
        /// </summary>
        Issue,

        /// <summary>
        /// Inventory adjustment (increase or decrease)
        /// </summary>
        Adjust,

        /// <summary>
        /// Return from job back to inventory
        /// </summary>
        Return,

        /// <summary>
        /// Transfer between storage locations
        /// </summary>
        Transfer,

        /// <summary>
        /// Sale of inventory item
        /// </summary>
        Sale,

        /// <summary>
        /// Damaged/write-off inventory
        /// </summary>
        WriteOff,

        /// <summary>
        /// Initial inventory count
        /// </summary>
        InitialCount
    }
}
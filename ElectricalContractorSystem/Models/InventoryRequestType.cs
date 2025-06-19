namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Enum representing the type of inventory request
    /// </summary>
    public enum InventoryRequestType
    {
        /// <summary>
        /// Request for materials for a specific job
        /// </summary>
        Job,

        /// <summary>
        /// Request to restock inventory
        /// </summary>
        Stock,

        /// <summary>
        /// Request to stock a work van
        /// </summary>
        Van,

        /// <summary>
        /// Other type of request
        /// </summary>
        Other,

        /// <summary>
        /// Emergency request requiring immediate attention
        /// </summary>
        Emergency,

        /// <summary>
        /// Request for office supplies
        /// </summary>
        Office
    }
}
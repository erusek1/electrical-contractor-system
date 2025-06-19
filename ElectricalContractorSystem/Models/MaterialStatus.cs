using System;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents the status of a material item in inventory
    /// </summary>
    public enum MaterialStatus
    {
        /// <summary>
        /// Material is currently active and available for use
        /// </summary>
        Active = 0,

        /// <summary>
        /// Material is inactive and not available for use
        /// </summary>
        Inactive = 1,

        /// <summary>
        /// Material is discontinued and no longer available
        /// </summary>
        Discontinued = 2,

        /// <summary>
        /// Material is temporarily out of stock
        /// </summary>
        OutOfStock = 3,

        /// <summary>
        /// Material is on order from vendor
        /// </summary>
        OnOrder = 4,

        /// <summary>
        /// Material has been received but not yet processed
        /// </summary>
        Received = 5,

        /// <summary>
        /// Material needs to be reordered (low stock)
        /// </summary>
        NeedsReorder = 6,

        /// <summary>
        /// Material is pending approval for use
        /// </summary>
        PendingApproval = 7,

        /// <summary>
        /// Material has been recalled by manufacturer
        /// </summary>
        Recalled = 8
    }

    /// <summary>
    /// Extension methods for MaterialStatus enum
    /// </summary>
    public static class MaterialStatusExtensions
    {
        /// <summary>
        /// Gets the display name for the material status
        /// </summary>
        /// <param name="status">The material status</param>
        /// <returns>Display-friendly status name</returns>
        public static string GetDisplayName(this MaterialStatus status)
        {
            return status switch
            {
                MaterialStatus.Active => "Active",
                MaterialStatus.Inactive => "Inactive",
                MaterialStatus.Discontinued => "Discontinued",
                MaterialStatus.OutOfStock => "Out of Stock",
                MaterialStatus.OnOrder => "On Order",
                MaterialStatus.Received => "Received",
                MaterialStatus.NeedsReorder => "Needs Reorder",
                MaterialStatus.PendingApproval => "Pending Approval",
                MaterialStatus.Recalled => "Recalled",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets the color associated with the material status for UI display
        /// </summary>
        /// <param name="status">The material status</param>
        /// <returns>Hex color code</returns>
        public static string GetStatusColor(this MaterialStatus status)
        {
            return status switch
            {
                MaterialStatus.Active => "#10B981", // Green
                MaterialStatus.Inactive => "#6B7280", // Gray
                MaterialStatus.Discontinued => "#EF4444", // Red
                MaterialStatus.OutOfStock => "#F59E0B", // Orange
                MaterialStatus.OnOrder => "#3B82F6", // Blue
                MaterialStatus.Received => "#8B5CF6", // Purple
                MaterialStatus.NeedsReorder => "#F59E0B", // Orange
                MaterialStatus.PendingApproval => "#F59E0B", // Orange
                MaterialStatus.Recalled => "#DC2626", // Dark Red
                _ => "#6B7280" // Gray
            };
        }

        /// <summary>
        /// Determines if the material is available for use in jobs
        /// </summary>
        /// <param name="status">The material status</param>
        /// <returns>True if available, false otherwise</returns>
        public static bool IsAvailable(this MaterialStatus status)
        {
            return status == MaterialStatus.Active || status == MaterialStatus.Received;
        }

        /// <summary>
        /// Determines if the material status indicates a problem that needs attention
        /// </summary>
        /// <param name="status">The material status</param>
        /// <returns>True if needs attention, false otherwise</returns>
        public static bool NeedsAttention(this MaterialStatus status)
        {
            return status == MaterialStatus.OutOfStock ||
                   status == MaterialStatus.NeedsReorder ||
                   status == MaterialStatus.PendingApproval ||
                   status == MaterialStatus.Recalled;
        }
    }
}
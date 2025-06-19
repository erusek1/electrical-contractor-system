using System;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents the status of an assembly template
    /// </summary>
    public enum AssemblyStatus
    {
        /// <summary>
        /// Assembly is active and available for use
        /// </summary>
        Active = 0,

        /// <summary>
        /// Assembly is inactive and not available for use
        /// </summary>
        Inactive = 1,

        /// <summary>
        /// Assembly is in draft mode and being developed
        /// </summary>
        Draft = 2,

        /// <summary>
        /// Assembly is deprecated but still viewable
        /// </summary>
        Deprecated = 3,

        /// <summary>
        /// Assembly is archived and hidden from normal use
        /// </summary>
        Archived = 4,

        /// <summary>
        /// Assembly is pending approval before activation
        /// </summary>
        PendingApproval = 5,

        /// <summary>
        /// Assembly has been rejected during approval process
        /// </summary>
        Rejected = 6,

        /// <summary>
        /// Assembly is under review for changes
        /// </summary>
        UnderReview = 7
    }

    /// <summary>
    /// Extension methods for AssemblyStatus enum
    /// </summary>
    public static class AssemblyStatusExtensions
    {
        /// <summary>
        /// Gets the display name for the assembly status
        /// </summary>
        /// <param name="status">The assembly status</param>
        /// <returns>Display-friendly status name</returns>
        public static string GetDisplayName(this AssemblyStatus status)
        {
            return status switch
            {
                AssemblyStatus.Active => "Active",
                AssemblyStatus.Inactive => "Inactive",
                AssemblyStatus.Draft => "Draft",
                AssemblyStatus.Deprecated => "Deprecated",
                AssemblyStatus.Archived => "Archived",
                AssemblyStatus.PendingApproval => "Pending Approval",
                AssemblyStatus.Rejected => "Rejected",
                AssemblyStatus.UnderReview => "Under Review",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets the color associated with the assembly status for UI display
        /// </summary>
        /// <param name="status">The assembly status</param>
        /// <returns>Hex color code</returns>
        public static string GetStatusColor(this AssemblyStatus status)
        {
            return status switch
            {
                AssemblyStatus.Active => "#10B981", // Green
                AssemblyStatus.Inactive => "#6B7280", // Gray
                AssemblyStatus.Draft => "#F59E0B", // Orange
                AssemblyStatus.Deprecated => "#F59E0B", // Orange
                AssemblyStatus.Archived => "#6B7280", // Gray
                AssemblyStatus.PendingApproval => "#3B82F6", // Blue
                AssemblyStatus.Rejected => "#EF4444", // Red
                AssemblyStatus.UnderReview => "#8B5CF6", // Purple
                _ => "#6B7280" // Gray
            };
        }

        /// <summary>
        /// Determines if the assembly is available for use in estimates
        /// </summary>
        /// <param name="status">The assembly status</param>
        /// <returns>True if available, false otherwise</returns>
        public static bool IsAvailable(this AssemblyStatus status)
        {
            return status == AssemblyStatus.Active;
        }

        /// <summary>
        /// Determines if the assembly can be edited
        /// </summary>
        /// <param name="status">The assembly status</param>
        /// <returns>True if editable, false otherwise</returns>
        public static bool IsEditable(this AssemblyStatus status)
        {
            return status == AssemblyStatus.Draft ||
                   status == AssemblyStatus.UnderReview ||
                   status == AssemblyStatus.Rejected;
        }

        /// <summary>
        /// Determines if the assembly status indicates it needs attention
        /// </summary>
        /// <param name="status">The assembly status</param>
        /// <returns>True if needs attention, false otherwise</returns>
        public static bool NeedsAttention(this AssemblyStatus status)
        {
            return status == AssemblyStatus.PendingApproval ||
                   status == AssemblyStatus.Rejected ||
                   status == AssemblyStatus.UnderReview;
        }
    }
}
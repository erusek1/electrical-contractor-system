using System;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents the type of audit action performed
    /// </summary>
    public enum AuditActionType
    {
        /// <summary>
        /// Record was created
        /// </summary>
        Created = 0,

        /// <summary>
        /// Record was updated/modified
        /// </summary>
        Updated = 1,

        /// <summary>
        /// Record was deleted
        /// </summary>
        Deleted = 2,

        /// <summary>
        /// Record was viewed/accessed
        /// </summary>
        Viewed = 3,

        /// <summary>
        /// Record was exported
        /// </summary>
        Exported = 4,

        /// <summary>
        /// Record was imported
        /// </summary>
        Imported = 5,

        /// <summary>
        /// Record was archived
        /// </summary>
        Archived = 6,

        /// <summary>
        /// Record was restored from archive
        /// </summary>
        Restored = 7,

        /// <summary>
        /// Record was approved
        /// </summary>
        Approved = 8,

        /// <summary>
        /// Record was rejected
        /// </summary>
        Rejected = 9,

        /// <summary>
        /// Record status was changed
        /// </summary>
        StatusChanged = 10,

        /// <summary>
        /// Record was duplicated/copied
        /// </summary>
        Duplicated = 11,

        /// <summary>
        /// Record was merged with another record
        /// </summary>
        Merged = 12,

        /// <summary>
        /// Record was split into multiple records
        /// </summary>
        Split = 13,

        /// <summary>
        /// System performed automatic action
        /// </summary>
        SystemAction = 14,

        /// <summary>
        /// User login event
        /// </summary>
        Login = 15,

        /// <summary>
        /// User logout event
        /// </summary>
        Logout = 16,

        /// <summary>
        /// Failed access attempt
        /// </summary>
        AccessDenied = 17,

        /// <summary>
        /// Configuration changed
        /// </summary>
        ConfigurationChanged = 18,

        /// <summary>
        /// Backup operation performed
        /// </summary>
        Backup = 19,

        /// <summary>
        /// Data migration performed
        /// </summary>
        Migration = 20
    }

    /// <summary>
    /// Extension methods for AuditActionType enum
    /// </summary>
    public static class AuditActionTypeExtensions
    {
        /// <summary>
        /// Gets the display name for the audit action type
        /// </summary>
        /// <param name="actionType">The audit action type</param>
        /// <returns>Display-friendly action name</returns>
        public static string GetDisplayName(this AuditActionType actionType)
        {
            return actionType switch
            {
                AuditActionType.Created => "Created",
                AuditActionType.Updated => "Updated",
                AuditActionType.Deleted => "Deleted",
                AuditActionType.Viewed => "Viewed",
                AuditActionType.Exported => "Exported",
                AuditActionType.Imported => "Imported",
                AuditActionType.Archived => "Archived",
                AuditActionType.Restored => "Restored",
                AuditActionType.Approved => "Approved",
                AuditActionType.Rejected => "Rejected",
                AuditActionType.StatusChanged => "Status Changed",
                AuditActionType.Duplicated => "Duplicated",
                AuditActionType.Merged => "Merged",
                AuditActionType.Split => "Split",
                AuditActionType.SystemAction => "System Action",
                AuditActionType.Login => "Login",
                AuditActionType.Logout => "Logout",
                AuditActionType.AccessDenied => "Access Denied",
                AuditActionType.ConfigurationChanged => "Configuration Changed",
                AuditActionType.Backup => "Backup",
                AuditActionType.Migration => "Migration",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets the icon associated with the audit action type
        /// </summary>
        /// <param name="actionType">The audit action type</param>
        /// <returns>Icon name or Unicode character</returns>
        public static string GetIcon(this AuditActionType actionType)
        {
            return actionType switch
            {
                AuditActionType.Created => "➕",
                AuditActionType.Updated => "✏️",
                AuditActionType.Deleted => "🗑️",
                AuditActionType.Viewed => "👁️",
                AuditActionType.Exported => "📤",
                AuditActionType.Imported => "📥",
                AuditActionType.Archived => "📦",
                AuditActionType.Restored => "♻️",
                AuditActionType.Approved => "✅",
                AuditActionType.Rejected => "❌",
                AuditActionType.StatusChanged => "🔄",
                AuditActionType.Duplicated => "📋",
                AuditActionType.Merged => "🔗",
                AuditActionType.Split => "✂️",
                AuditActionType.SystemAction => "🤖",
                AuditActionType.Login => "🔑",
                AuditActionType.Logout => "🚪",
                AuditActionType.AccessDenied => "🚫",
                AuditActionType.ConfigurationChanged => "⚙️",
                AuditActionType.Backup => "💾",
                AuditActionType.Migration => "📊",
                _ => "❓"
            };
        }

        /// <summary>
        /// Gets the color associated with the audit action type for UI display
        /// </summary>
        /// <param name="actionType">The audit action type</param>
        /// <returns>Hex color code</returns>
        public static string GetActionColor(this AuditActionType actionType)
        {
            return actionType switch
            {
                AuditActionType.Created => "#10B981", // Green
                AuditActionType.Updated => "#3B82F6", // Blue
                AuditActionType.Deleted => "#EF4444", // Red
                AuditActionType.Viewed => "#6B7280", // Gray
                AuditActionType.Exported => "#8B5CF6", // Purple
                AuditActionType.Imported => "#8B5CF6", // Purple
                AuditActionType.Archived => "#F59E0B", // Orange
                AuditActionType.Restored => "#10B981", // Green
                AuditActionType.Approved => "#10B981", // Green
                AuditActionType.Rejected => "#EF4444", // Red
                AuditActionType.StatusChanged => "#F59E0B", // Orange
                AuditActionType.Duplicated => "#3B82F6", // Blue
                AuditActionType.Merged => "#8B5CF6", // Purple
                AuditActionType.Split => "#F59E0B", // Orange
                AuditActionType.SystemAction => "#6B7280", // Gray
                AuditActionType.Login => "#10B981", // Green
                AuditActionType.Logout => "#6B7280", // Gray
                AuditActionType.AccessDenied => "#EF4444", // Red
                AuditActionType.ConfigurationChanged => "#F59E0B", // Orange
                AuditActionType.Backup => "#3B82F6", // Blue
                AuditActionType.Migration => "#8B5CF6", // Purple
                _ => "#6B7280" // Gray
            };
        }

        /// <summary>
        /// Determines if the action type represents a data-changing operation
        /// </summary>
        /// <param name="actionType">The audit action type</param>
        /// <returns>True if data-changing, false otherwise</returns>
        public static bool IsDataChanging(this AuditActionType actionType)
        {
            return actionType == AuditActionType.Created ||
                   actionType == AuditActionType.Updated ||
                   actionType == AuditActionType.Deleted ||
                   actionType == AuditActionType.Imported ||
                   actionType == AuditActionType.Archived ||
                   actionType == AuditActionType.Restored ||
                   actionType == AuditActionType.Approved ||
                   actionType == AuditActionType.Rejected ||
                   actionType == AuditActionType.StatusChanged ||
                   actionType == AuditActionType.Duplicated ||
                   actionType == AuditActionType.Merged ||
                   actionType == AuditActionType.Split ||
                   actionType == AuditActionType.Migration;
        }

        /// <summary>
        /// Determines if the action type represents a security-related event
        /// </summary>
        /// <param name="actionType">The audit action type</param>
        /// <returns>True if security-related, false otherwise</returns>
        public static bool IsSecurityEvent(this AuditActionType actionType)
        {
            return actionType == AuditActionType.Login ||
                   actionType == AuditActionType.Logout ||
                   actionType == AuditActionType.AccessDenied;
        }
    }
}
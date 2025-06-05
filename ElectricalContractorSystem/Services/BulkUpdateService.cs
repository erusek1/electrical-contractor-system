using System;
using System.Collections.Generic;
using System.Linq;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    /// <summary>
    /// Service for handling bulk operations on jobs
    /// </summary>
    public class BulkUpdateService
    {
        private readonly DatabaseService _databaseService;

        public BulkUpdateService() : this(new DatabaseService())
        {
        }

        public BulkUpdateService(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// Updates the status of multiple jobs
        /// </summary>
        /// <param name="jobIds">List of job IDs to update</param>
        /// <param name="newStatus">New status to set</param>
        /// <returns>Result containing success/failure information</returns>
        public BulkUpdateResult UpdateJobStatuses(List<int> jobIds, string newStatus)
        {
            var result = new BulkUpdateResult();
            
            if (jobIds == null || jobIds.Count == 0)
            {
                result.ErrorMessage = "No jobs selected for update.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(newStatus))
            {
                result.ErrorMessage = "New status cannot be empty.";
                return result;
            }

            foreach (var jobId in jobIds)
            {
                try
                {
                    bool success = _databaseService.UpdateJobStatus(jobId, newStatus);
                    if (success)
                    {
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.FailureCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    System.Diagnostics.Debug.WriteLine($"Error updating job {jobId}: {ex.Message}");
                }
            }

            result.TotalProcessed = result.SuccessCount + result.FailureCount;
            
            return result;
        }

        /// <summary>
        /// Gets jobs by status and date range for bulk operations
        /// </summary>
        /// <param name="status">Status filter (null for all statuses)</param>
        /// <param name="dateFrom">Start date filter (null for no start date)</param>
        /// <param name="dateTo">End date filter (null for no end date)</param>
        /// <returns>List of jobs matching the criteria</returns>
        public List<Job> GetJobsForBulkUpdate(string status = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            try
            {
                var allJobs = _databaseService.GetAllJobs();
                
                var filtered = allJobs.AsEnumerable();

                // Filter by status
                if (!string.IsNullOrEmpty(status) && status != "All Statuses")
                {
                    filtered = filtered.Where(j => j.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
                }

                // Filter by date range
                if (dateFrom.HasValue)
                {
                    filtered = filtered.Where(j => j.CreateDate >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    filtered = filtered.Where(j => j.CreateDate <= dateTo.Value.AddDays(1));
                }

                return filtered.OrderBy(j => j.JobNumber).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting jobs for bulk update: {ex.Message}");
                return new List<Job>();
            }
        }
    }

    /// <summary>
    /// Result of a bulk update operation
    /// </summary>
    public class BulkUpdateResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int TotalProcessed { get; set; }
        public string ErrorMessage { get; set; }
        
        public bool HasErrors => FailureCount > 0 || !string.IsNullOrEmpty(ErrorMessage);
        public bool IsSuccess => SuccessCount > 0 && string.IsNullOrEmpty(ErrorMessage);
    }
}
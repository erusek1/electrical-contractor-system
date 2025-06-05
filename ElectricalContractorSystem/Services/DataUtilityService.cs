using System;
using System.Collections.Generic;
using System.Linq;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    /// <summary>
    /// Utility service for one-time data operations
    /// </summary>
    public class DataUtilityService
    {
        private readonly DatabaseService _databaseService;

        public DataUtilityService(DatabaseService databaseService = null)
        {
            _databaseService = databaseService ?? new DatabaseService();
        }

        /// <summary>
        /// Updates some recent jobs to "In Progress" status for demo purposes
        /// Call this once to make your job list more realistic
        /// </summary>
        public void UpdateRecentJobsToActive()
        {
            try
            {
                // Get all jobs
                var allJobs = _databaseService.GetAllJobs();
                
                if (allJobs == null || !allJobs.Any())
                {
                    System.Windows.MessageBox.Show("No jobs found in database.", "Info");
                    return;
                }

                // Sort by job number (descending) to get the most recent jobs
                var recentJobs = allJobs
                    .Where(j => int.TryParse(j.JobNumber, out _))
                    .OrderByDescending(j => int.Parse(j.JobNumber))
                    .Take(15) // Take the 15 most recent jobs
                    .ToList();

                if (!recentJobs.Any())
                {
                    System.Windows.MessageBox.Show("No numeric job numbers found to update.", "Info");
                    return;
                }

                int updatedCount = 0;
                
                // Update the most recent 10 to "In Progress"
                var inProgressJobs = recentJobs.Take(10);
                foreach (var job in inProgressJobs)
                {
                    if (_databaseService.UpdateJobStatus(job.JobId, "In Progress"))
                    {
                        updatedCount++;
                    }
                }

                // Update the next 5 to "Estimate"
                var estimateJobs = recentJobs.Skip(10).Take(5);
                foreach (var job in estimateJobs)
                {
                    if (_databaseService.UpdateJobStatus(job.JobId, "Estimate"))
                    {
                        updatedCount++;
                    }
                }

                System.Windows.MessageBox.Show(
                    $"Successfully updated {updatedCount} jobs:\n" +
                    $"• 10 jobs set to 'In Progress'\n" +
                    $"• 5 jobs set to 'Estimate'\n\n" +
                    $"Refresh your job list to see the changes!",
                    "Jobs Updated");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error updating jobs: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Updates specific job numbers to active status
        /// </summary>
        public void UpdateSpecificJobsToActive(List<string> jobNumbers)
        {
            try
            {
                var allJobs = _databaseService.GetAllJobs();
                int updatedCount = 0;

                foreach (var jobNumber in jobNumbers)
                {
                    var job = allJobs.FirstOrDefault(j => j.JobNumber == jobNumber);
                    if (job != null)
                    {
                        if (_databaseService.UpdateJobStatus(job.JobId, "In Progress"))
                        {
                            updatedCount++;
                        }
                    }
                }

                System.Windows.MessageBox.Show(
                    $"Updated {updatedCount} jobs to 'In Progress' status.",
                    "Jobs Updated");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error updating specific jobs: {ex.Message}", "Error");
            }
        }
    }
}
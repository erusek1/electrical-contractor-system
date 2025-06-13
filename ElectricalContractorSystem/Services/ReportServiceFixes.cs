using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using ElectricalContractorSystem.Models;
using MySql.Data.MySqlClient;
using System.IO;
using System.Text;

namespace ElectricalContractorSystem.Services
{
    public partial class ReportService
    {
        // Fixed GetWeeklySummaryReport method to resolve parameters scope issue
        public new WeeklySummaryReport GetWeeklySummaryReport(DateTime weekStartDate)
        {
            var weekEndDate = weekStartDate.AddDays(4); // Monday to Friday
            
            var report = new WeeklySummaryReport
            {
                WeekStartDate = weekStartDate,
                WeekEndDate = weekEndDate,
                EmployeeSummaries = new Dictionary<string, EmployeeWeeklySummary>(),
                TotalHours = 0,
                TotalLaborCost = 0,
                ActiveJobs = 0,
                JobsWorked = new List<string>()
            };

            // Get all active employees
            string employeeQuery = "SELECT employee_id, name, hourly_rate FROM Employees WHERE status = 'Active'";
            var employees = new List<(int id, string name, decimal rate)>();
            
            using (var reader = _databaseService.ExecuteReader(employeeQuery))
            {
                while (reader.Read())
                {
                    employees.Add((
                        reader.GetInt32("employee_id"),
                        reader.GetString("name"),
                        reader.GetDecimal("hourly_rate")
                    ));
                }
            }

            // Get data for each employee
            foreach (var emp in employees)
            {
                var summary = new EmployeeWeeklySummary
                {
                    EmployeeName = emp.name,
                    DailyHours = new Dictionary<string, decimal>(),
                    TotalHours = 0,
                    JobsWorked = new List<string>()
                };

                // Initialize daily hours
                for (int i = 0; i < 5; i++)
                {
                    var date = weekStartDate.AddDays(i);
                    summary.DailyHours[date.ToString("dddd")] = 0;
                }

                // Get actual hours
                string hoursQuery = @"
                    SELECT 
                        le.date,
                        SUM(le.hours) as daily_hours,
                        GROUP_CONCAT(DISTINCT CONCAT(j.job_number, ' - ', j.job_name) SEPARATOR ', ') as jobs
                    FROM LaborEntries le
                    JOIN Jobs j ON le.job_id = j.job_id
                    WHERE le.employee_id = @employeeId
                        AND le.date BETWEEN @startDate AND @endDate
                    GROUP BY le.date";

                var hoursParameters = new Dictionary<string, object>
                {
                    ["@employeeId"] = emp.id,
                    ["@startDate"] = weekStartDate,
                    ["@endDate"] = weekEndDate
                };

                using (var reader = _databaseService.ExecuteReader(hoursQuery, hoursParameters))
                {
                    while (reader.Read())
                    {
                        var date = reader.GetDateTime("date");
                        var hours = reader.GetDecimal("daily_hours");
                        summary.DailyHours[date.ToString("dddd")] = hours;
                        summary.TotalHours += hours;
                        
                        var jobs = reader.GetString("jobs").Split(',').Select(j => j.Trim()).ToList();
                        foreach (var job in jobs)
                        {
                            if (!summary.JobsWorked.Contains(job))
                                summary.JobsWorked.Add(job);
                            if (!report.JobsWorked.Contains(job))
                                report.JobsWorked.Add(job);
                        }
                    }
                }

                summary.IsComplete = summary.TotalHours == 40;
                report.TotalHours += summary.TotalHours;
                report.TotalLaborCost += summary.TotalHours * emp.rate;
                report.EmployeeSummaries[emp.name] = summary;
            }

            // Get active jobs count
            string jobsQuery = @"
                SELECT COUNT(DISTINCT job_id) as active_jobs
                FROM LaborEntries
                WHERE date BETWEEN @startDate AND @endDate";

            var jobsParameters = new Dictionary<string, object>
            {
                ["@startDate"] = weekStartDate,
                ["@endDate"] = weekEndDate
            };

            using (var reader = _databaseService.ExecuteReader(jobsQuery, jobsParameters))
            {
                if (reader.Read())
                {
                    report.ActiveJobs = reader.GetInt32("active_jobs");
                }
            }

            return report;
        }
    }
}

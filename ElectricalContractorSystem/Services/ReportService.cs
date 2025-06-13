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
    public class ReportService
    {
        private readonly DatabaseService _databaseService;

        public ReportService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        #region Job Reports

        public class JobProfitabilityReport
        {
            public string JobNumber { get; set; }
            public string JobName { get; set; }
            public string CustomerName { get; set; }
            public string Status { get; set; }
            public decimal EstimatedRevenue { get; set; }
            public decimal ActualRevenue { get; set; }
            public decimal EstimatedCost { get; set; }
            public decimal ActualCost { get; set; }
            public decimal EstimatedProfit { get; set; }
            public decimal ActualProfit { get; set; }
            public decimal ProfitMargin { get; set; }
            public decimal EstimatedHours { get; set; }
            public decimal ActualHours { get; set; }
            public decimal MaterialCost { get; set; }
            public decimal LaborCost { get; set; }
        }

        public List<JobProfitabilityReport> GetJobProfitabilityReport(DateTime startDate, DateTime endDate, string status = null)
        {
            var reports = new List<JobProfitabilityReport>();
            
            string query = @"
                SELECT 
                    j.job_number,
                    j.job_name,
                    c.name as customer_name,
                    j.status,
                    j.total_estimate as estimated_revenue,
                    j.total_actual as actual_revenue,
                    COALESCE(SUM(js.estimated_hours), 0) as estimated_hours,
                    COALESCE(SUM(js.actual_hours), 0) as actual_hours,
                    COALESCE(SUM(js.estimated_material_cost), 0) as estimated_material,
                    COALESCE(SUM(js.actual_material_cost), 0) as actual_material
                FROM Jobs j
                JOIN Customers c ON j.customer_id = c.customer_id
                LEFT JOIN JobStages js ON j.job_id = js.job_id
                WHERE j.create_date BETWEEN @startDate AND @endDate";

            var parameters = new Dictionary<string, object>
            {
                ["@startDate"] = startDate,
                ["@endDate"] = endDate
            };

            if (!string.IsNullOrEmpty(status))
            {
                query += " AND j.status = @status";
                parameters["@status"] = status;
            }

            query += " GROUP BY j.job_id, j.job_number, j.job_name, c.name, j.status, j.total_estimate, j.total_actual";

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                while (reader.Read())
                {
                    var estimatedHours = reader.GetDecimal("estimated_hours");
                    var actualHours = reader.GetDecimal("actual_hours");
                    var estimatedMaterial = reader.GetDecimal("estimated_material");
                    var actualMaterial = reader.GetDecimal("actual_material");
                    
                    // Assume $75/hour labor rate (this could be configurable)
                    decimal laborRate = 75m;
                    var estimatedLabor = estimatedHours * laborRate;
                    var actualLabor = actualHours * laborRate;
                    
                    var estimatedCost = estimatedMaterial + estimatedLabor;
                    var actualCost = actualMaterial + actualLabor;
                    
                    var estimatedRevenue = reader.IsDBNull(reader.GetOrdinal("estimated_revenue")) ? 0 : reader.GetDecimal("estimated_revenue");
                    var actualRevenue = reader.IsDBNull(reader.GetOrdinal("actual_revenue")) ? 0 : reader.GetDecimal("actual_revenue");
                    
                    var report = new JobProfitabilityReport
                    {
                        JobNumber = reader.GetString("job_number"),
                        JobName = reader.GetString("job_name"),
                        CustomerName = reader.GetString("customer_name"),
                        Status = reader.GetString("status"),
                        EstimatedRevenue = estimatedRevenue,
                        ActualRevenue = actualRevenue,
                        EstimatedCost = estimatedCost,
                        ActualCost = actualCost,
                        EstimatedProfit = estimatedRevenue - estimatedCost,
                        ActualProfit = actualRevenue - actualCost,
                        ProfitMargin = actualRevenue > 0 ? ((actualRevenue - actualCost) / actualRevenue) * 100 : 0,
                        EstimatedHours = estimatedHours,
                        ActualHours = actualHours,
                        MaterialCost = actualMaterial,
                        LaborCost = actualLabor
                    };
                    
                    reports.Add(report);
                }
            }

            return reports;
        }

        public void ExportJobProfitabilityReportToCsv(DateTime startDate, DateTime endDate, string filePath, string status = null)
        {
            var reports = GetJobProfitabilityReport(startDate, endDate, status);
            
            using (var writer = new StreamWriter(filePath))
            {
                // Header
                writer.WriteLine("Job Number,Job Name,Customer,Status,Est Revenue,Act Revenue,Est Cost,Act Cost,Est Profit,Act Profit,Profit Margin %,Est Hours,Act Hours,Material Cost,Labor Cost");
                
                // Data
                foreach (var report in reports)
                {
                    writer.WriteLine($"{report.JobNumber}," +
                                   $"\"{report.JobName}\"," +
                                   $"\"{report.CustomerName}\"," +
                                   $"{report.Status}," +
                                   $"{report.EstimatedRevenue:F2}," +
                                   $"{report.ActualRevenue:F2}," +
                                   $"{report.EstimatedCost:F2}," +
                                   $"{report.ActualCost:F2}," +
                                   $"{report.EstimatedProfit:F2}," +
                                   $"{report.ActualProfit:F2}," +
                                   $"{report.ProfitMargin:F2}," +
                                   $"{report.EstimatedHours:F2}," +
                                   $"{report.ActualHours:F2}," +
                                   $"{report.MaterialCost:F2}," +
                                   $"{report.LaborCost:F2}");
                }
            }
        }

        #endregion

        #region Employee Reports

        public class EmployeeProductivityReport
        {
            public string EmployeeName { get; set; }
            public decimal TotalHours { get; set; }
            public int JobCount { get; set; }
            public decimal AverageHoursPerJob { get; set; }
            public decimal TotalLaborCost { get; set; }
            public Dictionary<string, decimal> HoursByStage { get; set; }
            public List<string> TopJobs { get; set; }
        }

        public List<EmployeeProductivityReport> GetEmployeeProductivityReport(DateTime startDate, DateTime endDate)
        {
            var reports = new List<EmployeeProductivityReport>();
            
            // Get employees with their total hours
            string query = @"
                SELECT 
                    e.employee_id,
                    e.name,
                    e.hourly_rate,
                    COALESCE(SUM(le.hours), 0) as total_hours,
                    COUNT(DISTINCT le.job_id) as job_count
                FROM Employees e
                LEFT JOIN LaborEntries le ON e.employee_id = le.employee_id 
                    AND le.date BETWEEN @startDate AND @endDate
                WHERE e.status = 'Active'
                GROUP BY e.employee_id, e.name, e.hourly_rate";

            var parameters = new Dictionary<string, object>
            {
                ["@startDate"] = startDate,
                ["@endDate"] = endDate
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                while (reader.Read())
                {
                    var employeeId = reader.GetInt32("employee_id");
                    var totalHours = reader.GetDecimal("total_hours");
                    var jobCount = reader.GetInt32("job_count");
                    
                    var report = new EmployeeProductivityReport
                    {
                        EmployeeName = reader.GetString("name"),
                        TotalHours = totalHours,
                        JobCount = jobCount,
                        AverageHoursPerJob = jobCount > 0 ? totalHours / jobCount : 0,
                        TotalLaborCost = totalHours * reader.GetDecimal("hourly_rate"),
                        HoursByStage = new Dictionary<string, decimal>(),
                        TopJobs = new List<string>()
                    };
                    
                    // Get hours by stage
                    GetEmployeeHoursByStage(employeeId, startDate, endDate, report.HoursByStage);
                    
                    // Get top jobs
                    GetEmployeeTopJobs(employeeId, startDate, endDate, report.TopJobs);
                    
                    reports.Add(report);
                }
            }

            return reports;
        }

        private void GetEmployeeHoursByStage(int employeeId, DateTime startDate, DateTime endDate, Dictionary<string, decimal> hoursByStage)
        {
            string query = @"
                SELECT 
                    js.stage_name,
                    SUM(le.hours) as stage_hours
                FROM LaborEntries le
                JOIN JobStages js ON le.stage_id = js.stage_id
                WHERE le.employee_id = @employeeId
                    AND le.date BETWEEN @startDate AND @endDate
                GROUP BY js.stage_name";

            var parameters = new Dictionary<string, object>
            {
                ["@employeeId"] = employeeId,
                ["@startDate"] = startDate,
                ["@endDate"] = endDate
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                while (reader.Read())
                {
                    hoursByStage[reader.GetString("stage_name")] = reader.GetDecimal("stage_hours");
                }
            }
        }

        private void GetEmployeeTopJobs(int employeeId, DateTime startDate, DateTime endDate, List<string> topJobs)
        {
            string query = @"
                SELECT 
                    j.job_number,
                    j.job_name,
                    SUM(le.hours) as job_hours
                FROM LaborEntries le
                JOIN Jobs j ON le.job_id = j.job_id
                WHERE le.employee_id = @employeeId
                    AND le.date BETWEEN @startDate AND @endDate
                GROUP BY j.job_id, j.job_number, j.job_name
                ORDER BY job_hours DESC
                LIMIT 5";

            var parameters = new Dictionary<string, object>
            {
                ["@employeeId"] = employeeId,
                ["@startDate"] = startDate,
                ["@endDate"] = endDate
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                while (reader.Read())
                {
                    topJobs.Add($"{reader.GetString("job_number")} - {reader.GetString("job_name")} ({reader.GetDecimal("job_hours"):F1} hrs)");
                }
            }
        }

        #endregion

        #region Material Reports

        public class MaterialUsageReport
        {
            public string VendorName { get; set; }
            public decimal TotalPurchases { get; set; }
            public int InvoiceCount { get; set; }
            public Dictionary<string, decimal> PurchasesByJob { get; set; }
            public Dictionary<string, decimal> PurchasesByStage { get; set; }
        }

        public List<MaterialUsageReport> GetMaterialUsageReport(DateTime startDate, DateTime endDate)
        {
            var reports = new List<MaterialUsageReport>();
            
            string query = @"
                SELECT 
                    v.vendor_id,
                    v.name,
                    COALESCE(SUM(me.cost), 0) as total_purchases,
                    COUNT(DISTINCT me.invoice_number) as invoice_count
                FROM Vendors v
                LEFT JOIN MaterialEntries me ON v.vendor_id = me.vendor_id
                    AND me.date BETWEEN @startDate AND @endDate
                GROUP BY v.vendor_id, v.name
                HAVING total_purchases > 0
                ORDER BY total_purchases DESC";

            var parameters = new Dictionary<string, object>
            {
                ["@startDate"] = startDate,
                ["@endDate"] = endDate
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                while (reader.Read())
                {
                    var vendorId = reader.GetInt32("vendor_id");
                    
                    var report = new MaterialUsageReport
                    {
                        VendorName = reader.GetString("name"),
                        TotalPurchases = reader.GetDecimal("total_purchases"),
                        InvoiceCount = reader.GetInt32("invoice_count"),
                        PurchasesByJob = new Dictionary<string, decimal>(),
                        PurchasesByStage = new Dictionary<string, decimal>()
                    };
                    
                    // Get purchases by job
                    GetVendorPurchasesByJob(vendorId, startDate, endDate, report.PurchasesByJob);
                    
                    // Get purchases by stage
                    GetVendorPurchasesByStage(vendorId, startDate, endDate, report.PurchasesByStage);
                    
                    reports.Add(report);
                }
            }

            return reports;
        }

        private void GetVendorPurchasesByJob(int vendorId, DateTime startDate, DateTime endDate, Dictionary<string, decimal> purchasesByJob)
        {
            string query = @"
                SELECT 
                    j.job_number,
                    j.job_name,
                    SUM(me.cost) as job_total
                FROM MaterialEntries me
                JOIN Jobs j ON me.job_id = j.job_id
                WHERE me.vendor_id = @vendorId
                    AND me.date BETWEEN @startDate AND @endDate
                GROUP BY j.job_id, j.job_number, j.job_name
                ORDER BY job_total DESC
                LIMIT 10";

            var parameters = new Dictionary<string, object>
            {
                ["@vendorId"] = vendorId,
                ["@startDate"] = startDate,
                ["@endDate"] = endDate
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                while (reader.Read())
                {
                    purchasesByJob[$"{reader.GetString("job_number")} - {reader.GetString("job_name")}"] = reader.GetDecimal("job_total");
                }
            }
        }

        private void GetVendorPurchasesByStage(int vendorId, DateTime startDate, DateTime endDate, Dictionary<string, decimal> purchasesByStage)
        {
            string query = @"
                SELECT 
                    js.stage_name,
                    SUM(me.cost) as stage_total
                FROM MaterialEntries me
                JOIN JobStages js ON me.stage_id = js.stage_id
                WHERE me.vendor_id = @vendorId
                    AND me.date BETWEEN @startDate AND @endDate
                GROUP BY js.stage_name";

            var parameters = new Dictionary<string, object>
            {
                ["@vendorId"] = vendorId,
                ["@startDate"] = startDate,
                ["@endDate"] = endDate
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                while (reader.Read())
                {
                    purchasesByStage[reader.GetString("stage_name")] = reader.GetDecimal("stage_total");
                }
            }
        }

        #endregion

        #region Weekly Summary Reports

        public class WeeklySummaryReport
        {
            public DateTime WeekStartDate { get; set; }
            public DateTime WeekEndDate { get; set; }
            public Dictionary<string, EmployeeWeeklySummary> EmployeeSummaries { get; set; }
            public decimal TotalHours { get; set; }
            public decimal TotalLaborCost { get; set; }
            public int ActiveJobs { get; set; }
            public List<string> JobsWorked { get; set; }
        }

        public class EmployeeWeeklySummary
        {
            public string EmployeeName { get; set; }
            public Dictionary<string, decimal> DailyHours { get; set; }
            public decimal TotalHours { get; set; }
            public List<string> JobsWorked { get; set; }
            public bool IsComplete { get; set; }
        }

        public WeeklySummaryReport GetWeeklySummaryReport(DateTime weekStartDate)
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

        public void ExportWeeklySummaryToCsv(DateTime weekStartDate, string filePath)
        {
            var report = GetWeeklySummaryReport(weekStartDate);
            
            using (var writer = new StreamWriter(filePath))
            {
                // Header
                writer.WriteLine($"Weekly Summary Report: {report.WeekStartDate:MM/dd/yyyy} - {report.WeekEndDate:MM/dd/yyyy}");
                writer.WriteLine();
                writer.WriteLine("Employee,Monday,Tuesday,Wednesday,Thursday,Friday,Total,Status");
                
                // Employee data
                foreach (var emp in report.EmployeeSummaries.Values)
                {
                    writer.Write($"{emp.EmployeeName},");
                    writer.Write($"{emp.DailyHours["Monday"]},");
                    writer.Write($"{emp.DailyHours["Tuesday"]},");
                    writer.Write($"{emp.DailyHours["Wednesday"]},");
                    writer.Write($"{emp.DailyHours["Thursday"]},");
                    writer.Write($"{emp.DailyHours["Friday"]},");
                    writer.Write($"{emp.TotalHours},");
                    writer.WriteLine(emp.IsComplete ? "Complete" : "Incomplete");
                }
                
                // Summary
                writer.WriteLine();
                writer.WriteLine($"Total Hours:,{report.TotalHours}");
                writer.WriteLine($"Total Labor Cost:,${report.TotalLaborCost:F2}");
                writer.WriteLine($"Active Jobs:,{report.ActiveJobs}");
                writer.WriteLine();
                writer.WriteLine("Jobs Worked:");
                foreach (var job in report.JobsWorked)
                {
                    writer.WriteLine($",{job}");
                }
            }
        }

        #endregion

        #region Estimate Reports

        public class EstimateConversionReport
        {
            public int TotalEstimates { get; set; }
            public int DraftEstimates { get; set; }
            public int SentEstimates { get; set; }
            public int ApprovedEstimates { get; set; }
            public int RejectedEstimates { get; set; }
            public int ConvertedEstimates { get; set; }
            public decimal ConversionRate { get; set; }
            public decimal TotalEstimatedValue { get; set; }
            public decimal TotalConvertedValue { get; set; }
            public List<EstimateDetail> EstimateDetails { get; set; }
        }

        public class EstimateDetail
        {
            public string EstimateNumber { get; set; }
            public string CustomerName { get; set; }
            public DateTime CreatedDate { get; set; }
            public string Status { get; set; }
            public decimal TotalPrice { get; set; }
            public string JobNumber { get; set; }
        }

        public EstimateConversionReport GetEstimateConversionReport(DateTime startDate, DateTime endDate)
        {
            var report = new EstimateConversionReport
            {
                EstimateDetails = new List<EstimateDetail>()
            };

            string query = @"
                SELECT 
                    e.estimate_number,
                    c.name as customer_name,
                    e.created_date,
                    e.status,
                    e.total_price,
                    j.job_number
                FROM Estimates e
                JOIN Customers c ON e.customer_id = c.customer_id
                LEFT JOIN Jobs j ON e.job_id = j.job_id
                WHERE e.created_date BETWEEN @startDate AND @endDate
                ORDER BY e.created_date DESC";

            var parameters = new Dictionary<string, object>
            {
                ["@startDate"] = startDate,
                ["@endDate"] = endDate
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                while (reader.Read())
                {
                    var status = reader.GetString("status");
                    var totalPrice = reader.GetDecimal("total_price");
                    
                    // Count by status
                    report.TotalEstimates++;
                    switch (status)
                    {
                        case "Draft":
                            report.DraftEstimates++;
                            break;
                        case "Sent":
                            report.SentEstimates++;
                            break;
                        case "Approved":
                            report.ApprovedEstimates++;
                            break;
                        case "Rejected":
                            report.RejectedEstimates++;
                            break;
                        case "Converted":
                            report.ConvertedEstimates++;
                            report.TotalConvertedValue += totalPrice;
                            break;
                    }
                    
                    report.TotalEstimatedValue += totalPrice;
                    
                    // Add detail
                    report.EstimateDetails.Add(new EstimateDetail
                    {
                        EstimateNumber = reader.GetString("estimate_number"),
                        CustomerName = reader.GetString("customer_name"),
                        CreatedDate = reader.GetDateTime("created_date"),
                        Status = status,
                        TotalPrice = totalPrice,
                        JobNumber = reader.IsDBNull(reader.GetOrdinal("job_number")) ? "" : reader.GetString("job_number")
                    });
                }
            }

            // Calculate conversion rate
            var sentOrApproved = report.SentEstimates + report.ApprovedEstimates + report.ConvertedEstimates + report.RejectedEstimates;
            report.ConversionRate = sentOrApproved > 0 ? (decimal)report.ConvertedEstimates / sentOrApproved * 100 : 0;

            return report;
        }

        #endregion
    }
}

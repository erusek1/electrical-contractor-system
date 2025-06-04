using System;
using System.Globalization;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents a labor entry (hours worked by an employee on a job stage)
    /// </summary>
    public class LaborEntry
    {
        /// <summary>
        /// Database ID of the labor entry
        /// </summary>
        public int EntryId { get; set; }

        /// <summary>
        /// Associated job ID
        /// </summary>
        public int JobId { get; set; }

        /// <summary>
        /// Associated employee ID
        /// </summary>
        public int EmployeeId { get; set; }

        /// <summary>
        /// Associated job stage ID
        /// </summary>
        public int StageId { get; set; }

        /// <summary>
        /// Date of the work
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Number of hours worked
        /// </summary>
        public decimal Hours { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Navigation property to Job
        /// </summary>
        public Job Job { get; set; }

        /// <summary>
        /// Navigation property to Employee
        /// </summary>
        public Employee Employee { get; set; }

        /// <summary>
        /// Navigation property to JobStage
        /// </summary>
        public JobStage Stage { get; set; }

        /// <summary>
        /// Calculates the labor cost based on employee hourly rate
        /// </summary>
        public decimal LaborCost
        {
            get
            {
                if (Employee != null)
                {
                    return Hours * Employee.HourlyRate;
                }
                // Default labor rate if employee not loaded
                return Hours * 75.0m;
            }
        }
        
        /// <summary>
        /// Gets day of week as string (Monday, Tuesday, etc.)
        /// </summary>
        public string DayOfWeek
        {
            get
            {
                return Date.DayOfWeek.ToString();
            }
        }

        /// <summary>
        /// Helper method to calculate ISO week of year for .NET Framework compatibility
        /// </summary>
        private static int GetISOWeekOfYear(DateTime date)
        {
            Calendar cal = CultureInfo.InvariantCulture.Calendar;
            DayOfWeek day = cal.GetDayOfWeek(date);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }
            return cal.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        /// <summary>
        /// Gets the week number within the year (ISO standard)
        /// </summary>
        public int WeekNumber
        {
            get
            {
                return GetISOWeekOfYear(Date);
            }
        }

        /// <summary>
        /// Gets the year-week identifier (e.g., "2023-W32")
        /// </summary>
        public string YearWeek
        {
            get
            {
                int weekNumber = GetISOWeekOfYear(Date);
                int year = weekNumber == 1 && Date.Month == 12 ? Date.Year + 1 : 
                          weekNumber >= 52 && Date.Month == 1 ? Date.Year - 1 : Date.Year;
                return $"{year}-W{weekNumber:D2}";
            }
        }
    }
}
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents an employee in the electrical contracting business
    /// </summary>
    public class Employee : INotifyPropertyChanged
    {
        private int _employeeId;
        private string _name;
        private decimal _hourlyRate;
        private decimal? _burdenRate;
        private decimal? _vehicleCostPerHour;
        private decimal? _vehicleCostPerMonth;
        private decimal? _overheadPercentage;
        private string _status;
        private string _notes;

        /// <summary>
        /// Database ID of the employee
        /// </summary>
        public int EmployeeId
        {
            get => _employeeId;
            set
            {
                _employeeId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Employee name
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Hourly rate
        /// </summary>
        public decimal HourlyRate
        {
            get => _hourlyRate;
            set
            {
                _hourlyRate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EffectiveRate));
                OnPropertyChanged(nameof(TotalHourlyCost));
                OnPropertyChanged(nameof(YearlyLaborCost));
                OnPropertyChanged(nameof(YearlyOverheadCost));
                OnPropertyChanged(nameof(TotalYearlyCost));
                OnPropertyChanged(nameof(CostPerBillableHour));
            }
        }

        /// <summary>
        /// Burden rate (additional employment costs like taxes, insurance, benefits)
        /// </summary>
        public decimal? BurdenRate
        {
            get => _burdenRate;
            set
            {
                _burdenRate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FullCostRate));
                OnPropertyChanged(nameof(EffectiveRate));
                OnPropertyChanged(nameof(TotalHourlyCost));
                OnPropertyChanged(nameof(YearlyOverheadCost));
                OnPropertyChanged(nameof(TotalYearlyCost));
                OnPropertyChanged(nameof(CostPerBillableHour));
            }
        }
        
        /// <summary>
        /// Vehicle cost per hour for this employee
        /// </summary>
        public decimal? VehicleCostPerHour
        {
            get => _vehicleCostPerHour;
            set
            {
                _vehicleCostPerHour = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EffectiveRate));
                // Update monthly cost if per hour changes (assuming 173.33 hours/month)
                if (value.HasValue)
                {
                    _vehicleCostPerMonth = value.Value * 173.33m;
                    OnPropertyChanged(nameof(VehicleCostPerMonth));
                }
            }
        }
        
        /// <summary>
        /// Vehicle cost per month for this employee
        /// </summary>
        public decimal? VehicleCostPerMonth
        {
            get => _vehicleCostPerMonth;
            set
            {
                _vehicleCostPerMonth = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalHourlyCost));
                OnPropertyChanged(nameof(YearlyVehicleCost));
                OnPropertyChanged(nameof(TotalYearlyCost));
                OnPropertyChanged(nameof(CostPerBillableHour));
                // Update hourly cost if monthly changes (assuming 173.33 hours/month)
                if (value.HasValue)
                {
                    _vehicleCostPerHour = value.Value / 173.33m;
                    OnPropertyChanged(nameof(VehicleCostPerHour));
                }
            }
        }
        
        /// <summary>
        /// Overhead percentage to apply to hourly rate
        /// </summary>
        public decimal? OverheadPercentage
        {
            get => _overheadPercentage;
            set
            {
                _overheadPercentage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EffectiveRate));
                OnPropertyChanged(nameof(TotalHourlyCost));
                OnPropertyChanged(nameof(YearlyOverheadCost));
                OnPropertyChanged(nameof(TotalYearlyCost));
                OnPropertyChanged(nameof(CostPerBillableHour));
            }
        }

        /// <summary>
        /// Employee status (Active, Inactive)
        /// </summary>
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Navigation property to collection of LaborEntries
        /// </summary>
        public ICollection<LaborEntry> LaborEntries { get; set; }

        /// <summary>
        /// Calculates the full cost rate including burden
        /// </summary>
        public decimal FullCostRate
        {
            get
            {
                if (BurdenRate.HasValue)
                {
                    return HourlyRate * (1 + (BurdenRate.Value / 100));
                }
                return HourlyRate;
            }
        }
        
        /// <summary>
        /// Calculates the effective rate including all costs
        /// Effective Rate = Base + Burden + Vehicle + (Base × Overhead%)
        /// </summary>
        public decimal EffectiveRate
        {
            get
            {
                decimal rate = HourlyRate;
                
                // Add burden rate (as fixed amount, not percentage)
                if (BurdenRate.HasValue)
                    rate += BurdenRate.Value;
                    
                // Add vehicle cost per hour
                if (VehicleCostPerHour.HasValue)
                    rate += VehicleCostPerHour.Value;
                    
                // Add overhead as percentage of base hourly rate
                if (OverheadPercentage.HasValue)
                    rate += HourlyRate * (OverheadPercentage.Value / 100);
                    
                return rate;
            }
        }

        /// <summary>
        /// Total hourly cost including all factors
        /// </summary>
        public decimal TotalHourlyCost
        {
            get
            {
                decimal cost = HourlyRate;
                
                // Add burden rate
                if (BurdenRate.HasValue)
                    cost += BurdenRate.Value;
                    
                // Add vehicle cost (converted from monthly)
                if (VehicleCostPerMonth.HasValue)
                {
                    // Assuming 173.33 hours per month (40 hours/week * 52 weeks / 12 months)
                    cost += VehicleCostPerMonth.Value / 173.33m;
                }
                    
                // Add overhead
                if (OverheadPercentage.HasValue)
                    cost = cost * (1 + OverheadPercentage.Value / 100);
                    
                return cost;
            }
        }

        /// <summary>
        /// Yearly labor cost (base hourly rate * 2080 hours)
        /// </summary>
        public decimal YearlyLaborCost => HourlyRate * 2080;

        /// <summary>
        /// Yearly vehicle cost
        /// </summary>
        public decimal YearlyVehicleCost => VehicleCostPerMonth.HasValue ? VehicleCostPerMonth.Value * 12 : 0;

        /// <summary>
        /// Yearly overhead cost
        /// </summary>
        public decimal YearlyOverheadCost
        {
            get
            {
                if (OverheadPercentage.HasValue)
                {
                    // Overhead on labor and burden
                    decimal baseCost = YearlyLaborCost;
                    if (BurdenRate.HasValue)
                        baseCost += BurdenRate.Value * 2080;
                    
                    return baseCost * (OverheadPercentage.Value / 100);
                }
                return 0;
            }
        }

        /// <summary>
        /// Total yearly cost
        /// </summary>
        public decimal TotalYearlyCost
        {
            get
            {
                decimal cost = YearlyLaborCost;
                
                // Add burden
                if (BurdenRate.HasValue)
                    cost += BurdenRate.Value * 2080;
                    
                // Add vehicle
                cost += YearlyVehicleCost;
                
                // Add overhead
                cost += YearlyOverheadCost;
                
                return cost;
            }
        }

        /// <summary>
        /// Cost per billable hour (assuming 85% billable)
        /// </summary>
        public decimal CostPerBillableHour
        {
            get
            {
                // Assuming 85% billable hours (1768 hours per year)
                decimal billableHours = 2080 * 0.85m;
                return TotalYearlyCost / billableHours;
            }
        }

        /// <summary>
        /// Returns a string representation of the employee
        /// </summary>
        public override string ToString()
        {
            return Name;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
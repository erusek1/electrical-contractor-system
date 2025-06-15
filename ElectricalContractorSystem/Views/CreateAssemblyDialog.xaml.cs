using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    public partial class CreateAssemblyDialog : Window, INotifyPropertyChanged
    {
        private PriceList _sourceItem;
        private string _assemblyCode;
        private string _assemblyName;
        private string _assemblyDescription;
        private int _roughMinutes;
        private int _finishMinutes;
        private int _serviceMinutes;
        private int _extraMinutes;

        public CreateAssemblyDialog(PriceList sourceItem)
        {
            InitializeComponent();
            DataContext = this;
            
            SourceItem = sourceItem;
            
            // Initialize with defaults
            AssemblyCode = sourceItem.ItemCode;
            AssemblyName = sourceItem.Name;
            AssemblyDescription = sourceItem.Description;
            
            // If the price list item has labor minutes, distribute them
            if (sourceItem.LaborMinutes.HasValue && sourceItem.LaborMinutes.Value > 0)
            {
                // Default distribution: 40% rough, 40% finish, 20% service
                int totalMinutes = sourceItem.LaborMinutes.Value;
                RoughMinutes = (int)(totalMinutes * 0.4);
                FinishMinutes = (int)(totalMinutes * 0.4);
                ServiceMinutes = totalMinutes - RoughMinutes - FinishMinutes;
                ExtraMinutes = 0;
            }
            else
            {
                // Default labor minutes based on common patterns
                if (sourceItem.ItemCode?.ToLower() == "o" || sourceItem.Name?.ToLower().Contains("outlet") == true)
                {
                    RoughMinutes = 30;
                    FinishMinutes = 20;
                }
                else if (sourceItem.ItemCode?.ToLower() == "s" || sourceItem.Name?.ToLower().Contains("switch") == true)
                {
                    RoughMinutes = 25;
                    FinishMinutes = 15;
                }
                else if (sourceItem.ItemCode?.ToLower() == "hh" || sourceItem.Name?.ToLower().Contains("high hat") == true)
                {
                    RoughMinutes = 45;
                    FinishMinutes = 30;
                }
                else if (sourceItem.ItemCode?.ToLower() == "gfi" || sourceItem.Name?.ToLower().Contains("gfi") == true)
                {
                    RoughMinutes = 35;
                    FinishMinutes = 25;
                }
                else
                {
                    RoughMinutes = 30;
                    FinishMinutes = 20;
                }
                ServiceMinutes = 0;
                ExtraMinutes = 0;
            }
            
            // Focus on the code textbox
            Loaded += (s, e) => AssemblyCodeTextBox.Focus();
        }

        public PriceList SourceItem
        {
            get => _sourceItem;
            set
            {
                _sourceItem = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SourceItemDescription));
            }
        }

        public string SourceItemDescription => $"From: {SourceItem?.Name} ({SourceItem?.ItemCode})";

        public string AssemblyCode
        {
            get => _assemblyCode;
            set
            {
                _assemblyCode = value;
                OnPropertyChanged();
            }
        }

        public string AssemblyName
        {
            get => _assemblyName;
            set
            {
                _assemblyName = value;
                OnPropertyChanged();
            }
        }

        public string AssemblyDescription
        {
            get => _assemblyDescription;
            set
            {
                _assemblyDescription = value;
                OnPropertyChanged();
            }
        }

        public int RoughMinutes
        {
            get => _roughMinutes;
            set
            {
                _roughMinutes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalMinutes));
                OnPropertyChanged(nameof(TotalHours));
            }
        }

        public int FinishMinutes
        {
            get => _finishMinutes;
            set
            {
                _finishMinutes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalMinutes));
                OnPropertyChanged(nameof(TotalHours));
            }
        }

        public int ServiceMinutes
        {
            get => _serviceMinutes;
            set
            {
                _serviceMinutes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalMinutes));
                OnPropertyChanged(nameof(TotalHours));
            }
        }

        public int ExtraMinutes
        {
            get => _extraMinutes;
            set
            {
                _extraMinutes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalMinutes));
                OnPropertyChanged(nameof(TotalHours));
            }
        }

        public int TotalMinutes => RoughMinutes + FinishMinutes + ServiceMinutes + ExtraMinutes;
        public double TotalHours => TotalMinutes / 60.0;

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(AssemblyCode))
            {
                MessageBox.Show("Assembly Code is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AssemblyCodeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(AssemblyName))
            {
                MessageBox.Show("Assembly Name is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TotalMinutes <= 0)
            {
                MessageBox.Show("Please enter labor minutes for at least one stage.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

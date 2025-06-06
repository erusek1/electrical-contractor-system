using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using Microsoft.Win32;
using System.Data.OleDb;

namespace ElectricalContractorSystem.ViewModels
{
    public class ImportJobsViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private readonly Window _window;
        private string _selectedFilePath;
        private ObservableCollection<ImportJobItem> _jobsToImport;
        private bool _importOnlyNew;

        public ImportJobsViewModel(DatabaseService databaseService, Window window)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _window = window;
            
            _jobsToImport = new ObservableCollection<ImportJobItem>();
            _importOnlyNew = true;

            BrowseCommand = new RelayCommand(() => ExecuteBrowse());
            ImportCommand = new RelayCommand(() => ExecuteImport(), () => CanExecuteImport());
            CancelCommand = new RelayCommand(() => ExecuteCancel());
        }

        #region Properties

        public string SelectedFilePath
        {
            get => _selectedFilePath;
            set => SetProperty(ref _selectedFilePath, value);
        }

        public ObservableCollection<ImportJobItem> JobsToImport
        {
            get => _jobsToImport;
            set => SetProperty(ref _jobsToImport, value);
        }

        public bool ImportOnlyNew
        {
            get => _importOnlyNew;
            set
            {
                if (SetProperty(ref _importOnlyNew, value))
                {
                    UpdateImportFlags();
                    OnPropertyChanged(nameof(ImportSummary));
                }
            }
        }

        public bool CanImport => JobsToImport.Any(j => j.Import);

        public string ImportSummary
        {
            get
            {
                var selectedCount = JobsToImport.Count(j => j.Import);
                var newCount = JobsToImport.Count(j => j.Status == "New");
                return $"{selectedCount} jobs selected for import ({newCount} new)";
            }
        }

        #endregion

        #region Commands

        public ICommand BrowseCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Private Methods

        private void ExecuteBrowse()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls|All Files|*.*",
                Title = "Select Jobs List Excel File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedFilePath = openFileDialog.FileName;
                LoadExcelFile();
            }
        }

        private void LoadExcelFile()
        {
            try
            {
                JobsToImport.Clear();

                // Connection string for Excel
                string connectionString;
                string extension = Path.GetExtension(SelectedFilePath).ToLower();
                
                if (extension == ".xlsx")
                {
                    connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={SelectedFilePath};Extended Properties='Excel 12.0 Xml;HDR=YES;'";
                }
                else
                {
                    connectionString = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={SelectedFilePath};Extended Properties='Excel 8.0;HDR=YES;'";
                }

                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    connection.Open();
                    
                    // Get the first sheet name
                    DataTable schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                    string sheetName = schemaTable.Rows[0]["TABLE_NAME"].ToString();
                    
                    // Query the sheet
                    string query = $"SELECT * FROM [{sheetName}]";
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        
                        // Get existing jobs from database
                        var existingJobs = _databaseService.GetAllJobs();
                        var existingJobNumbers = existingJobs.Select(j => j.JobNumber).ToHashSet();
                        
                        // Process each row
                        foreach (DataRow row in dataTable.Rows)
                        {
                            try
                            {
                                // Look for columns by common names
                                string jobNumber = GetCellValue(row, "Job #", "Job Number", "JobNumber", "Job");
                                string customerName = GetCellValue(row, "Customer", "Customer Name", "CustomerName", "Client");
                                string address = GetCellValue(row, "Address", "Job Address", "Location", "Site");
                                
                                // Skip empty rows
                                if (string.IsNullOrWhiteSpace(jobNumber))
                                    continue;
                                
                                var importItem = new ImportJobItem
                                {
                                    JobNumber = jobNumber.Trim(),
                                    CustomerName = customerName?.Trim() ?? "",
                                    Address = address?.Trim() ?? "",
                                    Status = existingJobNumbers.Contains(jobNumber.Trim()) ? "Exists" : "New",
                                    Import = !existingJobNumbers.Contains(jobNumber.Trim()),
                                    Notes = existingJobNumbers.Contains(jobNumber.Trim()) ? "Job already in database" : ""
                                };
                                
                                JobsToImport.Add(importItem);
                            }
                            catch (Exception ex)
                            {
                                // Add error item
                                JobsToImport.Add(new ImportJobItem
                                {
                                    JobNumber = "Error",
                                    Status = "Error",
                                    Notes = ex.Message,
                                    Import = false
                                });
                            }
                        }
                    }
                }

                UpdateImportFlags();
                OnPropertyChanged(nameof(ImportSummary));
                OnPropertyChanged(nameof(CanImport));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading Excel file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetCellValue(DataRow row, params string[] possibleColumnNames)
        {
            foreach (string columnName in possibleColumnNames)
            {
                if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
                {
                    return row[columnName].ToString();
                }
            }
            return null;
        }

        private void UpdateImportFlags()
        {
            foreach (var item in JobsToImport)
            {
                if (ImportOnlyNew)
                {
                    item.Import = item.Status == "New";
                }
            }
        }

        private bool CanExecuteImport()
        {
            return JobsToImport.Any(j => j.Import);
        }

        private void ExecuteImport()
        {
            try
            {
                int successCount = 0;
                int errorCount = 0;
                
                foreach (var item in JobsToImport.Where(j => j.Import))
                {
                    try
                    {
                        // Parse address
                        var addressParts = ParseAddress(item.Address);
                        
                        // Check if customer exists
                        var customers = _databaseService.GetAllCustomers();
                        var customer = customers.FirstOrDefault(c => 
                            c.Name.Equals(item.CustomerName, StringComparison.OrdinalIgnoreCase));
                        
                        if (customer == null)
                        {
                            // Create new customer
                            customer = new Customer
                            {
                                Name = item.CustomerName,
                                Address = addressParts.Street,
                                City = addressParts.City,
                                State = addressParts.State,
                                Zip = addressParts.Zip
                            };
                            
                            int customerId = _databaseService.AddCustomer(customer);
                            customer.CustomerId = customerId;
                        }
                        
                        // Create job
                        var job = new Job
                        {
                            JobNumber = item.JobNumber,
                            CustomerId = customer.CustomerId,
                            JobName = item.CustomerName,
                            Address = addressParts.Street,
                            City = addressParts.City,
                            State = addressParts.State,
                            Zip = addressParts.Zip,
                            Status = "Estimate",
                            CreateDate = DateTime.Now
                        };
                        
                        _databaseService.AddJob(job);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        item.Status = "Error";
                        item.Notes = ex.Message;
                    }
                }
                
                string message = $"Import complete!\n\nSuccessful: {successCount}\nErrors: {errorCount}";
                MessageBox.Show(message, "Import Complete", MessageBoxButton.OK, 
                    errorCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                
                if (errorCount == 0)
                {
                    _window.DialogResult = true;
                    _window.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (string Street, string City, string State, string Zip) ParseAddress(string fullAddress)
        {
            if (string.IsNullOrWhiteSpace(fullAddress))
                return ("", "", "NJ", "");
            
            // Simple parsing - can be improved
            var parts = fullAddress.Split(',').Select(p => p.Trim()).ToArray();
            
            string street = parts.Length > 0 ? parts[0] : "";
            string city = parts.Length > 1 ? parts[1] : "";
            string stateZip = parts.Length > 2 ? parts[2] : "NJ";
            
            // Try to extract state and zip from last part
            string state = "NJ";
            string zip = "";
            
            if (!string.IsNullOrWhiteSpace(stateZip))
            {
                var stateZipParts = stateZip.Split(' ').Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
                if (stateZipParts.Length >= 1)
                {
                    state = stateZipParts[0];
                    if (stateZipParts.Length >= 2)
                    {
                        zip = stateZipParts[1];
                    }
                }
            }
            
            return (street, city, state, zip);
        }

        private void ExecuteCancel()
        {
            _window.DialogResult = false;
            _window.Close();
        }

        #endregion
    }

    public class ImportJobItem : INotifyPropertyChanged
    {
        private bool _import;
        private string _status;
        private string _notes;

        public string JobNumber { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
        
        public bool Import
        {
            get => _import;
            set
            {
                _import = value;
                OnPropertyChanged(nameof(Import));
            }
        }
        
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
        
        public string Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged(nameof(Notes));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
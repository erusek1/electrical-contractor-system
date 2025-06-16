using System;
using System.Linq;
using System.Windows;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.Views
{
    /// <summary>
    /// Interaction logic for AddJobAtPropertyDialog.xaml
    /// </summary>
    public partial class AddJobAtPropertyDialog : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly PropertyService _propertyService;
        private readonly Property _property;

        public int CreatedJobId { get; private set; }

        public AddJobAtPropertyDialog(DatabaseService databaseService, Property property)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _propertyService = new PropertyService(databaseService);
            _property = property;

            InitializeDialog();
        }

        private void InitializeDialog()
        {
            // Set property information
            PropertyAddressText.Text = $"at {_property.FullAddress}";
            CustomerNameText.Text = _property.Customer?.Name ?? "Unknown Customer";
            
            var propertyDetails = $"{_property.PropertyType}";
            if (_property.SquareFootage.HasValue)
                propertyDetails += $" • {_property.SquareFootage:N0} sq ft";
            if (_property.YearBuilt.HasValue)
                propertyDetails += $" • Built {_property.YearBuilt}";
            PropertyDetailsText.Text = propertyDetails;

            // Set defaults
            StatusComboBox.SelectedIndex = 0; // Estimate
            CreateDatePicker.SelectedDate = DateTime.Today;

            // Generate next job number
            JobNumberTextBox.Text = GenerateNextJobNumber();

            // Load previous jobs at this property
            LoadPreviousJobs();
        }

        private string GenerateNextJobNumber()
        {
            try
            {
                // Get all jobs to find the highest job number
                var allJobs = _databaseService.GetAllJobs();
                
                if (allJobs.Count == 0)
                {
                    return "401"; // Starting number
                }

                // Find the highest numeric job number
                int highestNumber = 400; // Default starting point
                
                foreach (var job in allJobs)
                {
                    if (int.TryParse(job.JobNumber, out int jobNum))
                    {
                        if (jobNum > highestNumber)
                        {
                            highestNumber = jobNum;
                        }
                    }
                }

                return (highestNumber + 1).ToString();
            }
            catch
            {
                // If there's any error, return a placeholder
                return DateTime.Now.ToString("yyyyMMdd");
            }
        }

        private void LoadPreviousJobs()
        {
            try
            {
                var previousJobs = _propertyService.GetPropertyJobs(_property.PropertyId);
                PreviousJobsListBox.ItemsSource = previousJobs.OrderByDescending(j => j.CreateDate);
            }
            catch
            {
                // If error loading previous jobs, just continue
                PreviousJobsListBox.ItemsSource = null;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(JobNumberTextBox.Text))
            {
                MessageBox.Show("Please enter a job number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                JobNumberTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(JobNameTextBox.Text))
            {
                MessageBox.Show("Please enter a job name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                JobNameTextBox.Focus();
                return;
            }

            if (!CreateDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select a create date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                CreateDatePicker.Focus();
                return;
            }

            // Check for duplicate job number
            var existingJob = _databaseService.GetJobByNumber(JobNumberTextBox.Text.Trim());
            if (existingJob != null)
            {
                MessageBox.Show($"Job number {JobNumberTextBox.Text} already exists. Please use a different job number.", 
                    "Duplicate Job Number", MessageBoxButton.OK, MessageBoxImage.Warning);
                JobNumberTextBox.Focus();
                JobNumberTextBox.SelectAll();
                return;
            }

            // Parse estimate amount
            decimal? totalEstimate = null;
            if (!string.IsNullOrWhiteSpace(TotalEstimateTextBox.Text))
            {
                if (decimal.TryParse(TotalEstimateTextBox.Text.Replace("$", "").Replace(",", ""), out decimal estimate))
                {
                    totalEstimate = estimate;
                }
                else
                {
                    MessageBox.Show("Total estimate must be a valid dollar amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TotalEstimateTextBox.Focus();
                    return;
                }
            }

            try
            {
                // Create the job at the property
                var status = (StatusComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Estimate";
                
                CreatedJobId = _propertyService.CreateJobAtProperty(
                    _property.PropertyId,
                    JobNumberTextBox.Text.Trim(),
                    JobNameTextBox.Text.Trim(),
                    status,
                    CreateDatePicker.SelectedDate.Value,
                    totalEstimate,
                    string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim()
                );

                // Create default job stages for the new job
                CreateDefaultJobStages(CreatedJobId);

                MessageBox.Show($"Job {JobNumberTextBox.Text} created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating job: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateDefaultJobStages(int jobId)
        {
            try
            {
                // Create default stages for the job
                string[] stages = { "Demo", "Rough", "Service", "Finish", "Extra", "Temp Service", "Inspection", "Other" };
                
                foreach (var stageName in stages)
                {
                    _databaseService.CreateJobStage(new JobStage
                    {
                        JobId = jobId,
                        StageName = stageName,
                        EstimatedHours = 0,
                        EstimatedMaterialCost = 0,
                        ActualHours = 0,
                        ActualMaterialCost = 0
                    });
                }
            }
            catch
            {
                // If stages can't be created, the job is still valid
                // User can add stages manually later
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

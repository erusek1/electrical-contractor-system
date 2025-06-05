using System;
using System.ComponentModel;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Model for job items in the bulk status update dialog
    /// </summary>
    public class BulkUpdateJobItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public int JobId { get; set; }
        public string JobNumber { get; set; }
        public string JobName { get; set; }
        public string CustomerName { get; set; }
        public string Status { get; set; }
        public DateTime CreateDate { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
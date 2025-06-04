using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ElectricalContractorSystem.ViewModels
{
    /// <summary>
    /// Base class for ViewModels providing common functionality
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Error in PropertyChanged for {propertyName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets a property value and raises PropertyChanged if the value changed
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="backingStore">Reference to the backing field</param>
        /// <param name="value">New value</param>
        /// <param name="propertyName">Name of the property (automatically filled)</param>
        /// <returns>True if the value changed</returns>
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            try
            {
                if (EqualityComparer<T>.Default.Equals(backingStore, value))
                    return false;

                backingStore = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Error setting property {propertyName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safe way to execute an action with error handling
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="errorMessage">Optional custom error message</param>
        protected void SafeExecute(Action action, string errorMessage = null)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                var message = errorMessage ?? "An error occurred in the view model";
                System.Diagnostics.Debug.WriteLine($"{message}: {ex.Message}");
                
                // Optionally show a user-friendly message
                System.Windows.MessageBox.Show(
                    $"{message}. Please try again.",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }
    }
}

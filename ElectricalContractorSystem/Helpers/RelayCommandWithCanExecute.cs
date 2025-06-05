using System;
using System.Windows.Input;

namespace ElectricalContractorSystem.Helpers
{
    /// <summary>
    /// Extended RelayCommand that can raise CanExecuteChanged manually
    /// Used when we need to update command button states programmatically
    /// </summary>
    public class RelayCommandWithCanExecute : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommandWithCanExecute(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommandWithCanExecute(Action execute, Func<bool> canExecute = null)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            _execute = _ => execute();
            
            if (canExecute != null)
                _canExecute = _ => canExecute();
            else
                _canExecute = null;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        /// <summary>
        /// Force a re-evaluation of CanExecute for this command
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
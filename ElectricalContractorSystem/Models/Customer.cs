using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Customer model representing customer information
    /// </summary>
    public class Customer : INotifyPropertyChanged
    {
        private int _customerId;
        private string _name;
        private string _address;
        private string _city;
        private string _state;
        private string _zip;
        private string _email;
        private string _phone;
        private string _notes;

        public int CustomerId
        {
            get => _customerId;
            set => SetProperty(ref _customerId, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        public string City
        {
            get => _city;
            set => SetProperty(ref _city, value);
        }

        public string State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        public string Zip
        {
            get => _zip;
            set => SetProperty(ref _zip, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}

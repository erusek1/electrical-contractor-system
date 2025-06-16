using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    public partial class AddComponentDialog : Window, INotifyPropertyChanged
    {
        private ObservableCollection<Material> _allMaterials;
        private ObservableCollection<Material> _filteredMaterials;
        private Material _selectedMaterial;
        private decimal _quantity = 1;
        private bool _isOptional;

        public AddComponentDialog(List<Material> materials)
        {
            InitializeComponent();
            DataContext = this;
            
            _allMaterials = new ObservableCollection<Material>(materials);
            _filteredMaterials = new ObservableCollection<Material>(_allMaterials);
            
            // Focus on search box
            Loaded += (s, e) => SearchTextBox.Focus();
        }

        public ObservableCollection<Material> FilteredMaterials
        {
            get => _filteredMaterials;
            set
            {
                _filteredMaterials = value;
                OnPropertyChanged();
            }
        }

        public Material SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                _selectedMaterial = value;
                OnPropertyChanged();
            }
        }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
            }
        }

        public bool IsOptional
        {
            get => _isOptional;
            set
            {
                _isOptional = value;
                OnPropertyChanged();
            }
        }

        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                FilteredMaterials = new ObservableCollection<Material>(_allMaterials);
            }
            else
            {
                var filtered = _allMaterials.Where(m => 
                    m.Code.ToLower().Contains(searchText) || 
                    m.Name.ToLower().Contains(searchText) ||
                    (m.Category != null && m.Category.ToLower().Contains(searchText))).ToList();
                    
                FilteredMaterials = new ObservableCollection<Material>(filtered);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMaterial == null)
            {
                MessageBox.Show("Please select a material.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Quantity <= 0)
            {
                MessageBox.Show("Quantity must be greater than 0.", "Validation Error", 
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

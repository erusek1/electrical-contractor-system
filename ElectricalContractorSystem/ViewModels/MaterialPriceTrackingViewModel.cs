        private void ExecuteAddMaterial(object parameter)
        {
            try
            {
                var dialog = new AddMaterialDialog(_databaseService)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                if (dialog.ShowDialog() == true && dialog.Success)
                {
                    // Add the new material to the list
                    Materials.Add(dialog.NewMaterial);
                    ApplyFilters();
                    
                    // Select the new material
                    SelectedMaterial = dialog.NewMaterial;
                    
                    System.Windows.MessageBox.Show(
                        $"Material '{dialog.NewMaterial.Name}' added successfully!",
                        "Material Added",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error adding material: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteImportPrices(object parameter)
        {
            try
            {
                var dialog = new ImportPricesDialog(_databaseService)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                if (dialog.ShowDialog() == true && dialog.Success)
                {
                    // Refresh the material list to show updated prices
                    LoadData();
                    
                    System.Windows.MessageBox.Show(
                        $"Import completed successfully!\n" +
                        $"Updated {dialog.ImportedCount} material prices.",
                        "Import Complete",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error importing prices: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
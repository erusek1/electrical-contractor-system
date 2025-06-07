# Electrical Contractor Estimating System

A comprehensive database-driven system for managing electrical contracting business operations, with a focus on job estimation, tracking, and billing.

## Features

### Estimating Module
- **Room-by-Room Estimation**: Build detailed estimates organized by rooms
- **Quick Item Entry**: Use familiar short codes (like "hh" for recessed lights)
- **Automatic Calculations**: Labor hours and material costs calculated automatically
- **Price List Management**: Maintain your standard items with labor minutes
- **PDF Generation**: Create professional proposals for customers
- **Version Control**: Track estimate revisions

### Core Features
- Modern WPF interface replacing Excel-based workflows
- MySQL database for reliable data storage
- Labor tracking by job stage (Rough, Finish, Service, etc.)
- Material cost tracking with vendor management
- Profit analysis (Estimated vs Actual)
- Permit item counting for inspections

## System Requirements

- Windows 7 or later
- .NET Framework 4.7.2 or later
- MySQL Server 5.7 or later
- Visual Studio 2019 or later (for development)

## Installation

### 1. Database Setup

1. Install MySQL Server if not already installed
2. Create a new database:
   ```sql
   CREATE DATABASE electrical_estimating_db;
   ```
3. Run the database schema script:
   ```sql
   mysql -u your_username -p electrical_estimating_db < database/electrical_estimating_db.sql
   ```

### 2. Application Configuration

1. Open `ElectricalContractorSystem/App.config`
2. Update the connection string with your MySQL credentials:
   ```xml
   <connectionStrings>
       <add name="ElectricalDB" 
            connectionString="Server=localhost;Database=electrical_estimating_db;Uid=your_username;Pwd=your_password;" 
            providerName="MySql.Data.MySqlClient" />
   </connectionStrings>
   ```

### 3. Build and Run

1. Open `ElectricalContractorSystem.sln` in Visual Studio
2. Restore NuGet packages (MySql.Data)
3. Build the solution (F6)
4. Run the application (F5)

## Quick Start Guide

### Creating Your First Estimate

1. **Launch the application** and click "File" → "New Estimate"
2. **Select or create a customer**
3. **Add rooms** using the "Add Room" button
4. **Add items to rooms**:
   - Search by item code (e.g., "hh" for recessed lights)
   - Select the item from the price list
   - Click "Add Selected Item"
   - Adjust quantities as needed
5. **Save the estimate** using the "Save Estimate" button

### Understanding the Interface

- **Left Panel**: List of rooms in the estimate
- **Center Panel**: Items in the selected room
- **Right Panel**: Price list for adding items
- **Bottom Bar**: Running totals for labor hours and costs

## Migrating from Excel

See the `migration` folder for Python scripts to import your existing data:
- `import_price_list.py` - Import your price list from Excel
- `import_customers.py` - Import customer data
- `import_jobs.py` - Import historical job data

## Item Codes Reference

Common item codes from your Excel system:
- `hh` - 4" LED recessed light (high hat)
- `O` - Decora Outlet
- `S` - Single Pole Decora Switch
- `3W` - 3-way Decora Switch
- `Gfi` - 15a TP GFI
- `fridge` - Refrigerator receptacle
- `micro` - Microwave receptacle
- `dw` - Dishwasher receptacle

## Development

### Project Structure

```
ElectricalContractorSystem/
├── Models/          # Data models (Customer, Estimate, etc.)
├── ViewModels/      # MVVM ViewModels
├── Views/           # WPF Views (XAML)
├── Services/        # Database and business logic
├── Helpers/         # Utility classes
└── database/        # SQL schema and scripts
```

### Adding New Features

1. **Models**: Define data structures in the Models folder
2. **Database**: Update the schema if adding new tables
3. **ViewModels**: Implement business logic following MVVM pattern
4. **Views**: Create WPF interfaces in XAML

## Support

For questions or issues:
- Check the `docs` folder for detailed documentation
- Review the database schema in `database/electrical_estimating_db.sql`
- Contact: erik@erikrusekelectric.com

## License

Proprietary - Erik Rusek Electric

## Roadmap

- [ ] Invoice generation from completed jobs
- [ ] QuickBooks integration
- [ ] Mobile app for field updates
- [ ] Supplier price update integration
- [ ] Advanced reporting dashboard
- [ ] Multi-user support with permissions
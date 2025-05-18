# Electrical Contractor System - Implementation

This repository contains the implementation files for the Electrical Contractor System, a database-driven application to replace Excel-based business management for electrical contractors.

## Project Structure

### Database
- `database/electrical_contractor_db.sql` - MySQL database schema with tables for Jobs, Customers, Labor, etc.

### Data Migration
- `migration/customer_job_migration.py` - Script to import customers and jobs from Jobs List.xlsx
- `migration/material_labor_migration.py` - Script to import material and labor entries from ERE.xlsx
- `migration/price_list_migration.py` - Script to import price list from template 3.xlsx
- `migration/job_sheet_migration.py` - Script to import individual job sheets

### Application
- `ElectricalContractorSystem.sln` - Visual Studio solution file
- `ElectricalContractorSystem/ElectricalContractorSystem.csproj` - C# WPF project file
- `ElectricalContractorSystem/App.config` - Application configuration with database connection
- Core application files (App.xaml, MainWindow.xaml, etc.)

## Models

Complete data models have been implemented:

- `Customer` - Customer contact information
- `Job` - Project details including job number, address, square footage
- `JobStage` - Tracking of each project phase (Demo, Rough, Service, etc.)
- `Employee` - Employee information with hourly rates
- `LaborEntry` - Individual employee time entries
- `MaterialEntry` - Material purchases and costs
- `Vendor` - Vendor information
- `PriceList` - Catalog of standard items with pricing
- `RoomSpecification` - Room-by-room electrical specifications
- `PermitItem` - Items needed for permit applications

## Services

- `DatabaseService` - Service for handling database operations

## UI Components

- `MainWindow` - Primary application window with navigation menu
- UI converters for status indicators (colors, visibility)
- MVVM pattern implementation with ViewModelBase and RelayCommand

## Features Implemented

- Core data model structure matching database schema
- Database connection and operations service
- Application framework with navigation
- Input/output converters for UI

## Getting Started

1. Create MySQL database using the schema in `/database/electrical_contractor_db.sql`
2. Update database connection string in `ElectricalContractorSystem/App.config`
3. Run migration scripts to import existing data from Excel files
4. Open solution in Visual Studio and run the application

## Next Steps

1. Complete implementation of all views:
   - Job Management
   - Weekly Labor Entry
   - Material Entry
   - Job Cost Tracking
   - Job Details
2. Implement validation and error handling
3. Add reporting capabilities
4. Develop comprehensive testing

## Development Notes

- C# WPF application using MVVM architecture
- MySQL database backend
- Designed for easy data entry and validation
- Focus on data integrity and business rule enforcement

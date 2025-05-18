# Excel to Database Migration Plan

## Overview

This document outlines the step-by-step process for migrating your electrical contracting business data from Excel to the new database system. We'll break down the process into manageable phases to ensure a smooth transition with minimal disruption to your business operations.

## Phase 1: Data Preparation and Analysis

### Step 1: Inventory Your Excel Files
- Identify all Excel files containing business data:
  - Jobs List.xlsx (master job list)
  - ERE.xlsx (material and labor tracking)
  - Template 3.xlsx (job estimate template)
  - Individual job sheets (e.g., 619.xlsx)
- Document the structure and content of each file

### Step 2: Clean and Standardize Data
- Review all data for consistency
- Ensure customer names are standardized
- Check job numbering for consistency
- Identify and resolve any data anomalies

### Step 3: Create Data Mapping Document
- Define how each Excel field maps to database tables
- Example mapping:
  - Jobs List "Customer" column → Customers.name
  - Jobs List "Address" column → Jobs.address
  - Jobs List "Job #" column → Jobs.job_number
  - Material and Labor "Stage" column → JobStages.stage_name

## Phase 2: Database Preparation

### Step 1: Install and Configure Database System
- Set up MySQL server
- Create the electrical_contractor_db database
- Run the database schema script

### Step 2: Import Reference Data
- Import employee data
- Import vendor data
- Import price list data

## Phase 3: Data Migration Scripts

### Step 1: Create Customer and Job Migration Script

The `customer_job_migration.py` script imports your customers and jobs from the Jobs List Excel file. The script:
- Connects to the MySQL database
- Reads the Jobs List.xlsx file
- Creates unique customer records
- Creates corresponding job records with proper status
- Maps addresses to street/city/state/zip fields

### Step 2: Create Material and Labor Migration Script

The `material_labor_migration.py` script imports labor hours and material costs from ERE.xlsx. The script:
- Connects to the MySQL database
- Reads the ERE.xlsx Material and Labor sheet
- Maps employees, vendors, and jobs to their database IDs
- Creates or updates job stages as needed
- Creates labor entries for hours worked
- Creates material entries for purchases
- Validates data consistency during import

### Step 3: Create Price List Migration Script

The `price_list_migration.py` script imports your price list from Template 3.xlsx. The script:
- Connects to the MySQL database
- Reads the Template 3.xlsx Price List sheet
- Maps item codes, descriptions, and prices
- Adds tax rates and markup percentages
- Creates PriceList records in the database

### Step 4: Create Individual Job Sheet Migration Script

The `job_sheet_migration.py` script imports detailed job information from individual job sheets. The script:
- Connects to the MySQL database
- Processes job files from the job_sheets directory
- Updates jobs with square footage and floors information
- Imports estimated vs. actual data for each stage
- Imports room-by-room specifications
- Imports permit items

## Phase 4: Migration Execution and Verification

### Step 1: Preparation
- Create a backup of all your Excel files
- Set up a test database for initial migration testing
- Test the migration scripts on a small subset of data

### Step 2: Full Migration
1. Run the customer and job migration script
2. Run the price list migration script
3. Run the material and labor migration script
4. Run the job sheet migration script

### Step 3: Verification
- Check database record counts against Excel
- Spot-check individual records for accuracy
- Verify calculations match your Excel formulas
- Run key reports and compare to Excel reports

### Step 4: Post-Migration Tuning
- Optimize database indexes for performance
- Adjust any field sizes or constraints as needed
- Document any data inconsistencies found

## Phase 5: Parallel Operation

### Step 1: Start Using the Database System
- Begin entering new jobs in the database system
- Continue to maintain Excel for a short period

### Step 2: Fine-Tune and Adjust
- Make necessary adjustments to the database or interface
- Address any user feedback
- Document processes and procedures

### Step 3: Complete Transition
- Once confident in the new system, discontinue Excel updates
- Archive Excel files for historical reference
- Fully transition to database-driven operation

## Important Notes

- This migration plan assumes a relatively small dataset typical of a small to medium-sized electrical contractor
- For larger datasets (thousands of jobs), additional optimization may be necessary
- All scripts should be run first on a test database before executing on production
- Regular database backups should be implemented before, during, and after migration

## Migration Scripts Usage Guide

### Prerequisites

Before running the migration scripts:
1. Install Python 3.8 or later
2. Install required Python packages:
   ```
   pip install pandas mysql-connector-python openpyxl
   ```
3. Set up MySQL database using the schema in `/database/electrical_contractor_db.sql`

### Configuration

Update the database connection settings in each script:
```python
db_config = {
    "host": "localhost",
    "user": "your_db_username",
    "password": "your_db_password",
    "database": "electrical_contractor_db"
}
```

### Running the Scripts

Execute the scripts in this order:

1. First, migrate customers and jobs:
   ```
   python customer_job_migration.py
   ```

2. Next, import the price list:
   ```
   python price_list_migration.py
   ```

3. Then, import labor and material entries:
   ```
   python material_labor_migration.py
   ```

4. Finally, import individual job sheets:
   ```
   python job_sheet_migration.py
   ```

### Troubleshooting

Common issues:
- **File not found errors**: Ensure Excel files are in the correct location
- **Database connection errors**: Verify database credentials and connection parameters
- **Data format issues**: Check for inconsistencies in Excel data format
- **Missing references**: Ensure employees and vendors exist before importing labor/material entries

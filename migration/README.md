# Data Migration Scripts

This folder contains scripts for migrating data from Excel to the database system.

## Scripts

- `customer_job_migration.py` - Import customers and jobs from Jobs List.xlsx
- `material_labor_migration.py` - Import material and labor entries from ERE.xlsx
- `price_list_migration.py` - Import price list from template 3.xlsx
- `job_sheet_migration.py` - Import individual job sheets

## Usage Instructions

1. Make sure MySQL database is set up using the schema in /database
2. Configure database connection information in each script
3. Run scripts in the following order:
   - customer_job_migration.py
   - price_list_migration.py
   - material_labor_migration.py
   - job_sheet_migration.py
4. Verify data after each migration step

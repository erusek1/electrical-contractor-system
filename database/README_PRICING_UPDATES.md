# Database Update Scripts - Pricing and Assembly System

## Overview
These scripts add the new pricing, assembly management, and integration features to your electrical contractor database.

## Scripts Created

1. **add_pricing_tables.sql** - Creates all new tables for:
   - Materials tracking with price history
   - Assembly templates (your quick codes like 'o', 's', 'hh')
   - Assembly components and variants
   - Service types with multipliers
   - Difficulty presets for job complexity
   - Labor adjustments tracking

2. **add_integration_columns.sql** - Updates existing tables:
   - Adds estimate_id to Jobs table
   - Adds assembly_id to EstimateLineItems
   - Adds service_type_id to Estimates and Jobs
   - Creates EstimateConversions tracking table
   - Adds distance and drive time to Jobs

3. **run_all_updates.sql** - Master script that runs everything in order

4. **populate_sample_assemblies.sql** - Adds sample data:
   - Common materials (outlets, switches, wire, etc.)
   - Assembly templates for your quick codes
   - Variants (like Decora outlet vs Duplex outlet)
   - Links assemblies to materials with quantities

## How to Run

### Option 1: Run the Master Script (Recommended)
```sql
mysql -u your_username -p electrical_contractor_db < run_all_updates.sql
```

### Option 2: Run Individual Scripts
Run in this order:
1. First, make sure the estimating tables exist:
   ```sql
   mysql -u your_username -p electrical_contractor_db < add_estimating_tables.sql
   ```

2. Add pricing and assembly tables:
   ```sql
   mysql -u your_username -p electrical_contractor_db < add_pricing_tables.sql
   ```

3. Add integration columns:
   ```sql
   mysql -u your_username -p electrical_contractor_db < add_integration_columns.sql
   ```

4. (Optional) Add sample assembly data:
   ```sql
   mysql -u your_username -p electrical_contractor_db < populate_sample_assemblies.sql
   ```

## What Gets Added

### New Tables
- **Materials** - Raw components with current pricing
- **MaterialPriceHistory** - Track all price changes
- **AssemblyTemplates** - Your quick codes (o, s, hh, etc.)
- **AssemblyComponents** - What materials make up each assembly
- **AssemblyVariants** - Different versions of same code
- **ServiceTypes** - Residential, Service Call, Commercial, Emergency
- **DifficultyPresets** - Beach house, old construction, etc.
- **LaborAdjustments** - Track changes to labor estimates
- **EstimateConversions** - Track estimates that became jobs
- **AssemblyUsageStats** - Track which assemblies get used most

### Default Data
- 4 Service Types (with multipliers)
- 24 Difficulty Presets in 4 categories
- Sample assemblies if you run populate_sample_assemblies.sql

### Views Created
- **vw_material_price_changes** - See price changes and percentages
- **vw_assembly_costs** - Current cost for each assembly
- **vw_assembly_with_variants** - All assemblies with their variants
- **vw_estimate_job_tracking** - Compare estimates to actual jobs

## Verification
After running, check that everything was created:
```sql
-- Check new tables
SELECT COUNT(*) AS New_Tables 
FROM information_schema.TABLES 
WHERE TABLE_SCHEMA = 'electrical_contractor_db' 
AND TABLE_NAME IN ('Materials', 'AssemblyTemplates', 'ServiceTypes', 'DifficultyPresets');

-- Check default data
SELECT COUNT(*) AS ServiceTypes FROM ServiceTypes;
SELECT COUNT(*) AS DifficultyPresets FROM DifficultyPresets;

-- If you ran the sample data
SELECT COUNT(*) AS Assemblies FROM AssemblyTemplates;
SELECT COUNT(*) AS Materials FROM Materials;
```

## Next Steps
After running these scripts:
1. Import your Excel price list using the migration tools
2. Test creating estimates with the new assembly system
3. Configure your preferred difficulty presets
4. Start tracking material price changes

## Troubleshooting
- If you get foreign key errors, make sure you ran add_estimating_tables.sql first
- If tables already exist, you can drop them first or modify the scripts
- Check that you're using the correct database name (electrical_contractor_db)
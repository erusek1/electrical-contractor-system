# Fix for Assembly Management and Material Price Tracking Issues

## Overview

This document explains the fixes for two issues:
1. Unable to add items to new assemblies
2. Material Price Tracking should use PriceList items instead of pre-populated materials

## Issue 1: Assembly Management - Can't Add Items

### Problem
When adding a new component to an assembly, the component wasn't being saved properly and the ComponentId wasn't being returned from the database.

### Solution
Updated the following files:
- `ElectricalContractorSystem/ViewModels/AssemblyManagementViewModel.cs`
- `ElectricalContractorSystem/Services/DatabaseServicePricingExtensions.cs`

#### Changes Made:
1. Modified `SaveComponent` method in AssemblyManagementViewModel to:
   - Set the Material reference on the new component
   - Get the generated ComponentId after saving
   - Reload the saved component from the database to ensure it has all properties
   - Clear the material selection after saving

2. Updated `SaveAssemblyComponent` in DatabaseServicePricingExtensions to:
   - Use `SELECT LAST_INSERT_ID()` to return the generated ComponentId
   - Set the ComponentId on the component object after insertion

## Issue 2: Material Price Tracking - Use PriceList Instead of Materials

### Problem
The Material Price Tracking was designed to use a separate Materials table, but you want it to use your existing PriceList items.

### Solution
Created a migration script to convert PriceList items to Materials so they can be used in the Material Price Tracking system.

#### Migration Script: `migration/migrate_pricelist_to_materials.py`

This script:
1. Creates the Materials table if it doesn't exist
2. Copies all active items from PriceList to Materials
3. Creates initial price history entries
4. Preserves your existing item codes and pricing

## How to Apply These Fixes

### Step 1: Update Your Database Schema
Run the pricing tables script to add the new tables:
```sql
mysql -u your_username -p electrical_contractor_db < database/add_pricing_tables.sql
```

### Step 2: Run the Migration Script
```bash
cd migration
python migrate_pricelist_to_materials.py
```

The script will:
- Prompt you to confirm before proceeding
- Create the Materials and MaterialPriceHistory tables if needed
- Copy all your PriceList items to Materials
- Skip any duplicate item codes
- Create initial price history entries
- Show a summary of items migrated

### Step 3: Update Your Connection String
Edit the migration script and update the database connection settings:
```python
DB_CONFIG = {
    'host': 'localhost',
    'user': 'your_mysql_username',  # Update this
    'password': 'your_mysql_password',  # Update this
    'database': 'electrical_contractor_db'
}
```

### Step 4: Rebuild Your Application
The C# code changes have already been committed to your repository. Simply rebuild your application in Visual Studio.

## Benefits

### Assembly Management
- You can now successfully add components to assemblies
- Components display properly with their material information
- The total material cost calculates correctly

### Material Price Tracking
- All your PriceList items are now available in Material Price Tracking
- You can track price changes over time
- Price alerts will notify you of significant changes
- Historical price data helps with purchasing decisions

## Future Enhancements

### Option 1: Direct PriceList Integration
If you prefer not to duplicate data, we could modify the Material Price Tracking to work directly with the PriceList table. This would require:
- Modifying MaterialPriceTrackingViewModel to load from PriceList
- Creating a PriceListHistory table
- Updating the PricingService to work with PriceList

### Option 2: Unified Pricing System
Consolidate PriceList and Materials into a single table with:
- All the features of both systems
- No data duplication
- Seamless integration with both job tracking and estimating

## Troubleshooting

### If the migration fails:
1. Check your MySQL credentials
2. Ensure the electrical_contractor_db database exists
3. Verify you have CREATE TABLE permissions
4. Check that the Vendors table exists (required for foreign key)

### If components still won't add:
1. Check the browser console for JavaScript errors
2. Verify the AssemblyComponents table exists in your database
3. Ensure the Materials table has data (run the migration)
4. Check that the component_id column is set to AUTO_INCREMENT

## Testing

### Test Assembly Components:
1. Open Assembly Management
2. Select or create an assembly
3. Select a material from the dropdown
4. Click "Add Component"
5. Enter a quantity
6. Click "Save"
7. Verify the component appears in the list

### Test Material Price Tracking:
1. Open Material Price Tracking
2. Verify all your price list items appear
3. Select an item
4. Update its price
5. Check that price history is recorded

## Notes

- The migration script preserves your original PriceList data
- Material codes are unique, so duplicates are skipped
- Tax rate defaults to 6.4% if not specified in PriceList
- Initial price history entries are created with "Migration Script" as the user

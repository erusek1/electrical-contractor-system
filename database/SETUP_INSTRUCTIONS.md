# Database Setup Instructions

## Adding Pricing and Assembly Tables

The Assembly Management and Material Price Tracking features require additional tables in your database. Follow these steps to add them:

### Method 1: Using MySQL Workbench

1. Open MySQL Workbench
2. Connect to your MySQL server
3. Open the SQL script file: `database/add_pricing_assembly_tables.sql`
4. Execute the script by clicking the lightning bolt icon or pressing Ctrl+Shift+Enter
5. The script will create all necessary tables and add sample data

### Method 2: Using Command Line

1. Open a command prompt or terminal
2. Navigate to the project directory
3. Run the following command:
   ```
   mysql -u your_username -p electrical_contractor_db < database/add_pricing_assembly_tables.sql
   ```
4. Enter your MySQL password when prompted

### Method 3: Using phpMyAdmin

1. Open phpMyAdmin in your web browser
2. Select the `electrical_contractor_db` database
3. Click on the "SQL" tab
4. Copy and paste the contents of `database/add_pricing_assembly_tables.sql`
5. Click "Go" to execute the script

## What This Script Creates

The script adds the following tables to your existing database:

1. **Materials** - Raw materials/components with pricing
2. **MaterialPriceHistory** - Track price changes over time
3. **AssemblyTemplates** - Your quick codes (o, s, hh, etc.)
4. **AssemblyComponents** - Links materials to assemblies
5. **AssemblyVariants** - Different versions of same assembly
6. **DifficultyPresets** - Job complexity multipliers
7. **LaborAdjustments** - Track labor adjustments
8. **ServiceTypes** - Different service rate multipliers

## Sample Data Included

The script includes sample data to get you started:

### Materials
- Electrical boxes
- Outlets and switches (regular and 3-way)
- GFCI outlets
- Wire (12-2 and 14-2 Romex)
- LED recessed lights

### Assemblies (Quick Codes)
- **o** - Decora Outlet
- **s** - Single Pole Switch
- **3w** - 3-Way Switch
- **gfi** - GFCI Outlet
- **hh** - 4" LED Recessed Light

### Difficulty Presets
- New Construction (0.9x multiplier)
- Standard Renovation (1.0x baseline)
- Old House Pre-1960 (1.2x multiplier)
- Beach House (1.2x rough, 1.1x finish)
- Occupied Home (1.1x all stages)
- December Work (1.15x all stages)

### Service Types
- Residential (1.0x rate)
- Service Call (1.5x rate)
- Commercial (1.1x rate)
- Emergency (2.0x rate)

## Verifying the Installation

After running the script, you can verify the tables were created:

```sql
USE electrical_contractor_db;
SHOW TABLES LIKE '%Materials%';
SHOW TABLES LIKE '%Assembly%';
```

You should see:
- Materials
- MaterialPriceHistory
- AssemblyTemplates
- AssemblyComponents
- AssemblyVariants

## Troubleshooting

If you encounter errors:

1. **Table already exists**: The script uses `CREATE TABLE IF NOT EXISTS`, so this shouldn't happen. If it does, you may have partial tables from a previous attempt.

2. **Foreign key constraint fails**: Make sure your Vendors table exists and has data before running this script.

3. **Access denied**: Ensure you're using a MySQL user with CREATE TABLE permissions.

## Next Steps

Once the tables are created:

1. Launch the application
2. Navigate to Tools → Assembly Management
3. You should see the sample assemblies
4. Navigate to Tools → Material Price Tracking
5. You should see the sample materials

You can now start adding your own materials and assemblies!
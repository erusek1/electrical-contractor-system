# Database Schema for Electrical Contractor System

This directory contains SQL files for setting up the database schema for the electrical contractor management system.

## Database Setup Instructions

### Option 1: Integrated Database (Recommended)
Use this if you're setting up the system for the first time:

1. Open MySQL Workbench or your preferred MySQL client
2. Connect to your MySQL server
3. Run the following command to create the database:
   ```sql
   CREATE DATABASE IF NOT EXISTS electrical_contractor_db;
   USE electrical_contractor_db;
   ```
4. Run the `integrated_database.sql` script:
   ```
   source /path/to/integrated_database.sql
   ```

This creates all tables including both job tracking and estimating features.

### Option 2: Add Estimating to Existing Database
Use this if you already have the job tracking system working:

1. Connect to your existing `electrical_contractor_db` database
2. Run the `add_estimating_tables.sql` script:
   ```
   USE electrical_contractor_db;
   source /path/to/add_estimating_tables.sql
   ```

This adds only the new estimating-related tables.

## Files in this Directory

- `electrical_contractor_db.sql` - Original job tracking tables only
- `electrical_estimating_db.sql` - Estimating tables only (standalone)
- `add_estimating_tables.sql` - Script to add estimating tables to existing database
- `integrated_database.sql` - Complete database with all features
- `inventory_tracking_tables.sql` - Future inventory tracking feature (not yet implemented)

## Troubleshooting

If you get "table doesn't exist" errors when clicking on Estimates:
1. You probably need to run `add_estimating_tables.sql`
2. Make sure you're connected to the correct database
3. Verify tables were created: `SHOW TABLES;`

Required tables for estimating features:
- Estimates
- EstimateRooms
- EstimateLineItems
- EstimateStageSummary

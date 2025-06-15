-- Fix vehicle cost per hour calculation
-- This script checks if columns exist before adding them and updates the calculation

-- Check and add vehicle_cost_per_hour if it doesn't exist
SET @dbname = DATABASE();
SET @tablename = 'Employees';
SET @columnname = 'vehicle_cost_per_hour';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE table_schema = @dbname
      AND table_name = @tablename
      AND column_name = @columnname
  ) > 0,
  'SELECT "Column vehicle_cost_per_hour already exists" AS message',
  'ALTER TABLE Employees ADD COLUMN vehicle_cost_per_hour DECIMAL(10,2) NULL AFTER burden_rate'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Check and add vehicle_cost_per_month if it doesn't exist
SET @columnname = 'vehicle_cost_per_month';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE table_schema = @dbname
      AND table_name = @tablename
      AND column_name = @columnname
  ) > 0,
  'SELECT "Column vehicle_cost_per_month already exists" AS message',
  'ALTER TABLE Employees ADD COLUMN vehicle_cost_per_month DECIMAL(10,2) NULL AFTER vehicle_cost_per_hour'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Check and add overhead_percentage if it doesn't exist
SET @columnname = 'overhead_percentage';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE table_schema = @dbname
      AND table_name = @tablename
      AND column_name = @columnname
  ) > 0,
  'SELECT "Column overhead_percentage already exists" AS message',
  'ALTER TABLE Employees ADD COLUMN overhead_percentage DECIMAL(5,2) NULL AFTER vehicle_cost_per_month'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Update vehicle_cost_per_hour calculation
-- Formula: vehicle_cost_per_hour = vehicle_cost_per_month / 173.33 (average work hours per month)
UPDATE Employees 
SET vehicle_cost_per_hour = ROUND(vehicle_cost_per_month / 173.33, 2)
WHERE vehicle_cost_per_month IS NOT NULL 
  AND vehicle_cost_per_month > 0
  AND (vehicle_cost_per_hour IS NULL OR vehicle_cost_per_hour = 0);

-- Show the updated records
SELECT 
    name,
    hourly_rate,
    burden_rate,
    vehicle_cost_per_month,
    vehicle_cost_per_hour,
    overhead_percentage,
    ROUND(hourly_rate + COALESCE(burden_rate, 0) + COALESCE(vehicle_cost_per_hour, 0), 2) AS total_hourly_cost
FROM Employees
WHERE status = 'Active'
ORDER BY name;

-- Show a summary of the update
SELECT 
    COUNT(*) AS employees_updated,
    AVG(vehicle_cost_per_hour) AS avg_vehicle_cost_per_hour,
    MIN(vehicle_cost_per_hour) AS min_vehicle_cost_per_hour,
    MAX(vehicle_cost_per_hour) AS max_vehicle_cost_per_hour
FROM Employees
WHERE vehicle_cost_per_hour IS NOT NULL AND vehicle_cost_per_hour > 0;

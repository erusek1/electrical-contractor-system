-- Update vehicle_cost_per_hour with safe update mode handling
-- This script handles MySQL's safe update mode

-- Option 1: Temporarily disable safe update mode for this session
SET SQL_SAFE_UPDATES = 0;

-- Update vehicle_cost_per_hour calculation
-- Formula: vehicle_cost_per_hour = vehicle_cost_per_month / 173.33 (average work hours per month)
UPDATE Employees 
SET vehicle_cost_per_hour = ROUND(vehicle_cost_per_month / 173.33, 2)
WHERE vehicle_cost_per_month IS NOT NULL 
  AND vehicle_cost_per_month > 0;

-- Re-enable safe update mode
SET SQL_SAFE_UPDATES = 1;

-- Show the results
SELECT 
    employee_id,
    name,
    hourly_rate,
    burden_rate,
    vehicle_cost_per_month,
    vehicle_cost_per_hour,
    overhead_percentage,
    ROUND(hourly_rate + COALESCE(burden_rate, 0) + COALESCE(vehicle_cost_per_hour, 0), 2) AS total_hourly_cost,
    status
FROM Employees
ORDER BY name;

-- Alternative Option 2: Use the primary key in the WHERE clause
-- This satisfies safe update mode without disabling it
/*
UPDATE Employees 
SET vehicle_cost_per_hour = ROUND(vehicle_cost_per_month / 173.33, 2)
WHERE employee_id > 0  -- This uses the KEY column
  AND vehicle_cost_per_month IS NOT NULL 
  AND vehicle_cost_per_month > 0;
*/

-- Option 3: Update specific employees by ID
-- If you want to update specific employees only
/*
UPDATE Employees 
SET vehicle_cost_per_hour = ROUND(vehicle_cost_per_month / 173.33, 2)
WHERE employee_id IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)  -- Add all your employee IDs
  AND vehicle_cost_per_month IS NOT NULL 
  AND vehicle_cost_per_month > 0;
*/

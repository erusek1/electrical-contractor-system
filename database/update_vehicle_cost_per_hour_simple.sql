-- Simple update script for vehicle_cost_per_hour calculation
-- This script updates the vehicle_cost_per_hour based on vehicle_cost_per_month

-- First, let's check what columns exist in the Employees table
SHOW COLUMNS FROM Employees;

-- Update vehicle_cost_per_hour calculation
-- Formula: vehicle_cost_per_hour = vehicle_cost_per_month / 173.33 (average work hours per month)
UPDATE Employees 
SET vehicle_cost_per_hour = ROUND(vehicle_cost_per_month / 173.33, 2)
WHERE vehicle_cost_per_month IS NOT NULL 
  AND vehicle_cost_per_month > 0;

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

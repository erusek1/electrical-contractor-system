-- Debug script to check employee data and verify updates

-- 1. Show all employees with their complete data
SELECT 
    employee_id,
    name,
    hourly_rate,
    burden_rate,
    vehicle_cost_per_hour,
    vehicle_cost_per_month,
    overhead_percentage,
    status,
    notes,
    -- Calculate effective rate
    ROUND(hourly_rate + COALESCE(burden_rate, 0) + COALESCE(vehicle_cost_per_hour, 0), 2) AS calculated_effective_rate
FROM Employees
ORDER BY name;

-- 2. Check for any null vehicle_cost_per_hour where vehicle_cost_per_month exists
SELECT 
    employee_id,
    name,
    vehicle_cost_per_month,
    vehicle_cost_per_hour,
    ROUND(vehicle_cost_per_month / 173.33, 2) AS calculated_per_hour
FROM Employees
WHERE vehicle_cost_per_month IS NOT NULL 
  AND vehicle_cost_per_month > 0
  AND (vehicle_cost_per_hour IS NULL OR vehicle_cost_per_hour = 0);

-- 3. Show a detailed breakdown of costs for active employees
SELECT 
    name AS 'Employee',
    CONCAT('$', FORMAT(hourly_rate, 2)) AS 'Base Rate',
    CONCAT('$', FORMAT(COALESCE(burden_rate, 0), 2)) AS 'Burden',
    CONCAT('$', FORMAT(COALESCE(vehicle_cost_per_hour, 0), 2)) AS 'Vehicle/Hr',
    CONCAT(FORMAT(COALESCE(overhead_percentage, 0), 1), '%') AS 'Overhead %',
    CONCAT('$', FORMAT(
        hourly_rate + 
        COALESCE(burden_rate, 0) + 
        COALESCE(vehicle_cost_per_hour, 0) + 
        (hourly_rate * COALESCE(overhead_percentage, 0) / 100), 
        2)) AS 'Total Cost/Hr'
FROM Employees
WHERE status = 'Active'
ORDER BY name;

-- 4. Check column data types and constraints
DESCRIBE Employees;

-- 5. Show recent changes (if you have audit logging)
-- This won't work unless you have audit tables, but shows the concept
/*
SELECT 
    'Employee Update' AS action,
    NOW() AS check_time,
    COUNT(*) AS total_employees,
    SUM(CASE WHEN vehicle_cost_per_hour IS NOT NULL THEN 1 ELSE 0 END) AS with_vehicle_cost,
    SUM(CASE WHEN overhead_percentage IS NOT NULL THEN 1 ELSE 0 END) AS with_overhead
FROM Employees;
*/

-- 6. Test update for a specific employee (replace 'Erik' with actual employee name)
-- This shows what the update would do without actually doing it
SELECT 
    employee_id,
    name,
    'UPDATE Employees SET vehicle_cost_per_hour = ' || 
    COALESCE(vehicle_cost_per_hour, 'NULL') || 
    ' WHERE employee_id = ' || employee_id AS update_statement
FROM Employees
WHERE name = 'Erik';

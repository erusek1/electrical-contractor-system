-- Add support for mixed mode estimating (Assembly vs PriceList)
-- This script updates the EstimateLineItems table to support both entry modes

-- Add new columns to EstimateLineItems for mixed mode support
ALTER TABLE EstimateLineItems
ADD COLUMN assembly_id INT NULL AFTER item_id,
ADD COLUMN entry_mode ENUM('Assembly', 'PriceList') DEFAULT 'PriceList' AFTER assembly_id,
ADD COLUMN rough_labor_minutes INT NULL AFTER labor_minutes,
ADD COLUMN finish_labor_minutes INT NULL AFTER rough_labor_minutes,
ADD COLUMN service_labor_minutes INT NULL AFTER finish_labor_minutes,
ADD COLUMN extra_labor_minutes INT NULL AFTER service_labor_minutes;

-- Add foreign key for assembly reference
ALTER TABLE EstimateLineItems
ADD CONSTRAINT fk_estimatelineitems_assembly
FOREIGN KEY (assembly_id) REFERENCES AssemblyTemplates(assembly_id)
ON DELETE SET NULL
ON UPDATE CASCADE;

-- Create view for estimate labor breakdown by stage
CREATE OR REPLACE VIEW EstimateLaborByStage AS
SELECT 
    eli.estimate_id,
    SUM(CASE 
        WHEN eli.entry_mode = 'Assembly' THEN eli.quantity * COALESCE(eli.rough_labor_minutes, 0)
        ELSE 0 
    END) / 60.0 AS rough_hours,
    SUM(CASE 
        WHEN eli.entry_mode = 'Assembly' THEN eli.quantity * COALESCE(eli.finish_labor_minutes, 0)
        ELSE 0 
    END) / 60.0 AS finish_hours,
    SUM(CASE 
        WHEN eli.entry_mode = 'Assembly' THEN eli.quantity * COALESCE(eli.service_labor_minutes, 0)
        ELSE 0 
    END) / 60.0 AS service_hours,
    SUM(CASE 
        WHEN eli.entry_mode = 'Assembly' THEN eli.quantity * COALESCE(eli.extra_labor_minutes, 0)
        ELSE 0 
    END) / 60.0 AS extra_hours,
    SUM(CASE 
        WHEN eli.entry_mode = 'PriceList' THEN eli.quantity * eli.labor_minutes
        ELSE 0 
    END) / 60.0 AS unassigned_hours,
    SUM(eli.quantity * CASE 
        WHEN eli.entry_mode = 'Assembly' THEN 
            COALESCE(eli.rough_labor_minutes, 0) + 
            COALESCE(eli.finish_labor_minutes, 0) + 
            COALESCE(eli.service_labor_minutes, 0) + 
            COALESCE(eli.extra_labor_minutes, 0)
        ELSE eli.labor_minutes 
    END) / 60.0 AS total_hours
FROM EstimateLineItems eli
INNER JOIN EstimateRooms er ON eli.room_id = er.room_id
GROUP BY eli.estimate_id;

-- Sample query to show estimates with labor breakdown
/*
SELECT 
    e.estimate_number,
    e.job_name,
    e.customer_id,
    elbs.rough_hours,
    elbs.finish_hours,
    elbs.service_hours,
    elbs.extra_hours,
    elbs.unassigned_hours,
    elbs.total_hours,
    e.total_price
FROM Estimates e
LEFT JOIN EstimateLaborByStage elbs ON e.estimate_id = elbs.estimate_id
ORDER BY e.estimate_number DESC;
*/

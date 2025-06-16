-- Add Properties table to track locations separately from jobs
-- This allows multiple jobs at the same address

-- -----------------------------------------------------
-- Table `Properties`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Properties` (
  `property_id` INT NOT NULL AUTO_INCREMENT,
  `customer_id` INT NOT NULL,
  `address` VARCHAR(255) NOT NULL,
  `city` VARCHAR(50) NULL,
  `state` VARCHAR(2) NULL,
  `zip` VARCHAR(10) NULL,
  `property_type` ENUM('Residential', 'Commercial', 'Industrial', 'Other') NOT NULL DEFAULT 'Residential',
  `square_footage` INT NULL,
  `num_floors` INT NULL,
  `year_built` INT NULL,
  `electrical_panel_info` TEXT NULL,
  `notes` TEXT NULL,
  `created_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`property_id`),
  INDEX `idx_properties_customer` (`customer_id` ASC),
  INDEX `idx_properties_address` (`address` ASC, `city` ASC, `state` ASC),
  CONSTRAINT `fk_Properties_Customers`
    FOREIGN KEY (`customer_id`)
    REFERENCES `Customers` (`customer_id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION
);

-- -----------------------------------------------------
-- Add property_id to Jobs table
-- -----------------------------------------------------
ALTER TABLE `Jobs` 
ADD COLUMN `property_id` INT NULL AFTER `customer_id`,
ADD INDEX `idx_jobs_property` (`property_id` ASC),
ADD CONSTRAINT `fk_Jobs_Properties`
  FOREIGN KEY (`property_id`)
  REFERENCES `Properties` (`property_id`)
  ON DELETE NO ACTION
  ON UPDATE NO ACTION;

-- -----------------------------------------------------
-- Create view for jobs with property info
-- -----------------------------------------------------
CREATE OR REPLACE VIEW `JobsWithProperty` AS
SELECT 
    j.*,
    p.property_type,
    p.year_built,
    p.electrical_panel_info,
    CASE 
        WHEN p.property_id IS NOT NULL THEN p.address
        ELSE j.address
    END AS display_address,
    CASE 
        WHEN p.property_id IS NOT NULL THEN p.city
        ELSE j.city
    END AS display_city,
    CASE 
        WHEN p.property_id IS NOT NULL THEN p.state
        ELSE j.state
    END AS display_state,
    CASE 
        WHEN p.property_id IS NOT NULL THEN p.zip
        ELSE j.zip
    END AS display_zip,
    CASE 
        WHEN p.property_id IS NOT NULL THEN p.square_footage
        ELSE j.square_footage
    END AS display_square_footage,
    CASE 
        WHEN p.property_id IS NOT NULL THEN p.num_floors
        ELSE j.num_floors
    END AS display_num_floors
FROM Jobs j
LEFT JOIN Properties p ON j.property_id = p.property_id;

-- -----------------------------------------------------
-- Create view for property job history
-- -----------------------------------------------------
CREATE OR REPLACE VIEW `PropertyJobHistory` AS
SELECT 
    p.property_id,
    p.address,
    p.city,
    p.state,
    p.zip,
    p.property_type,
    c.name AS customer_name,
    j.job_id,
    j.job_number,
    j.job_name,
    j.status,
    j.create_date,
    j.completion_date,
    j.total_estimate,
    j.total_actual
FROM Properties p
INNER JOIN Customers c ON p.customer_id = c.customer_id
LEFT JOIN Jobs j ON p.property_id = j.property_id
ORDER BY p.property_id, j.create_date DESC;

-- -----------------------------------------------------
-- Migration script to create properties from existing jobs
-- This creates one property per unique address
-- -----------------------------------------------------
INSERT INTO Properties (customer_id, address, city, state, zip, square_footage, num_floors)
SELECT DISTINCT 
    customer_id,
    address,
    city,
    state,
    zip,
    MAX(square_footage) as square_footage,
    MAX(num_floors) as num_floors
FROM Jobs
WHERE address IS NOT NULL AND address != ''
GROUP BY customer_id, address, city, state, zip;

-- Update existing jobs to link to properties
UPDATE Jobs j
INNER JOIN Properties p ON 
    j.customer_id = p.customer_id 
    AND j.address = p.address 
    AND IFNULL(j.city, '') = IFNULL(p.city, '')
    AND IFNULL(j.state, '') = IFNULL(p.state, '')
SET j.property_id = p.property_id
WHERE j.address IS NOT NULL AND j.address != '';

-- -----------------------------------------------------
-- Stored procedure to get all jobs at a property
-- -----------------------------------------------------
DELIMITER $$
CREATE PROCEDURE `GetPropertyJobs`(IN p_property_id INT)
BEGIN
    SELECT 
        j.job_id,
        j.job_number,
        j.job_name,
        j.status,
        j.create_date,
        j.completion_date,
        j.total_estimate,
        j.total_actual,
        j.notes
    FROM Jobs j
    WHERE j.property_id = p_property_id
    ORDER BY j.create_date DESC;
END$$
DELIMITER ;

-- -----------------------------------------------------
-- Stored procedure to create a new job at existing property
-- -----------------------------------------------------
DELIMITER $$
CREATE PROCEDURE `CreateJobAtProperty`(
    IN p_property_id INT,
    IN p_job_number VARCHAR(20),
    IN p_job_name VARCHAR(100),
    IN p_status VARCHAR(20),
    IN p_create_date DATE,
    IN p_total_estimate DECIMAL(10,2),
    IN p_notes TEXT
)
BEGIN
    DECLARE v_customer_id INT;
    DECLARE v_address VARCHAR(255);
    DECLARE v_city VARCHAR(50);
    DECLARE v_state VARCHAR(2);
    DECLARE v_zip VARCHAR(10);
    DECLARE v_square_footage INT;
    DECLARE v_num_floors INT;
    
    -- Get property details
    SELECT customer_id, address, city, state, zip, square_footage, num_floors
    INTO v_customer_id, v_address, v_city, v_state, v_zip, v_square_footage, v_num_floors
    FROM Properties
    WHERE property_id = p_property_id;
    
    -- Insert new job
    INSERT INTO Jobs (
        job_number, 
        customer_id, 
        property_id,
        job_name, 
        address, 
        city, 
        state, 
        zip,
        square_footage,
        num_floors,
        status, 
        create_date, 
        total_estimate, 
        notes
    ) VALUES (
        p_job_number,
        v_customer_id,
        p_property_id,
        p_job_name,
        v_address,
        v_city,
        v_state,
        v_zip,
        v_square_footage,
        v_num_floors,
        p_status,
        p_create_date,
        p_total_estimate,
        p_notes
    );
    
    SELECT LAST_INSERT_ID() AS job_id;
END$$
DELIMITER ;

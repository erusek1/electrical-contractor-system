# migrate_pricelist_to_materials.py
# This script migrates items from the PriceList table to the Materials table
# so they can be used in Material Price Tracking

import mysql.connector
from datetime import datetime

# Database configuration
DB_CONFIG = {
    'host': 'localhost',
    'user': 'your_username',  # Update with your MySQL username
    'password': 'your_password',  # Update with your MySQL password
    'database': 'electrical_contractor_db'
}

def migrate_pricelist_to_materials():
    """
    Migrate items from PriceList table to Materials table
    """
    connection = None
    cursor = None
    
    try:
        # Connect to database
        connection = mysql.connector.connect(**DB_CONFIG)
        cursor = connection.cursor(dictionary=True)
        
        print("Connected to database successfully")
        
        # First, check if Materials table exists
        cursor.execute("""
            SELECT COUNT(*) as table_exists 
            FROM information_schema.tables 
            WHERE table_schema = %s 
            AND table_name = 'Materials'
        """, (DB_CONFIG['database'],))
        
        result = cursor.fetchone()
        
        if result['table_exists'] == 0:
            print("Materials table does not exist. Creating it now...")
            
            # Create Materials table
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS `Materials` (
                    `material_id` INT NOT NULL AUTO_INCREMENT,
                    `material_code` VARCHAR(20) NOT NULL,
                    `name` VARCHAR(100) NOT NULL,
                    `description` TEXT NULL,
                    `category` VARCHAR(50) NOT NULL,
                    `unit_of_measure` VARCHAR(20) NOT NULL DEFAULT 'each',
                    `current_price` DECIMAL(10,2) NOT NULL,
                    `tax_rate` DECIMAL(5,3) NULL DEFAULT 0,
                    `min_stock_level` INT NOT NULL DEFAULT 0,
                    `max_stock_level` INT NOT NULL DEFAULT 0,
                    `preferred_vendor_id` INT NULL,
                    `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
                    `created_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    `updated_date` DATETIME NULL,
                    PRIMARY KEY (`material_id`),
                    UNIQUE INDEX `material_code_UNIQUE` (`material_code` ASC),
                    INDEX `idx_category` (`category`),
                    CONSTRAINT `fk_materials_vendor`
                        FOREIGN KEY (`preferred_vendor_id`)
                        REFERENCES `Vendors` (`vendor_id`)
                        ON DELETE SET NULL
                        ON UPDATE NO ACTION
                );
            """)
            connection.commit()
            print("Materials table created successfully")
        
        # Get all items from PriceList
        cursor.execute("""
            SELECT * FROM PriceList 
            WHERE is_active = TRUE
            ORDER BY category, item_code
        """)
        
        price_list_items = cursor.fetchall()
        
        if not price_list_items:
            print("No active items found in PriceList table")
            return
        
        print(f"Found {len(price_list_items)} items in PriceList")
        
        # Check for existing materials to avoid duplicates
        cursor.execute("SELECT material_code FROM Materials")
        existing_codes = {row['material_code'] for row in cursor.fetchall()}
        
        # Prepare data for insertion
        materials_to_insert = []
        skipped_count = 0
        
        for item in price_list_items:
            # Skip if material code already exists
            if item['item_code'] in existing_codes:
                print(f"Skipping {item['item_code']} - {item['name']} (already exists)")
                skipped_count += 1
                continue
            
            # Prepare material data
            material_data = (
                item['item_code'],  # material_code
                item['name'],  # name
                item['description'],  # description
                item['category'],  # category
                'each',  # unit_of_measure (default)
                item['base_cost'],  # current_price
                item['tax_rate'] if item['tax_rate'] else 0.064,  # tax_rate (default to 6.4% if null)
                0,  # min_stock_level
                0,  # max_stock_level
                None,  # preferred_vendor_id
                True,  # is_active
                datetime.now()  # created_date
            )
            
            materials_to_insert.append(material_data)
        
        if materials_to_insert:
            # Insert materials in batches
            insert_query = """
                INSERT INTO Materials 
                (material_code, name, description, category, unit_of_measure, 
                 current_price, tax_rate, min_stock_level, max_stock_level, 
                 preferred_vendor_id, is_active, created_date)
                VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
            """
            
            cursor.executemany(insert_query, materials_to_insert)
            connection.commit()
            
            print(f"\nSuccessfully migrated {len(materials_to_insert)} items to Materials table")
        
        if skipped_count > 0:
            print(f"Skipped {skipped_count} items that already existed")
        
        # Create MaterialPriceHistory entries for the initial prices
        print("\nCreating initial price history entries...")
        
        cursor.execute("""
            INSERT INTO MaterialPriceHistory 
            (material_id, price, effective_date, notes, created_by, created_date)
            SELECT 
                m.material_id,
                m.current_price,
                m.created_date,
                'Initial price from PriceList migration',
                'Migration Script',
                NOW()
            FROM Materials m
            LEFT JOIN MaterialPriceHistory mph ON m.material_id = mph.material_id
            WHERE mph.material_id IS NULL
        """)
        
        history_count = cursor.rowcount
        connection.commit()
        
        print(f"Created {history_count} initial price history entries")
        
        # Show summary
        cursor.execute("SELECT COUNT(*) as total FROM Materials")
        total_materials = cursor.fetchone()['total']
        
        cursor.execute("""
            SELECT category, COUNT(*) as count 
            FROM Materials 
            GROUP BY category 
            ORDER BY category
        """)
        
        print(f"\nMigration complete! Total materials in database: {total_materials}")
        print("\nMaterials by category:")
        
        for row in cursor.fetchall():
            print(f"  {row['category']}: {row['count']} items")
        
    except mysql.connector.Error as e:
        print(f"Database error: {e}")
        if connection:
            connection.rollback()
    except Exception as e:
        print(f"Error: {e}")
        if connection:
            connection.rollback()
    finally:
        if cursor:
            cursor.close()
        if connection:
            connection.close()

def create_material_price_history_table():
    """
    Create MaterialPriceHistory table if it doesn't exist
    """
    connection = None
    cursor = None
    
    try:
        connection = mysql.connector.connect(**DB_CONFIG)
        cursor = connection.cursor()
        
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS `MaterialPriceHistory` (
                `price_history_id` INT NOT NULL AUTO_INCREMENT,
                `material_id` INT NOT NULL,
                `price` DECIMAL(10,2) NOT NULL,
                `effective_date` DATETIME NOT NULL,
                `vendor_id` INT NULL,
                `purchase_order_number` VARCHAR(50) NULL,
                `quantity_purchased` DECIMAL(10,2) NULL,
                `notes` TEXT NULL,
                `created_by` VARCHAR(50) NOT NULL,
                `created_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (`price_history_id`),
                INDEX `idx_material_date` (`material_id`, `effective_date` DESC),
                INDEX `idx_vendor` (`vendor_id`),
                CONSTRAINT `fk_price_history_material`
                    FOREIGN KEY (`material_id`)
                    REFERENCES `Materials` (`material_id`)
                    ON DELETE CASCADE
                    ON UPDATE NO ACTION,
                CONSTRAINT `fk_price_history_vendor`
                    FOREIGN KEY (`vendor_id`)
                    REFERENCES `Vendors` (`vendor_id`)
                    ON DELETE SET NULL
                    ON UPDATE NO ACTION
            );
        """)
        
        connection.commit()
        print("MaterialPriceHistory table created successfully")
        
    except mysql.connector.Error as e:
        print(f"Database error creating MaterialPriceHistory table: {e}")
    finally:
        if cursor:
            cursor.close()
        if connection:
            connection.close()

if __name__ == "__main__":
    print("PriceList to Materials Migration Script")
    print("======================================")
    print("\nThis script will migrate items from the PriceList table to the Materials table")
    print("for use in Material Price Tracking.")
    print("\nNote: Items with duplicate item codes will be skipped.")
    
    response = input("\nDo you want to continue? (yes/no): ")
    
    if response.lower() in ['yes', 'y']:
        # First ensure MaterialPriceHistory table exists
        create_material_price_history_table()
        
        # Then run migration
        migrate_pricelist_to_materials()
    else:
        print("Migration cancelled.")

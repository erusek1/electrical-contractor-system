#!/usr/bin/env python3
# price_list_migration.py
# Script to import price list from template 3.xlsx to the MySQL database

import pandas as pd
import mysql.connector
import os
import sys

def main():
    print("Electrical Contractor System - Price List Migration")
    print("=================================================")
    
    # Configuration - Update these settings
    db_config = {
        "host": "localhost",
        "user": "your_db_username",
        "password": "your_db_password",
        "database": "electrical_contractor_db"
    }
    
    excel_file = "template 3.xlsx"
    sheet_name = "Price List"  # Adjust if your sheet name is different
    
    # Verify file exists
    if not os.path.exists(excel_file):
        print(f"Error: File {excel_file} not found.")
        print(f"Please place {excel_file} in the same directory as this script.")
        sys.exit(1)
    
    try:
        # Connect to the database
        print(f"Connecting to MySQL database...")
        conn = mysql.connector.connect(**db_config)
        cursor = conn.cursor()
        
        # Read the Excel file
        print(f"Reading {excel_file}, sheet '{sheet_name}'...")
        try:
            price_df = pd.read_excel(excel_file, sheet_name=sheet_name)
            print(f"Found {len(price_df)} records.")
        except Exception as e:
            print(f"Error reading Excel sheet: {e}")
            print("Make sure the sheet name is correct and contains price data.")
            sys.exit(1)
        
        # Initialize counters
        items_added = 0
        errors = 0
        
        # Process each row
        for index, row in price_df.iterrows():
            try:
                # Skip header rows or empty rows
                if not 'Name' in row or pd.isna(row['Name']):
                    continue
                
                name = str(row['Name']).strip()
                
                # Skip rows without a name
                if not name:
                    continue
                
                # Create item code
                item_code = None
                if 'x' in row and pd.notna(row['x']) and row['x']:
                    item_code = str(row['x']).strip()
                else:
                    # Use first 3 letters of name + index as code if no code provided
                    item_code = name[:3].upper() + str(index)
                
                # Extract category, default to "General" if not found
                category = "General"
                if 'A' in row and pd.notna(row['A']):
                    category = str(row['A']).strip()
                
                # Extract description
                description = None
                if 'D' in row and pd.notna(row['D']):
                    description = str(row['D']).strip()
                
                # Extract base cost
                base_cost = 0
                if 'E' in row and pd.notna(row['E']):
                    try:
                        base_cost = float(row['E'])
                    except (ValueError, TypeError):
                        print(f"Warning: Invalid cost for item '{name}', using 0")
                
                # Set default tax rate (New Jersey)
                tax_rate = 0.066  # 6.6% NJ sales tax
                
                # Extract labor minutes if available (adjust column name as needed)
                labor_minutes = 0
                if 'Labor_Minutes' in row and pd.notna(row['Labor_Minutes']):
                    try:
                        labor_minutes = int(row['Labor_Minutes'])
                    except (ValueError, TypeError):
                        print(f"Warning: Invalid labor minutes for item '{name}', using 0")
                
                # Default markup percentage
                markup_percentage = 15.0  # 15% markup
                if 'Markup' in row and pd.notna(row['Markup']):
                    try:
                        markup_percentage = float(row['Markup'])
                    except (ValueError, TypeError):
                        print(f"Warning: Invalid markup for item '{name}', using 15%")
                
                # Check if item code already exists
                cursor.execute("SELECT item_id FROM PriceList WHERE item_code = %s", (item_code,))
                existing_item = cursor.fetchone()
                
                if existing_item:
                    print(f"Item code '{item_code}' already exists, updating.")
                    
                    # Update existing item
                    cursor.execute(
                        """UPDATE PriceList SET 
                           category = %s, name = %s, description = %s, 
                           base_cost = %s, tax_rate = %s, labor_minutes = %s, 
                           markup_percentage = %s
                           WHERE item_code = %s""",
                        (
                            category, name, description,
                            base_cost, tax_rate, labor_minutes,
                            markup_percentage, item_code
                        )
                    )
                else:
                    # Insert new item
                    cursor.execute(
                        """INSERT INTO PriceList 
                           (category, item_code, name, description, 
                            base_cost, tax_rate, labor_minutes, markup_percentage) 
                           VALUES (%s, %s, %s, %s, %s, %s, %s, %s)""",
                        (
                            category, item_code, name, description,
                            base_cost, tax_rate, labor_minutes, markup_percentage
                        )
                    )
                    items_added += 1
                
                conn.commit()
                print(f"Processed: {item_code} - {name}")
                
            except Exception as e:
                print(f"Error processing row {index}: {e}")
                errors += 1
        
        print("\nMigration Summary:")
        print(f"Price list items added: {items_added}")
        print(f"Errors encountered: {errors}")
        print("Migration completed!")
        
    except mysql.connector.Error as err:
        print(f"Database error: {err}")
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)
    finally:
        if 'conn' in locals() and conn.is_connected():
            cursor.close()
            conn.close()
            print("Database connection closed.")

if __name__ == "__main__":
    main()

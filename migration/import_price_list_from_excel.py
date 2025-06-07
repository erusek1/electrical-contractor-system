#!/usr/bin/env python3
"""
Import Price List from Excel Template to MySQL Database

This script reads your template3.xlsx Price List sheet and imports
all items into the database with their labor minutes and stages.
"""

import pandas as pd
import mysql.connector
from mysql.connector import Error
import sys
import os

def connect_to_database():
    """Create database connection"""
    try:
        connection = mysql.connector.connect(
            host='localhost',
            database='electrical_estimating_db',
            user='your_username',  # Update with your MySQL username
            password='your_password'  # Update with your MySQL password
        )
        if connection.is_connected():
            print("Successfully connected to database")
            return connection
    except Error as e:
        print(f"Error connecting to MySQL: {e}")
        sys.exit(1)

def import_price_list(excel_file):
    """Import price list from Excel to database"""
    
    # Connect to database
    connection = connect_to_database()
    cursor = connection.cursor()
    
    try:
        # Read the Price List sheet
        print(f"Reading {excel_file}...")
        df = pd.read_excel(excel_file, sheet_name='Price List')
        
        # Clean column names
        df.columns = df.columns.str.strip()
        
        items_imported = 0
        
        for index, row in df.iterrows():
            # Skip rows without item codes
            if pd.isna(row.get('x')) or str(row['x']).strip() == '':
                continue
                
            item_code = str(row['x']).strip()
            name = str(row.get('Name', '')).strip()
            
            # Skip if no name
            if not name:
                continue
                
            # Get base price from column E
            base_price = float(row.get('E', 0)) if pd.notna(row.get('E')) else 0
            
            # Determine category based on item type
            category = 'General'  # Default
            if 'kitchen' in name.lower() or item_code in ['fridge', 'micro', 'dw', 'hood', 'oven', 'cook']:
                category = 'Kitchen'
            elif 'light' in name.lower() or item_code in ['hh', 'pend', 'Sc', 'Van']:
                category = 'Lighting'
            elif 'wire' in name.lower() or 'yellow' in item_code or 'white' in item_code:
                category = 'Wire'
            elif item_code in ['ARL'] or 'outdoor' in name.lower():
                category = 'Exterior'
            elif 'fan' in name.lower() or item_code in ['Ex-l']:
                category = 'Ventilation'
                
            # Insert the item
            insert_query = """
                INSERT INTO PriceListItems (item_code, name, base_price, category)
                VALUES (%s, %s, %s, %s)
                ON DUPLICATE KEY UPDATE
                    name = VALUES(name),
                    base_price = VALUES(base_price),
                    category = VALUES(category)
            """
            
            cursor.execute(insert_query, (item_code, name, base_price, category))
            item_id = cursor.lastrowid
            
            # If item already existed, get its ID
            if item_id == 0:
                cursor.execute("SELECT item_id FROM PriceListItems WHERE item_code = %s", (item_code,))
                item_id = cursor.fetchone()[0]
            
            # Insert labor minutes if columns exist
            # Rough labor (column W)
            if 'W' in df.columns and pd.notna(row.get('W')) and row['W'] > 0:
                labor_query = """
                    INSERT INTO LaborMinutes (item_id, stage, minutes)
                    VALUES (%s, 'Rough', %s)
                    ON DUPLICATE KEY UPDATE minutes = VALUES(minutes)
                """
                cursor.execute(labor_query, (item_id, int(row['W'])))
            
            # Finish labor (column X)
            if 'X' in df.columns and pd.notna(row.get('X')) and row['X'] > 0:
                labor_query = """
                    INSERT INTO LaborMinutes (item_id, stage, minutes)
                    VALUES (%s, 'Finish', %s)
                    ON DUPLICATE KEY UPDATE minutes = VALUES(minutes)
                """
                cursor.execute(labor_query, (item_id, int(row['X'])))
            
            # Service labor (column S)
            if 'S' in df.columns and pd.notna(row.get('S')) and row['S'] > 0:
                labor_query = """
                    INSERT INTO LaborMinutes (item_id, stage, minutes)
                    VALUES (%s, 'Service', %s)
                    ON DUPLICATE KEY UPDATE minutes = VALUES(minutes)
                """
                cursor.execute(labor_query, (item_id, int(row['S'])))
            
            # Extra labor (column Y)  
            if 'Y' in df.columns and pd.notna(row.get('Y')) and row['Y'] > 0:
                labor_query = """
                    INSERT INTO LaborMinutes (item_id, stage, minutes)
                    VALUES (%s, 'Extra', %s)
                    ON DUPLICATE KEY UPDATE minutes = VALUES(minutes)
                """
                cursor.execute(labor_query, (item_id, int(row['Y'])))
            
            items_imported += 1
            print(f"Imported: {item_code} - {name}")
        
        # Commit the transaction
        connection.commit()
        print(f"\nSuccessfully imported {items_imported} items")
        
    except Error as e:
        print(f"Error importing data: {e}")
        connection.rollback()
    except Exception as e:
        print(f"Unexpected error: {e}")
        connection.rollback()
    finally:
        if connection.is_connected():
            cursor.close()
            connection.close()
            print("Database connection closed")

if __name__ == "__main__":
    # Check if file path provided
    if len(sys.argv) != 2:
        print("Usage: python import_price_list_from_excel.py <path_to_template3.xlsx>")
        sys.exit(1)
    
    excel_file = sys.argv[1]
    
    # Check if file exists
    if not os.path.exists(excel_file):
        print(f"Error: File '{excel_file}' not found")
        sys.exit(1)
    
    import_price_list(excel_file)
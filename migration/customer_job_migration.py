#!/usr/bin/env python3
# customer_job_migration.py
# Script to import customers and jobs from Jobs List.xlsx to the MySQL database

import pandas as pd
import mysql.connector
from datetime import datetime
import os
import sys

def main():
    print("Electrical Contractor System - Customer and Job Migration")
    print("========================================================")
    
    # Configuration - Update these settings
    db_config = {
        "host": "localhost",
        "user": "root",
        "password": "215Osborn",
        "database": "electrical_contractor_db"
    }
    
    excel_file = "Jobs List.xlsx"
    
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
        print(f"Reading {excel_file}...")
        jobs_df = pd.read_excel(excel_file)
        
        print(f"Found {len(jobs_df)} job records.")
        
        # Process each job/customer
        customers_added = 0
        jobs_added = 0
        
        for index, row in jobs_df.iterrows():
            # Extract basic info, handling potential NaN values
            job_number = str(row['Job #']) if 'Job #' in row and pd.notna(row['Job #']) else None
            customer_name = str(row['Customer']) if 'Customer' in row and pd.notna(row['Customer']) else None
            
            if not job_number or not customer_name:
                print(f"Skipping row {index}: Missing job number or customer name")
                continue
                
            # Process address - format typically "123 Main St, City, State Zip"
            address = str(row['Address']) if 'Address' in row and pd.notna(row['Address']) else ""
            address_parts = address.split(',')
            
            street = address_parts[0].strip() if len(address_parts) > 0 else ''
            city = address_parts[1].strip() if len(address_parts) > 1 else ''
            
            # Handle state and zip (assuming format like "NJ 07733")
            state_zip = address_parts[2].strip() if len(address_parts) > 2 else ''
            state = 'NJ'  # Default to NJ
            zip_code = ''
            
            if state_zip:
                parts = state_zip.split()
                if len(parts) >= 1:
                    state = parts[0]
                if len(parts) >= 2:
                    zip_code = parts[1]
            
            # Check if customer already exists
            cursor.execute("SELECT customer_id FROM Customers WHERE name = %s", (customer_name,))
            result = cursor.fetchone()
            
            customer_id = None
            if result:
                customer_id = result[0]
                print(f"Found existing customer: {customer_name} (ID: {customer_id})")
            else:
                # Insert new customer
                cursor.execute(
                    "INSERT INTO Customers (name, address, city, state, zip) VALUES (%s, %s, %s, %s, %s)",
                    (customer_name, street, city, state, zip_code)
                )
                conn.commit()
                customer_id = cursor.lastrowid
                customers_added += 1
                print(f"Added new customer: {customer_name} (ID: {customer_id})")
            
            # Check if job already exists
            cursor.execute("SELECT job_id FROM Jobs WHERE job_number = %s", (job_number,))
            if cursor.fetchone():
                print(f"Job {job_number} already exists, skipping.")
                continue
            
            # Determine job status (assuming all existing jobs are complete)
            status = 'Complete'
                
            # Create date (default to current date if not available)
            create_date = row['Date'] if 'Date' in row and pd.notna(row['Date']) else datetime.now().date()
            
            # Insert job
            cursor.execute(
                """INSERT INTO Jobs 
                   (job_number, customer_id, job_name, address, city, state, zip, 
                    status, create_date) 
                   VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s)""",
                (
                    job_number, 
                    customer_id,
                    customer_name,  # Using customer name as job name
                    street,
                    city,
                    state,
                    zip_code,
                    status,
                    create_date
                )
            )
            conn.commit()
            jobs_added += 1
            print(f"Added job: {job_number} - {customer_name}")
        
        print("\nMigration Summary:")
        print(f"Customers added: {customers_added}")
        print(f"Jobs added: {jobs_added}")
        print("Migration completed successfully!")
        
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

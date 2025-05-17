# Electrical Contractor System

A database-driven system for managing electrical contracting business operations. This system replaces an Excel-based solution with a comprehensive database system for tracking jobs, labor, materials, and costs.

## Project Overview

This electrical contracting business management system helps track:

- Jobs with sequential numbering and customer information
- Labor hours by employee, job, and project stage
- Material costs with vendor and invoice tracking
- Estimated vs. actual cost comparisons
- Room-by-room electrical specifications
- Permit items for inspections

## Repository Structure

- `/database` - Database schema files
- `/docs` - Project documentation
- `/frontend` - User interface components
- `/migration` - Data migration scripts and utilities

## Getting Started

See [Database Implementation Plan](docs/Database_Implementation_Plan.md) for detailed setup instructions.

## Key Features

1. **Job Management** - Create, track, and manage electrical jobs
2. **Weekly Labor Entry** - Track employee hours with validation
3. **Job Cost Tracking** - Compare estimated vs. actual costs
4. **Material Tracking** - Track materials by vendor, job, and stage
5. **Room Specifications** - Manage electrical items by room

## Technology Stack

- **Database**: MySQL
- **Frontend Options**:
  - Microsoft Access (easiest)
  - Web-based PHP application (flexible)
  - C# Windows Forms application (powerful)

## Implementation Timeline

- **Database Setup**: 1-2 days
- **User Interface Development**: 2-4 weeks
- **Data Migration**: 1-2 weeks
- **Testing and Training**: 1-2 weeks
- **Total Implementation**: 6-10 weeks

## Documentation

- [Project Breakdown](docs/Breakdown.md) - Complete system overview
- [Database Implementation Plan](docs/Database_Implementation_Plan.md) - Step-by-step implementation guide

## Future Enhancements

- Mobile-friendly interface
- Accounting system integration
- Customer portal

## Migration from Excel

Data will be migrated from several Excel files:

- ERE.xlsx (labor and material tracking)
- Jobs List.xlsx (master job reference)
- Individual job sheets (e.g., 619.xlsx)
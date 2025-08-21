# Asset Management System

A comprehensive ASP.NET Core 9.0 MVC application for managing IT and Facilities equipment with interactive floor plans, role-based access, request/approval workflows, and procurement-to-deployment lifecycle.

## Features

- **Asset Management**: Complete CRUD operations for IT and facilities equipment
- **Excel Import**: Bulk import with preview functionality and validation
- **Role-Based Access**: Admin, IT, Facilities, Procurement, Storage, Manager, User roles
- **Audit Logging**: Complete history tracking for all asset changes
- **Bootstrap 5 UI**: Modern, responsive interface
- **SQL Server Integration**: Entity Framework Core with code-first approach
- **Session Management**: Secure file handling for imports

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- SQL Server (Local or Remote)
- Git (for deployment)

### 1. Database Setup

#### Option A: SQL Authentication
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.8.229;Database=AssetManagement;User ID=sa;Password=MSPress#1;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

#### Option B: Integrated Security
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AssetManagement;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

#### Option C: Environment Variable
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=...;User ID=...;Password=...;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

### 2. Database Migration

```bash
# Navigate to Infrastructure project
cd AssetManagement.Infrastructure

# Create initial migration
dotnet ef migrations add InitialCreate --startup-project ../AssetManagement.Web

# Update database
dotnet ef database update --startup-project ../AssetManagement.Web
```

### 3. Run the Application

```bash
# Navigate to Web project
cd AssetManagement.Web

# Run the application
dotnet run
```

### 4. Default Login

- **Email**: admin@assetmanagement.com
- **Password**: Admin123!

## Deployment to GitHub

### Using the Batch Script

1. Run the provided batch script:
   ```bash
   deploy-to-github.bat
   ```

2. The script will:
   - Initialize git repository (if not exists)
   - Add all files to git
   - Commit with descriptive message
   - Push to https://github.com/ZetallicA/AssetAugust

### Manual Deployment

```bash
# Initialize git
git init

# Add files
git add .

# Commit
git commit -m "Asset Management System - Initial commit"

# Add remote
git remote add origin https://github.com/ZetallicA/AssetAugust.git

# Push
git branch -M main
git push -u origin main
```

## Excel Import

### Template Download

1. Navigate to **Assets > Import**
2. Click **"Download Template"**
3. Fill in the 41-column structure:
   - Asset Tag (Required)
   - Serial Number
   - Service Tag
   - Manufacturer
   - Model
   - Category
   - Net Name
   - Assigned User Name
   - Assigned User Email
   - Manager
   - Department
   - Unit
   - Location
   - Floor
   - Desk
   - Status
   - IP Address
   - MAC Address
   - Wall Port
   - Switch Name
   - Switch Port
   - Phone Number
   - Extension
   - IMEI
   - Card Number
   - OS Version
   - License1-5
   - Purchase Price
   - Order Number
   - Vendor
   - Vendor Invoice
   - Purchase Date
   - Warranty Start
   - Warranty End Date
   - Notes
   - Created At
   - Created By

### Import Process

1. **Upload File**: Select your Excel file (.xlsx or .xls)
2. **Preview Data**: Check "Preview data before import" to review first 10 rows
3. **Review**: Check for duplicates and validation errors
4. **Confirm Import**: Click "Confirm Import" to process all records

## Admin Bulk Operations

### Bulk Delete Selected Assets

1. **Select Assets**: Use checkboxes to select individual assets
2. **Select All**: Click "Select All" to select all visible assets
3. **Delete Selected**: Click "Delete Selected" to remove chosen assets

### Delete All Imported Assets

1. **Quick Clean**: Click "Delete All Imported Assets" to remove all imported records
2. **Confirmation**: Confirm the action (cannot be undone)

## Database Management

### Delete Imported Records (SQL)

Use the provided `delete-imported-assets.sql` script:

```sql
-- Option 1: Delete ALL imported assets
DELETE FROM AssetHistory 
WHERE AssetId IN (
    SELECT Id FROM Assets 
    WHERE IsActive = 1 
        AND CreatedBy != 'System'
);

DELETE FROM Assets 
WHERE IsActive = 1 
    AND CreatedBy != 'System';

-- Option 2: Delete by specific user
DELETE FROM Assets 
WHERE IsActive = 1 
    AND CreatedBy = 'admin@assetmanagement.com';

-- Option 3: Soft delete (recommended)
UPDATE Assets 
SET IsActive = 0,
    UpdatedAt = GETUTCDATE(),
    UpdatedBy = 'System'
WHERE IsActive = 1 
    AND CreatedBy != 'System';
```

## Project Structure

```
AssetManagement/
├── AssetManagement.Domain/          # Domain entities
│   └── Entities/
│       ├── Asset.cs
│       ├── ApplicationUser.cs
│       ├── Building.cs
│       ├── Floor.cs
│       └── AssetHistory.cs
├── AssetManagement.Infrastructure/  # Data access & services
│   ├── Data/
│   │   ├── AssetManagementDbContext.cs
│   │   └── DatabaseSeeder.cs
│   └── Services/
│       ├── ExcelImportService.cs
│       └── BackgroundJobService.cs
└── AssetManagement.Web/             # Web application
    ├── Controllers/
    │   ├── AssetsController.cs
    │   └── DashboardController.cs
    ├── Views/
    │   └── Assets/
    │       ├── Index.cshtml
    │       ├── Import.cshtml
    │       └── ImportPreview.cshtml
    └── Program.cs
```

## Health Checks

- **Database**: `/health` - Verifies SQL Server connectivity
- **Application**: Built-in ASP.NET Core health monitoring

## Security

- **Authentication**: ASP.NET Core Identity
- **Authorization**: Role-based access control
- **Audit Logging**: Complete change tracking
- **Session Management**: Secure file handling

## Troubleshooting

### Common Issues

1. **Database Connection**: Verify connection string and SQL Server access
2. **File Locking**: Ensure no other processes are using Excel files
3. **Import Errors**: Check Excel file format and column structure
4. **Authentication**: Verify user roles and permissions

### Logs

- **Application Logs**: Check console output and Serilog files
- **Database Logs**: SQL Server error logs
- **Import Logs**: Detailed import progress and error tracking

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review application logs
3. Verify database connectivity
4. Test with sample data

## License

This project is for internal use. Please ensure compliance with your organization's policies.

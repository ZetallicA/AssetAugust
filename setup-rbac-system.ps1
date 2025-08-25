# Asset Management RBAC System Setup Script
# This script sets up the complete role-based access control system

Write-Host "Setting up Asset Management RBAC System..." -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green

# Database connection parameters
$server = "192.168.8.229"
$database = "AssetManagement"
$username = "sa"
$password = "MSPress#1"

# SQL script file
$sqlFile = "setup-rbac-system.sql"

# Check if SQL script exists
if (-not (Test-Path $sqlFile)) {
    Write-Host "Error: SQL script file '$sqlFile' not found!" -ForegroundColor Red
    exit 1
}

Write-Host "Executing RBAC setup script..." -ForegroundColor Yellow

try {
    # Execute the SQL script
    $result = sqlcmd -S $server -U $username -P $password -d $database -i $sqlFile -b
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "RBAC System setup completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "What was created:" -ForegroundColor Cyan
        Write-Host "- 7 Standard Roles: Admin, ITAdmin, FacilitiesAdmin, UnitManager, AssetManager, ReportViewer, User" -ForegroundColor White
        Write-Host "- 42 Granular Permissions across all system areas" -ForegroundColor White
        Write-Host "- 5 Standard Groups: IT_DEPARTMENT, FACILITIES_DEPARTMENT, MANAGEMENT_TEAM, ASSET_TEAM, REPORTING_TEAM" -ForegroundColor White
        Write-Host "- Role-Permission mappings for each role" -ForegroundColor White
        Write-Host "- Default 'User' role assigned to all existing users" -ForegroundColor White
        Write-Host ""
        Write-Host "Next Steps:" -ForegroundColor Cyan
        Write-Host "1. Access the User Management interface at: https://assets.oathone.com/Admin/UserManagement" -ForegroundColor White
        Write-Host "2. Assign appropriate roles to your 23 users" -ForegroundColor White
        Write-Host "3. Use bulk role assignment for efficiency" -ForegroundColor White
        Write-Host "4. Create custom permission assignments for special cases" -ForegroundColor White
        Write-Host ""
        Write-Host "Recommended role assignments for your users:" -ForegroundColor Cyan
        Write-Host "- IT Team: ITAdmin role" -ForegroundColor White
        Write-Host "- Facilities Team: FacilitiesAdmin role" -ForegroundColor White
        Write-Host "- Managers: UnitManager role" -ForegroundColor White
        Write-Host "- Asset Team: AssetManager role" -ForegroundColor White
        Write-Host "- Report Users: ReportViewer role" -ForegroundColor White
        Write-Host "- General Users: User role (already assigned)" -ForegroundColor White
    } else {
        Write-Host "Error: Failed to execute RBAC setup script!" -ForegroundColor Red
        Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Setup complete! You can now manage user roles and permissions through the web interface." -ForegroundColor Green

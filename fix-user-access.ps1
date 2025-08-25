# Fix User Access Issues Script
# This script helps diagnose and fix user access problems

Write-Host "Asset Management - User Access Diagnostics" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

# Database connection parameters
$server = "192.168.8.229"
$database = "AssetManagement"
$username = "sa"
$password = "MSPress#1"

Write-Host "Checking user roles and permissions..." -ForegroundColor Yellow

# Check current user roles
$userQuery = @"
SELECT 
    u.Email,
    u.UserName,
    u.FirstName,
    u.LastName,
    STRING_AGG(r.Name, ', ') as Roles
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'rabi@oathone.com'
GROUP BY u.Email, u.UserName, u.FirstName, u.LastName
"@

$userResult = sqlcmd -S $server -U $username -P $password -d $database -Q $userQuery -h -1

Write-Host "Current user information:" -ForegroundColor Cyan
Write-Host $userResult -ForegroundColor White

# Check if Admin role exists
$adminRoleQuery = "SELECT Name FROM AspNetRoles WHERE Name = 'Admin'"
$adminRoleResult = sqlcmd -S $server -U $username -P $password -d $database -Q $adminRoleQuery -h -1

if ($adminRoleResult -match "Admin") {
    Write-Host "Admin role exists ✓" -ForegroundColor Green
} else {
    Write-Host "Admin role does not exist! Creating it..." -ForegroundColor Red
    
    $createAdminRole = "INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES ('admin-role-id', 'Admin', 'ADMIN', NEWID())"
    
    sqlcmd -S $server -U $username -P $password -d $database -Q $createAdminRole
    Write-Host "Admin role created ✓" -ForegroundColor Green
}

# Ensure user has Admin role
$checkAdminAssignment = @"
SELECT COUNT(*) as HasAdminRole
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'rabi@oathone.com' AND r.Name = 'Admin'
"@

$adminAssignmentResult = sqlcmd -S $server -U $username -P $password -d $database -Q $checkAdminAssignment -h -1

if ($adminAssignmentResult -match "0") {
    Write-Host "User does not have Admin role! Assigning it..." -ForegroundColor Yellow
    
    $assignAdminRole = "INSERT INTO AspNetUserRoles (UserId, RoleId) SELECT u.Id, r.Id FROM AspNetUsers u, AspNetRoles r WHERE u.Email = 'rabi@oathone.com' AND r.Name = 'Admin'"
    
    sqlcmd -S $server -U $username -P $password -d $database -Q $assignAdminRole
    Write-Host "Admin role assigned to user ✓" -ForegroundColor Green
} else {
    Write-Host "User already has Admin role ✓" -ForegroundColor Green
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Sign out of the application completely" -ForegroundColor White
Write-Host "2. Sign back in to refresh your session" -ForegroundColor White
Write-Host "3. Try accessing: https://assets.oathone.com/Admin/UserManagement" -ForegroundColor White
Write-Host "4. If still having issues, try: https://assets.oathone.com/Admin/TestAccess" -ForegroundColor White

Write-Host ""
Write-Host "Diagnostics complete!" -ForegroundColor Green

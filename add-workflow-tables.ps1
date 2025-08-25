# PowerShell script to add workflow tables to AssetManagement database
# This script executes the SQL script to create the workflow tables

$connectionString = "Server=192.168.8.229;Database=AssetManagement;User ID=sa;Password=MSPress#1;TrustServerCertificate=True;MultipleActiveResultSets=True"
$sqlScript = Get-Content "add-workflow-tables.sql" -Raw

try {
    Write-Host "Connecting to database..." -ForegroundColor Yellow
    
    # Create SQL connection
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()
    
    Write-Host "Connected successfully!" -ForegroundColor Green
    
    # Create command
    $command = New-Object System.Data.SqlClient.SqlCommand($sqlScript, $connection)
    $command.CommandTimeout = 300
    
    Write-Host "Executing SQL script..." -ForegroundColor Yellow
    $result = $command.ExecuteNonQuery()
    
    Write-Host "SQL script executed successfully!" -ForegroundColor Green
    Write-Host "Workflow tables have been created." -ForegroundColor Green
    
} catch {
    Write-Host "Error executing SQL script: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.Exception.StackTrace)" -ForegroundColor Red
} finally {
    if ($connection -and $connection.State -eq 'Open') {
        $connection.Close()
        Write-Host "Database connection closed." -ForegroundColor Yellow
    }
}

Write-Host "Script completed." -ForegroundColor Green

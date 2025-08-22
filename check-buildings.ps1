# Check if 100 Church Street building exists
$connectionString = "Server=(localdb)\MSSQLLocalDB;Database=AssetManagement;Trusted_Connection=true;"
$query = "SELECT Name, BuildingCode, Phone, Fax FROM Buildings WHERE BuildingCode = '100CHURCH'"

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    $command = New-Object System.Data.SqlClient.SqlCommand($query, $connection)
    $reader = $command.ExecuteReader()
    
    if ($reader.Read()) {
        Write-Host "✅ 100 Church Street building found:" -ForegroundColor Green
        Write-Host "   Name: $($reader['Name'])" -ForegroundColor Yellow
        Write-Host "   Code: $($reader['BuildingCode'])" -ForegroundColor Yellow
        Write-Host "   Phone: $($reader['Phone'])" -ForegroundColor Yellow
        Write-Host "   Fax: $($reader['Fax'])" -ForegroundColor Yellow
    } else {
        Write-Host "❌ 100 Church Street building not found" -ForegroundColor Red
    }
    
    $reader.Close()
    $connection.Close()
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# PowerShell script to check actual column names in PatientVitalSign table
$serverName = "BOOK-N38E1PL5F3"
$databaseName = "EMR"

Add-Type -AssemblyName System.Data

try {
    $connectionString = "Server=$serverName;Database=$databaseName;Integrated Security=True;TrustServerCertificate=True;"
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "PatientVitalSign Table Columns:" -ForegroundColor Green
    Write-Host "----------------------------------------"
    
    $query = @"
        SELECT 
            c.name AS ColumnName,
            t.name AS DataType,
            c.max_length AS MaxLength,
            CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END AS IsNullable
        FROM sys.columns c
        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID('PatientVitalSign')
        ORDER BY c.column_id
"@
    
    $command = New-Object System.Data.SqlClient.SqlCommand($query, $connection)
    $reader = $command.ExecuteReader()
    while ($reader.Read()) {
        Write-Host "  $($reader['ColumnName']) - $($reader['DataType']) (Nullable: $($reader['IsNullable']))" -ForegroundColor Cyan
    }
    $reader.Close()
    
    Write-Host ""
    Write-Host "Sample PatientVitalSign Data (Top 1):" -ForegroundColor Green
    Write-Host "----------------------------------------"
    $query2 = "SELECT TOP 1 * FROM PatientVitalSign"
    $command2 = New-Object System.Data.SqlClient.SqlCommand($query2, $connection)
    $reader2 = $command2.ExecuteReader()
    if ($reader2.Read()) {
        for ($i = 0; $i -lt $reader2.FieldCount; $i++) {
            $colName = $reader2.GetName($i)
            $value = if ($reader2.IsDBNull($i)) { "NULL" } else { $reader2[$i].ToString() }
            Write-Host "  $colName = $value" -ForegroundColor White
        }
    }
    $reader2.Close()
    
    $connection.Close()
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}














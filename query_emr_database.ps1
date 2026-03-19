# PowerShell script to query EMR database structure
# This uses .NET SqlClient to connect and query the database

$serverName = "BOOK-N38E1PL5F3"
$databaseName = "EMR"

# Load SQL Server types
Add-Type -AssemblyName System.Data

try {
    # Create connection string
    $connectionString = "Server=$serverName;Database=$databaseName;Integrated Security=True;TrustServerCertificate=True;"
    
    # Create connection
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "EMR DATABASE SCAN RESULTS" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    
    # Query 1: List all tables
    Write-Host "1. TABLES IN EMR DATABASE:" -ForegroundColor Yellow
    Write-Host "----------------------------------------"
    $query1 = @"
        SELECT 
            t.name AS TableName,
            (SELECT SUM(p.rows) 
             FROM sys.partitions p 
             WHERE p.object_id = t.object_id AND p.index_id IN (0,1)) AS [RowCount]
        FROM sys.tables t
        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        WHERE s.name = 'dbo'
        ORDER BY t.name
"@
    
    $command = New-Object System.Data.SqlClient.SqlCommand($query1, $connection)
    $reader = $command.ExecuteReader()
    while ($reader.Read()) {
        Write-Host "  Table: $($reader['TableName']) - Rows: $($reader['RowCount'])" -ForegroundColor Cyan
    }
    $reader.Close()
    Write-Host ""
    
    # Query 2: List all views
    Write-Host "2. VIEWS IN EMR DATABASE:" -ForegroundColor Yellow
    Write-Host "----------------------------------------"
    $query2 = "SELECT v.name AS ViewName FROM sys.views v INNER JOIN sys.schemas s ON v.schema_id = s.schema_id WHERE s.name = 'dbo' ORDER BY v.name"
    $command = New-Object System.Data.SqlClient.SqlCommand($query2, $connection)
    $reader = $command.ExecuteReader()
    $viewCount = 0
    while ($reader.Read()) {
        Write-Host "  View: $($reader['ViewName'])" -ForegroundColor Cyan
        $viewCount++
    }
    $reader.Close()
    if ($viewCount -eq 0) {
        Write-Host "  (No views found)" -ForegroundColor Gray
    }
    Write-Host ""
    
    # Query 3: List stored procedures
    Write-Host "3. STORED PROCEDURES IN EMR DATABASE:" -ForegroundColor Yellow
    Write-Host "----------------------------------------"
    $query3 = "SELECT p.name AS ProcedureName FROM sys.procedures p INNER JOIN sys.schemas s ON p.schema_id = s.schema_id WHERE s.name = 'dbo' ORDER BY p.name"
    $command = New-Object System.Data.SqlClient.SqlCommand($query3, $connection)
    $reader = $command.ExecuteReader()
    $procCount = 0
    while ($reader.Read()) {
        Write-Host "  Procedure: $($reader['ProcedureName'])" -ForegroundColor Cyan
        $procCount++
    }
    $reader.Close()
    if ($procCount -eq 0) {
        Write-Host "  (No stored procedures found)" -ForegroundColor Gray
    }
    Write-Host ""
    
    # Query 4: UnitOfMeasure table structure
    Write-Host "4. UnitOfMeasure TABLE STRUCTURE:" -ForegroundColor Yellow
    Write-Host "----------------------------------------"
    $query4 = @"
        SELECT 
            c.name AS ColumnName,
            t.name AS DataType,
            c.max_length AS MaxLength,
            CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END AS IsNullable
        FROM sys.columns c
        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID('UnitOfMeasure')
        ORDER BY c.column_id
"@
    $command = New-Object System.Data.SqlClient.SqlCommand($query4, $connection)
    $reader = $command.ExecuteReader()
    while ($reader.Read()) {
        Write-Host "  $($reader['ColumnName']) - $($reader['DataType']) (Nullable: $($reader['IsNullable']))" -ForegroundColor White
    }
    $reader.Close()
    Write-Host ""
    
    # Query 5: Sample UnitOfMeasure data
    Write-Host "5. Sample UnitOfMeasure Data (Top 5):" -ForegroundColor Yellow
    Write-Host "----------------------------------------"
    $query5 = "SELECT TOP 5 * FROM UnitOfMeasure"
    $command = New-Object System.Data.SqlClient.SqlCommand($query5, $connection)
    $reader = $command.ExecuteReader()
    $columns = @()
    for ($i = 0; $i -lt $reader.FieldCount; $i++) {
        $columns += $reader.GetName($i)
    }
    Write-Host "  Columns: $($columns -join ', ')" -ForegroundColor Gray
    while ($reader.Read()) {
        $row = @()
        for ($i = 0; $i -lt $reader.FieldCount; $i++) {
            $value = if ($reader.IsDBNull($i)) { "NULL" } else { $reader[$i].ToString() }
            $row += "$($columns[$i])=$value"
        }
        Write-Host "  $($row -join ', ')" -ForegroundColor White
    }
    $reader.Close()
    Write-Host ""
    
    # Query 6: OrderRequest table structure (if exists)
    $checkOrderRequest = "SELECT COUNT(*) AS TableExists FROM sys.tables WHERE name = 'OrderRequest'"
    $command = New-Object System.Data.SqlClient.SqlCommand($checkOrderRequest, $connection)
    $exists = $command.ExecuteScalar()
    
    if ($exists -gt 0) {
        Write-Host "6. OrderRequest TABLE STRUCTURE:" -ForegroundColor Yellow
        Write-Host "----------------------------------------"
        $query6 = @"
            SELECT 
                c.name AS ColumnName,
                t.name AS DataType,
                c.max_length AS MaxLength,
                CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END AS IsNullable
            FROM sys.columns c
            INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
            WHERE c.object_id = OBJECT_ID('OrderRequest')
            ORDER BY c.column_id
"@
        $command = New-Object System.Data.SqlClient.SqlCommand($query6, $connection)
        $reader = $command.ExecuteReader()
        while ($reader.Read()) {
            Write-Host "  $($reader['ColumnName']) - $($reader['DataType']) (Nullable: $($reader['IsNullable']))" -ForegroundColor White
        }
        $reader.Close()
        Write-Host ""
    }
    
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "SCAN COMPLETE" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    
    $connection.Close()
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}


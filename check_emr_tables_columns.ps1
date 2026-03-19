# PowerShell script to check actual column names in EMR tables used by PatientMedicalFileController
$serverName = "BOOK-N38E1PL5F3"
$databaseName = "EMR"

Add-Type -AssemblyName System.Data

try {
    $connectionString = "Server=$serverName;Database=$databaseName;Integrated Security=True;TrustServerCertificate=True;"
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    $tables = @("PatientMedicationSchedule", "ProgressNotes", "ClinicExam", "PatientHistoryTextHelper", "PatientRiskFactor", "PatientCurrentIllness", "PatientCardiacHX", "PatientMedicationHistory")
    
    foreach ($tableName in $tables) {
        Write-Host "`n$tableName Table Columns:" -ForegroundColor Green
        Write-Host "----------------------------------------"
        
        $query = @"
            SELECT 
                c.name AS ColumnName,
                t.name AS DataType,
                CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END AS IsNullable
            FROM sys.columns c
            INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
            WHERE c.object_id = OBJECT_ID('$tableName')
            ORDER BY c.column_id
"@
        
        $command = New-Object System.Data.SqlClient.SqlCommand($query, $connection)
        $reader = $command.ExecuteReader()
        while ($reader.Read()) {
            Write-Host "  $($reader['ColumnName']) - $($reader['DataType']) (Nullable: $($reader['IsNullable']))" -ForegroundColor Cyan
        }
        $reader.Close()
    }
    
    $connection.Close()
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}














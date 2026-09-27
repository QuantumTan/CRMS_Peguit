try {
    $connStr = "Server=(localdb)\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT name FROM sys.databases WHERE name LIKE 'CRMS%'"
    $r = $cmd.ExecuteReader()
    Write-Output "--- Local CRMS Databases ---"
    while ($r.Read()) {
        Write-Output $r.GetString(0)
    }
    $r.Close()
    $conn.Close()
} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

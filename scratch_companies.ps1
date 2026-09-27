try {
    $cloudConnStr = "Server=db66713.public.databaseasp.net;Database=db66713;User Id=db66713;Password=2Ni%Sz_9?J8m;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($cloudConnStr)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT * FROM CompanyDatabases"
    $r = $cmd.ExecuteReader()
    Write-Output "--- Rows in CompanyDatabases ---"
    while ($r.Read()) {
        $row = @()
        for ($i = 0; $i -lt $r.FieldCount; $i++) {
            $row += "$($r.GetName($i))=$($r.GetValue($i))"
        }
        Write-Output ($row -join ", ")
    }
    $r.Close()
    $conn.Close()
} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

try {
    $localConnStr = "Server=(localdb)\mssqllocaldb;Database=CRMS_Tenant_3;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($localConnStr)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
SELECT l.LeadId, p.FirstName, p.LastName, p.Email, p.Phone, l.Source, l.Stage, l.ExpectedValue, l.CreatedByUserId, l.CreatedAt
FROM Leads l
JOIN Persons p ON l.PersonId = p.PersonId
WHERE l.LeadId = 95
"@
    $r = $cmd.ExecuteReader()
    Write-Output "--- Local Lead #95 ---"
    while ($r.Read()) {
        Write-Output "Lead #$($r[0]): $($r[1]) $($r[2]), Email=$($r[3]), Phone=$($r[4]), Source=$($r[5]), Stage=$($r[6]), Value=$($r[7]), CreatedBy=$($r[8]), CreatedAt=$($r[9])"
    }
    $r.Close()
    $conn.Close()
} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

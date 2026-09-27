try {
    $cloudConnStr = "Server=db66713.public.databaseasp.net;Database=db66713;User Id=db66713;Password=2Ni%Sz_9?J8m;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    $localConnStr = "Server=(localdb)\mssqllocaldb;Database=CRMS_Tenant_3;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;"

    $cConn = New-Object System.Data.SqlClient.SqlConnection($cloudConnStr)
    $cConn.Open()
    $cmd = $cConn.CreateCommand()
    $cmd.CommandText = "SELECT l.LeadId, p.FirstName, p.LastName, p.Email, l.Source, l.Stage FROM Leads l JOIN Persons p ON l.PersonId = p.PersonId WHERE l.LeadId = 1"
    $r = $cmd.ExecuteReader()
    Write-Output "--- Cloud Lead #1 ---"
    if ($r.Read()) {
        Write-Output "Lead #$($r[0]): $($r[1]) $($r[2]) ($($r[3])), Source: $($r[4]), Stage: $($r[5])"
    }
    $r.Close()
    $cConn.Close()

    $lConn = New-Object System.Data.SqlClient.SqlConnection($localConnStr)
    $lConn.Open()
    $cmd = $lConn.CreateCommand()
    $cmd.CommandText = "SELECT l.LeadId, p.FirstName, p.LastName, p.Email, l.Source, l.Stage FROM Leads l JOIN Persons p ON l.PersonId = p.PersonId WHERE l.LeadId = 1"
    $r = $cmd.ExecuteReader()
    Write-Output "--- Local CRMS_Tenant_3 Lead #1 ---"
    if ($r.Read()) {
        Write-Output "Lead #$($r[0]): $($r[1]) $($r[2]) ($($r[3])), Source: $($r[4]), Stage: $($r[5])"
    }
    $r.Close()
    $lConn.Close()

} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

try {
    $cloudConnStr = "Server=db66713.public.databaseasp.net;Database=db66713;User Id=db66713;Password=2Ni%Sz_9?J8m;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    $cConn = New-Object System.Data.SqlClient.SqlConnection($cloudConnStr)
    $cConn.Open()
    $cmd = $cConn.CreateCommand()
    $cmd.CommandText = "SELECT l.LeadId, l.PersonId, p.FirstName, p.LastName, p.Email, l.CreatedByUserId, l.Source, l.Stage FROM Leads l JOIN Persons p ON l.PersonId = p.PersonId WHERE l.LeadId IN (5, 50)"
    $r = $cmd.ExecuteReader()
    Write-Output "--- Cloud Leads 5 & 50 ---"
    while ($r.Read()) {
        Write-Output "Lead #$($r[0]): PersonId=$($r[1]) $($r[2]) $($r[3]) ($($r[4])), CreatedByUserId=$($r[5]), Source=$($r[6]), Stage=$($r[7])"
    }
    $r.Close()
    $cConn.Close()
} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

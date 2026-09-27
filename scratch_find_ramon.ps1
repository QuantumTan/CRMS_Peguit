try {
    $cloudConnStr = "Server=db66713.public.databaseasp.net;Database=db66713;User Id=db66713;Password=2Ni%Sz_9?J8m;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    $cConn = New-Object System.Data.SqlClient.SqlConnection($cloudConnStr)
    $cConn.Open()
    $cmd = $cConn.CreateCommand()
    $cmd.CommandText = "SELECT p.PersonId, p.FirstName, p.LastName, p.Email, l.LeadId FROM Persons p LEFT JOIN Leads l ON p.PersonId = l.PersonId WHERE p.FirstName = 'Ramon' OR p.LastName = 'Valderama'"
    $r = $cmd.ExecuteReader()
    Write-Output "--- Search in Cloud for Ramon Valderama ---"
    while ($r.Read()) {
        Write-Output "Person #$($r[0]): $($r[1]) $($r[2]) ($($r[3])), LeadId: $($r[4])"
    }
    $r.Close()
    $cConn.Close()
} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

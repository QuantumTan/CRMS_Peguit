try {
    $cloudConnStr = "Server=db66713.public.databaseasp.net;Database=db66713;User Id=db66713;Password=2Ni%Sz_9?J8m;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    $cConn = New-Object System.Data.SqlClient.SqlConnection($cloudConnStr)
    $cConn.Open()
    $cmd = $cConn.CreateCommand()
    $cmd.CommandText = @"
SELECT l.CreatedByUserId, u.PersonId, p.FirstName, p.LastName, p.Email, COUNT(*) 
FROM Leads l
LEFT JOIN Users u ON l.CreatedByUserId = u.UserId
LEFT JOIN Persons p ON u.PersonId = p.PersonId
GROUP BY l.CreatedByUserId, u.PersonId, p.FirstName, p.LastName, p.Email
ORDER BY l.CreatedByUserId
"@
    $r = $cmd.ExecuteReader()
    Write-Output "--- Leads by CreatedByUserId in Cloud ---"
    while ($r.Read()) {
        Write-Output "CreatedByUserId: $($r[0]), User: $($r[2]) $($r[3]) ($($r[4])), Count: $($r[5])"
    }
    $r.Close()
    $cConn.Close()
} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

try {
    $cloudConnStr = "Server=db66713.public.databaseasp.net;Database=db66713;User Id=db66713;Password=2Ni%Sz_9?J8m;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($cloudConnStr)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
SELECT u.UserId, p.FirstName, p.LastName, p.Email, u.RoleId, r.RoleName, r.TenantId
FROM Users u
LEFT JOIN Persons p ON u.PersonId = p.PersonId
LEFT JOIN Roles r ON u.RoleId = r.RoleId
ORDER BY u.UserId
"@
    $r = $cmd.ExecuteReader()
    Write-Output "--- Cloud Users & Roles ---"
    while ($r.Read()) {
        Write-Output "User #$($r[0]): $($r[1]) $($r[2]) ($($r[3])), RoleId: $($r[4]) ($($r[5])), Role.TenantId: $($r[6])"
    }
    $r.Close()
    $conn.Close()
} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

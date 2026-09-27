try {
    $localConnStr = "Server=(localdb)\mssqllocaldb;Database=CRMS_Tenant_3;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($localConnStr)
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
    Write-Output "--- Local CRMS_Tenant_3 Users & Roles ---"
    while ($r.Read()) {
        Write-Output "User #$($r[0]): $($r[1]) $($r[2]) ($($r[3])), RoleId: $($r[4]) ($($r[5])), Role.TenantId: $($r[6])"
    }
    $r.Close()
    $conn.Close()
} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

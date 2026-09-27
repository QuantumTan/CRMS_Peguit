try {
    $cloudConnStr = "Server=db66713.public.databaseasp.net;Database=db66713;User Id=db66713;Password=2Ni%Sz_9?J8m;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($cloudConnStr)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
SELECT COUNT(*) 
FROM Leads l
JOIN Users u ON l.CreatedByUserId = u.UserId
JOIN Roles r ON u.RoleId = r.RoleId
WHERE r.TenantId = 3
"@
    $cntTenant3 = $cmd.ExecuteScalar()
    Write-Output "Cloud Leads for Tenant 3 (via User->Role->TenantId=3): $cntTenant3"

    $cmd.CommandText = @"
SELECT COUNT(*) 
FROM Leads l
JOIN Users u ON l.CreatedByUserId = u.UserId
JOIN Roles r ON u.RoleId = r.RoleId
WHERE r.TenantId = 1
"@
    $cntTenant1 = $cmd.ExecuteScalar()
    Write-Output "Cloud Leads for Tenant 1 (via User->Role->TenantId=1): $cntTenant1"

    $conn.Close()
} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

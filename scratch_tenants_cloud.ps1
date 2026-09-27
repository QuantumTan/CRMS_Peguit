try {
    $cloudConnStr = "Server=db66713.public.databaseasp.net;Database=db66713;User Id=db66713;Password=2Ni%Sz_9?J8m;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($cloudConnStr)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
SELECT r.TenantId, COUNT(*) 
FROM Customers c
JOIN Users u ON c.CreatedByUserId = u.UserId
JOIN Roles r ON u.RoleId = r.RoleId
GROUP BY r.TenantId
"@
    $r = $cmd.ExecuteReader()
    Write-Output "--- Customers by TenantId in Cloud ---"
    while ($r.Read()) {
        Write-Output "TenantId: $($r[0]), Count: $($r[1])"
    }
    $r.Close()

    $cmd.CommandText = @"
SELECT r.TenantId, COUNT(*) 
FROM Deals d
JOIN Users u ON d.CreatedByUserId = u.UserId
JOIN Roles r ON u.RoleId = r.RoleId
GROUP BY r.TenantId
"@
    $r = $cmd.ExecuteReader()
    Write-Output "--- Deals by TenantId in Cloud ---"
    while ($r.Read()) {
        Write-Output "TenantId: $($r[0]), Count: $($r[1])"
    }
    $r.Close()

    $cmd.CommandText = @"
SELECT r.TenantId, COUNT(*) 
FROM Users u
JOIN Roles r ON u.RoleId = r.RoleId
GROUP BY r.TenantId
"@
    $r = $cmd.ExecuteReader()
    Write-Output "--- Users by TenantId in Cloud ---"
    while ($r.Read()) {
        Write-Output "TenantId: $($r[0]), Count: $($r[1])"
    }
    $r.Close()

    $conn.Close()
} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

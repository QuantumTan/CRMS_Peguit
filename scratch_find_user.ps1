try {
    $cloudConnStr = "Server=db66713.public.databaseasp.net;Database=db66713;User Id=db66713;Password=2Ni%Sz_9?J8m;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    $cConn = New-Object System.Data.SqlClient.SqlConnection($cloudConnStr)
    $cConn.Open()
    $cmd = $cConn.CreateCommand()
    $cmd.CommandText = "SELECT u.*, r.* FROM Users u JOIN Roles r ON u.RoleId = r.RoleId WHERE u.UserId = 1024"
    $r = $cmd.ExecuteReader()
    Write-Output "--- Columns of User 1024 ---"
    if ($r.Read()) {
        for ($i = 0; $i -lt $r.FieldCount; $i++) {
            Write-Output "$($r.GetName($i)) = $($r.GetValue($i))"
        }
    }
    $r.Close()
    $cConn.Close()
} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

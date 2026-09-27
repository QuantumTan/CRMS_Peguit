try {
    $cloudConnStr = "Server=db66713.public.databaseasp.net;Database=db66713;User Id=db66713;Password=2Ni%Sz_9?J8m;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($cloudConnStr)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 10 LeadId, PersonId, CreatedByUserId, Stage, CreatedAt FROM Leads ORDER BY LeadId"
    $r = $cmd.ExecuteReader()
    Write-Output "--- Cloud Leads (Top 10) ---"
    while ($r.Read()) {
        Write-Output "LeadId: $($r[0]), PersonId: $($r[1]), CreatedByUserId: $($r[2]), Stage: $($r[3]), CreatedAt: $($r[4])"
    }
    $r.Close()

    $cmd.CommandText = "SELECT MAX(LeadId), MIN(LeadId), COUNT(*) FROM Leads"
    $r = $cmd.ExecuteReader()
    if ($r.Read()) {
        Write-Output "Cloud Leads: Min=$($r[1]), Max=$($r[0]), Count=$($r[2])"
    }
    $r.Close()

    $cmd.CommandText = "SELECT DISTINCT CreatedByUserId FROM Leads"
    $r = $cmd.ExecuteReader()
    Write-Output "--- Cloud Leads CreatedByUserIds ---"
    while ($r.Read()) {
        Write-Output "CreatedByUserId: $($r[0])"
    }
    $r.Close()

    $conn.Close()

    $localConnStr = "Server=(localdb)\mssqllocaldb;Database=CRMS_Tenant_3;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;MultipleActiveResultSets=True;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($localConnStr)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT MAX(LeadId), MIN(LeadId), COUNT(*) FROM Leads"
    $r = $cmd.ExecuteReader()
    if ($r.Read()) {
        Write-Output "Local Leads: Min=$($r[1]), Max=$($r[0]), Count=$($r[2])"
    }
    $r.Close()

    $cmd.CommandText = "SELECT TOP 10 LeadId, PersonId, CreatedByUserId, Stage, CreatedAt FROM Leads ORDER BY LeadId DESC"
    $r = $cmd.ExecuteReader()
    Write-Output "--- Local Leads (Latest 10) ---"
    while ($r.Read()) {
        Write-Output "LeadId: $($r[0]), PersonId: $($r[1]), CreatedByUserId: $($r[2]), Stage: $($r[3]), CreatedAt: $($r[4])"
    }
    $r.Close()

    $conn.Close()

} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

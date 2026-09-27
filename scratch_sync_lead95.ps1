try {
    $cloudConnStr = "Server=db66713.public.databaseasp.net;Database=db66713;User Id=db66713;Password=2Ni%Sz_9?J8m;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    $localConnStr = "Server=(localdb)\mssqllocaldb;Database=CRMS_Tenant_3;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;"

    $lConn = New-Object System.Data.SqlClient.SqlConnection($localConnStr)
    $lConn.Open()
    $cmd = $lConn.CreateCommand()
    $cmd.CommandText = @"
SELECT l.LeadId, p.FirstName, p.MiddleName, p.LastName, p.Suffix, p.Email, p.Phone,
       l.Source, l.Stage, l.ExpectedValue, l.Notes, l.Priority, l.AssignmentStatus, l.CreatedAt, l.CreatedByUserId,
       up.Email AS CreatorEmail
FROM Leads l
JOIN Persons p ON l.PersonId = p.PersonId
LEFT JOIN Users u ON l.CreatedByUserId = u.UserId
LEFT JOIN Persons up ON u.PersonId = up.PersonId
WHERE l.LeadId = 95
"@
    $r = $cmd.ExecuteReader()
    if (!$r.Read()) {
        Write-Output "Lead 95 not found in local DB!"
        return
    }

    $leadId = $r["LeadId"]
    $firstName = $r["FirstName"]
    $middleName = $r["MiddleName"]
    $lastName = $r["LastName"]
    $suffix = $r["Suffix"]
    $email = $r["Email"]
    $phone = $r["Phone"]
    $source = $r["Source"]
    $stage = $r["Stage"]
    $expectedValue = $r["ExpectedValue"]
    $notes = $r["Notes"]
    $priority = $r["Priority"]
    $assignmentStatus = $r["AssignmentStatus"]
    $createdAt = $r["CreatedAt"]
    $creatorEmail = $r["CreatorEmail"]
    $r.Close()
    $lConn.Close()

    Write-Output "Local Lead: $firstName $lastName, Email=$email, Creator=$creatorEmail"

    # Now connect to Cloud and sync
    $cConn = New-Object System.Data.SqlClient.SqlConnection($cloudConnStr)
    $cConn.Open()

    # 1. Match or create Cloud Creator User
    $cloudUserId = 1
    if ($creatorEmail) {
        $cmd = $cConn.CreateCommand()
        $cmd.CommandText = "SELECT u.UserId FROM Users u JOIN Persons p ON u.PersonId = p.PersonId WHERE p.Email = @email"
        $cmd.Parameters.AddWithValue("@email", $creatorEmail)
        $val = $cmd.ExecuteScalar()
        if ($val) { $cloudUserId = [int]$val }
    }
    Write-Output "Cloud Creator UserId: $cloudUserId"

    # 2. Match or create Cloud Person
    $cloudPersonId = 0
    if ($email) {
        $cmd = $cConn.CreateCommand()
        $cmd.CommandText = "SELECT PersonId FROM Persons WHERE Email = @email"
        $cmd.Parameters.AddWithValue("@email", $email)
        $val = $cmd.ExecuteScalar()
        if ($val) { $cloudPersonId = [int]$val }
    }
    if ($cloudPersonId -eq 0) {
        $cmd = $cConn.CreateCommand()
        $cmd.CommandText = @"
INSERT INTO Persons (FirstName, MiddleName, LastName, Suffix, Email, Phone, CreatedAt)
VALUES (@fn, @mn, @ln, @sfx, @em, @ph, GETUTCDATE());
SELECT SCOPE_IDENTITY();
"@
        $cmd.Parameters.AddWithValue("@fn", $firstName)
        $cmd.Parameters.AddWithValue("@mn", $(if ($middleName) { $middleName } else { [DBNull]::Value }))
        $cmd.Parameters.AddWithValue("@ln", $lastName)
        $cmd.Parameters.AddWithValue("@sfx", $(if ($suffix) { $suffix } else { [DBNull]::Value }))
        $cmd.Parameters.AddWithValue("@em", $(if ($email) { $email } else { [DBNull]::Value }))
        $cmd.Parameters.AddWithValue("@ph", $(if ($phone) { $phone } else { [DBNull]::Value }))
        $cloudPersonId = [int]$cmd.ExecuteScalar()
        Write-Output "Created Cloud Person #$cloudPersonId"
    } else {
        Write-Output "Found Cloud Person #$cloudPersonId"
    }

    # 3. Match or insert Cloud Lead
    $cmd = $cConn.CreateCommand()
    $cmd.CommandText = "SELECT LeadId FROM Leads WHERE PersonId = @pid"
    $cmd.Parameters.AddWithValue("@pid", $cloudPersonId)
    $existingLeadId = $cmd.ExecuteScalar()

    if ($existingLeadId) {
        $cmd = $cConn.CreateCommand()
        $cmd.CommandText = @"
UPDATE Leads SET 
    Source = @src, Stage = @stg, ExpectedValue = @val, Notes = @notes, 
    Priority = @pri, AssignmentStatus = @asg
WHERE LeadId = @lid
"@
        $cmd.Parameters.AddWithValue("@src", $(if ($source) { $source } else { [DBNull]::Value }))
        $cmd.Parameters.AddWithValue("@stg", $stage)
        $cmd.Parameters.AddWithValue("@val", $(if ($expectedValue -ne [DBNull]::Value -and $expectedValue) { $expectedValue } else { [DBNull]::Value }))
        $cmd.Parameters.AddWithValue("@notes", $(if ($notes) { $notes } else { [DBNull]::Value }))
        $cmd.Parameters.AddWithValue("@pri", $(if ($priority) { $priority } else { "medium" }))
        $cmd.Parameters.AddWithValue("@asg", $(if ($assignmentStatus) { $assignmentStatus } else { "pending_review" }))
        $cmd.Parameters.AddWithValue("@lid", $existingLeadId)
        $cmd.ExecuteNonQuery()
        Write-Output "Updated existing Cloud Lead #$existingLeadId"
    } else {
        $cmd = $cConn.CreateCommand()
        $cmd.CommandText = @"
INSERT INTO Leads (PersonId, CreatedByUserId, Source, Stage, ExpectedValue, Notes, Priority, AssignmentStatus, CreatedAt, IsDeleted)
VALUES (@pid, @cb, @src, @stg, @val, @notes, @pri, @asg, GETUTCDATE(), 0);
SELECT SCOPE_IDENTITY();
"@
        $cmd.Parameters.AddWithValue("@pid", $cloudPersonId)
        $cmd.Parameters.AddWithValue("@cb", $cloudUserId)
        $cmd.Parameters.AddWithValue("@src", $(if ($source) { $source } else { [DBNull]::Value }))
        $cmd.Parameters.AddWithValue("@stg", $stage)
        $cmd.Parameters.AddWithValue("@val", $(if ($expectedValue -ne [DBNull]::Value -and $expectedValue) { $expectedValue } else { [DBNull]::Value }))
        $cmd.Parameters.AddWithValue("@notes", $(if ($notes) { $notes } else { [DBNull]::Value }))
        $cmd.Parameters.AddWithValue("@pri", $(if ($priority) { $priority } else { "medium" }))
        $cmd.Parameters.AddWithValue("@asg", $(if ($assignmentStatus) { $assignmentStatus } else { "pending_review" }))
        $newServerLeadId = [int]$cmd.ExecuteScalar()
        Write-Output "Successfully synced Lead 95 to MonsterASP Cloud! New Cloud LeadId: $newServerLeadId"
    }

    $cConn.Close()
} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

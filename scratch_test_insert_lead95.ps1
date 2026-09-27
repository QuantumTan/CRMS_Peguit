try {
    $cloudConnStr = "Server=db66713.public.databaseasp.net;Database=db66713;User Id=db66713;Password=2Ni%Sz_9?J8m;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    $cConn = New-Object System.Data.SqlClient.SqlConnection($cloudConnStr)
    $cConn.Open()
    $cmd = $cConn.CreateCommand()
    $cmd.CommandText = @"
SET IDENTITY_INSERT Leads ON;
INSERT INTO Leads (LeadId, PersonId, CreatedByUserId, Source, Stage, ExpectedValue, Notes, Priority, AssignmentStatus, CreatedAt, IsDeleted)
VALUES (95, 100412, 1016, 'Facebook Ad', 'contacted', NULL, NULL, 'medium', 'pending_review', GETUTCDATE(), 0);
SET IDENTITY_INSERT Leads OFF;
"@
    try {
        $cmd.ExecuteNonQuery()
        Write-Output "Successfully inserted Lead 95 into Cloud!"
    } catch {
        Write-Output "Error inserting Lead 95: $($_.Exception.ToString())"
    }
    $cConn.Close()
} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

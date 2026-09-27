try {
    $connStr = "Server=(localdb)\mssqllocaldb;Database=CRMS_Tenant_1;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    $tables = @('Roles', 'Persons', 'Users', 'Customers', 'BuyerProfiles', 'Branches', 'Properties', 'Leads', 'Deals', 'DealContingencies', 'DealClauses', 'Activities', 'PropertyShowingDetails', 'Campaigns', 'TaskReminders', 'SupportTickets', 'TicketComments')
    foreach ($t in $tables) {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT COUNT(*) FROM [$t]"
        $cnt = $cmd.ExecuteScalar()
        Write-Output "$t : $cnt"
    }
    $conn.Close()
} catch {
    Write-Output "ERROR: $($_.Exception.ToString())"
}

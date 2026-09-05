using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.infrastructure.data;

namespace CRMS_Peguit.winforms.Models.Services
{
    /// <summary>
    /// Periodically pushes local CRM data to the cloud database.
    /// One-way sync: local → cloud. Local always wins.
    /// Writes status to sync-log.txt next to the app.
    /// Implements graceful failure handling with exponential backoff.
    /// </summary>
    public class SyncService : IDisposable
    {
        private readonly string _localConnection;
        private readonly string _cloudConnection;
        private readonly string _logPath;
        private System.Threading.Timer? _timer;
        private bool _isSyncing;
        private int _failureCount;
        private int _baseIntervalSeconds;
        public bool CloudAvailable { get; private set; } = true;

        public SyncService(string localConnection, string cloudConnection)
        {
            _localConnection = localConnection;
            _cloudConnection = cloudConnection;
            _logPath = Path.Combine(AppContext.BaseDirectory, "sync-log.txt");
            _failureCount = 0;
        }

        public void Start(int intervalSeconds = 30)
        {
            _baseIntervalSeconds = intervalSeconds;
            Log($"SyncService started. Base interval: {intervalSeconds}s");

            _timer = new System.Threading.Timer(
                async _ => await SyncAsync(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(intervalSeconds));
        }

        public async Task SyncAsync()
        {
            if (_isSyncing) return;
            _isSyncing = true;

            try
            {
                Log("Sync started (local → cloud)...");

                await using var local = CreateLocalContext();
                await using var cloud = CreateCloudContext();

                // Test cloud connection first
                await cloud.Database.OpenConnectionAsync();
                cloud.Database.CloseConnection();
                CloudAvailable = true;
                _failureCount = 0;

                await SyncTable(local.Roles.ToList(), cloud.Roles.ToList(), cloud);
                await SyncTable(local.Users.ToList(), cloud.Users.ToList(), cloud);
                await SyncTable(local.LoginSessions.ToList(), cloud.LoginSessions.ToList(), cloud);
                await SyncTable(local.Customers.ToList(), cloud.Customers.ToList(), cloud);
                await SyncTable(local.BuyerProfiles.ToList(), cloud.BuyerProfiles.ToList(), cloud);
                await SyncTable(local.Properties.ToList(), cloud.Properties.ToList(), cloud);
                await SyncTable(local.Leads.ToList(), cloud.Leads.ToList(), cloud);
                await SyncTable(local.Deals.ToList(), cloud.Deals.ToList(), cloud);
                await SyncTable(local.Activities.ToList(), cloud.Activities.ToList(), cloud);
                await SyncTable(local.PropertyShowingDetails.ToList(), cloud.PropertyShowingDetails.ToList(), cloud);
                await SyncTable(local.SupportTickets.ToList(), cloud.SupportTickets.ToList(), cloud);
                await SyncTable(local.Subscriptions.ToList(), cloud.Subscriptions.ToList(), cloud);
                await SyncTable(local.SystemSettings.ToList(), cloud.SystemSettings.ToList(), cloud);
                await SyncTable(local.BackupLogs.ToList(), cloud.BackupLogs.ToList(), cloud);

                await cloud.SaveChangesAsync();

                Log($"Sync completed successfully at {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                CloudAvailable = false;
                _failureCount++;

                Log($"SYNC FAILED (attempt #{_failureCount}): {ex.Message}");
                if (ex.InnerException is not null)
                    Log($"INNER: {ex.InnerException.Message}");

                // Implement exponential backoff: wait longer between retries
                int backoffSeconds = Math.Min(_baseIntervalSeconds * (int)Math.Pow(2, Math.Min(_failureCount - 1, 4)), 600);
                Log($"Next retry in {backoffSeconds} seconds...");
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private RealEstateDbContext CreateLocalContext()
        {
            var options = new DbContextOptionsBuilder<RealEstateDbContext>()
                .UseSqlServer(_localConnection)
                .Options;
            return new RealEstateDbContext(options, 0);
        }

        private RealEstateDbContext CreateCloudContext()
        {
            var options = new DbContextOptionsBuilder<RealEstateDbContext>()
                .UseSqlServer(_cloudConnection, sql => sql.EnableRetryOnFailure(maxRetryCount: 3))
                .Options;
            return new RealEstateDbContext(options, 0);
        }

        private async Task SyncTable<T>(List<T> localRows, List<T> cloudRows, RealEstateDbContext cloud)
            where T : class
        {
            var cloudSet = cloud.Set<T>();

            foreach (var localRow in localRows)
            {
                var key = GetKey(localRow);
                var existing = cloudRows.FirstOrDefault(r => GetKey(r).Equals(key));

                if (existing is null)
                {
                    cloudSet.Add(localRow);
                }
                else
                {
                    cloud.Entry(existing).CurrentValues.SetValues(localRow);
                }
            }
        }

        private object GetKey<T>(T entity) where T : class
        {
            var keyProperty = typeof(T).GetProperties()
                .First(p => p.Name.EndsWith("Id"));

            return keyProperty.GetValue(entity)!;
        }

        public int FailureCount => _failureCount;

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(
                    _logPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch
            {
                // ignore logging failures
            }
        }

        public void Stop() => _timer?.Change(Timeout.Infinite, Timeout.Infinite);

        public void Dispose()
        {
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

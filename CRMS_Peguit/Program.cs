using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms
{
    internal static class Program
    {
        private static SyncService? _syncService;

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var localConnection =
                "Server=localhost\\SQLEXPRESS;Database=CRMS_Local;Trusted_Connection=True;TrustServerCertificate=True;";

            var cloudConnection =
                "Server=db66713.public.databaseasp.net; Database=db66713; User Id=db66713; Password=2Ni%Sz_9?J8m; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

            // Test connections and show diagnostic info
            string testResult = ConnectionTester.Test();
            MessageBox.Show(testResult, "DATABASE CONNECTION TEST", MessageBoxButtons.OK, 
                testResult.Contains("✗") ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

            // Initialize and start sync service (won't block app if cloud fails)
            _syncService = new SyncService(localConnection, cloudConnection);
            _syncService.Start(30);

            Application.Run(new Form1());

            _syncService?.Dispose();
        }
    }
}

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

            // ==================================================
            // LOCAL DATABASE
            // ==================================================

            var localConnection =
                "Server=localhost\\SQLEXPRESS;" +
                "Database=CRMS_Local;" +
                "Trusted_Connection=True;" +
                "TrustServerCertificate=True;";

            // ==================================================
            // CLOUD DATABASE
            // ==================================================
            //
            // Keep your actual connection string here for now.
            // Do NOT commit database credentials to Git.
            //

            var cloudConnection =
                "YOUR_CLOUD_CONNECTION_STRING";

            // ==================================================
            // START SYNC SERVICE
            // ==================================================

            _syncService = new SyncService(
                localConnection,
                cloudConnection
            );

            _syncService.Start(30);

            // ==================================================
            // START LOGIN FORM
            // ==================================================

            using var loginForm = new LoginForm();

            Application.Run(loginForm);

            // ==================================================
            // CLEAN UP
            // ==================================================

            _syncService?.Dispose();
        }
    }
}
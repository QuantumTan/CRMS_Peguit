using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Views.Dashboard;
using CRMS_Peguit.winforms.Views.Customers;
using CRMS_Peguit.winforms.Views.Leads;
using CRMS_Peguit.winforms.Views.Properties;
using CRMS_Peguit.winforms.Views.Deals;
using ReaLTaiizor.Forms;

namespace CRMS_Peguit.winforms
{
    public partial class Form1 : MaterialForm
    {
        public Form1()
        {
            InitializeComponent();

            ApplyRolePermissions();

            ShowView(new DashboardView());
        }

        // =====================================================
        // ROLE PERMISSIONS
        // =====================================================

        private void ApplyRolePermissions()
        {
            if (CurrentSession.CurrentUser is null)
            {
                MessageBox.Show(
                    "No active session. Please sign in again.",
                    "Session Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                Application.Exit();

                return;
            }

            // ---------------------------------------------
            // Navigation permissions
            // ---------------------------------------------

            btnCustomers.Visible =
                CurrentSession.CanAccess("Customers");

            btnLeads.Visible =
                CurrentSession.CanAccess("Leads");

            btnProperties.Visible =
                CurrentSession.CanAccess("Properties");

            btnDeals.Visible =
                CurrentSession.CanAccess("Deals");

            // ---------------------------------------------
            // Window title
            // ---------------------------------------------

            Text =
                $"CRMS - {CurrentSession.CurrentUser.FullName} " +
                $"({CurrentSession.CurrentUser.GetDashboardType()})";
        }

        // =====================================================
        // VIEW MANAGEMENT
        // =====================================================

        private void ShowView(UserControl view)
        {
            mainPanel.Controls.Clear();

            view.Dock = DockStyle.Fill;

            mainPanel.Controls.Add(view);
        }

        // =====================================================
        // DASHBOARD
        // =====================================================

        private void BtnDashboardClick(
            object? sender,
            EventArgs e)
        {
            ShowView(new DashboardView());
        }

        // =====================================================
        // CUSTOMERS
        // =====================================================

        private void BtnCustomersClick(
            object? sender,
            EventArgs e)
        {
            if (!CurrentSession.CanAccess("Customers"))
                return;

            ShowView(new CustomersView());
        }

        // =====================================================
        // LEADS
        // =====================================================

        private void BtnLeadsClick(
            object? sender,
            EventArgs e)
        {
            if (!CurrentSession.CanAccess("Leads"))
                return;

            ShowView(new LeadsView());
        }

        // =====================================================
        // PROPERTIES
        // =====================================================

        private void BtnPropertiesClick(
            object? sender,
            EventArgs e)
        {
            if (!CurrentSession.CanAccess("Properties"))
                return;

            ShowView(new PropertiesView());
        }

        // =====================================================
        // DEALS
        // =====================================================

        private void BtnDealsClick(
            object? sender,
            EventArgs e)
        {
            if (!CurrentSession.CanAccess("Deals"))
                return;

            ShowView(new DealsView());
        }

        // =====================================================
        // LOGOUT
        // =====================================================

        private void BtnLogoutClick(
            object? sender,
            EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            // User selected No.
            if (result != DialogResult.Yes)
            {
                return;
            }

            // ---------------------------------------------
            // Clear current session
            // ---------------------------------------------

            CurrentSession.SignOut();

            // ---------------------------------------------
            // Hide CRM window
            // ---------------------------------------------

            Hide();

            // ---------------------------------------------
            // Show login screen
            // ---------------------------------------------

            using var loginForm = new LoginForm();

            loginForm.ShowDialog();

            // ---------------------------------------------
            // Check result of login
            // ---------------------------------------------

            if (CurrentSession.CurrentUser != null)
            {
                // Login successful.
                Show();
            }
            else
            {
                // Login form was closed without logging in.
                Close();
            }
        }
    }
}
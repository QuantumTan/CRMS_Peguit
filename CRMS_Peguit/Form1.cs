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

            // Always start with a fresh Dashboard.
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

                Close();

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
            // Dispose the old view before replacing it.
            foreach (Control control in mainPanel.Controls)
            {
                control.Dispose();
            }

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

            if (result != DialogResult.Yes)
            {
                return;
            }

            // Clear the current authentication session.
            CurrentSession.SignOut();

            // Completely destroy this Form1.
            // LoginForm will create a new Form1 after login.
            Close();
        }
    }
}
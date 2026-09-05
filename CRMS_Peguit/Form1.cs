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

        // Hides/disables nav buttons the current role shouldn't see,
        // driven by CurrentUser.GetAccessibleModules() from NEXA.Model.
        private void ApplyRolePermissions()
        {
            if (CurrentSession.CurrentUser is null)
            {
                // Safety net - nobody should reach Form1 without a session
                MessageBox.Show("No active session. Please sign in again.", "Session Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            btnCustomers.Visible = CurrentSession.CanAccess("Customers");
            btnLeads.Visible = CurrentSession.CanAccess("Leads");
            btnProperties.Visible = CurrentSession.CanAccess("Properties");
            btnDeals.Visible = CurrentSession.CanAccess("Deals");

            Text = $"CRMS - {CurrentSession.CurrentUser.FullName} ({CurrentSession.CurrentUser.GetDashboardType()})";
        }

        private void ShowView(UserControl view)
        {
            mainPanel.Controls.Clear();
            view.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(view);
        }

        private void BtnDashboardClick(object? sender, EventArgs e) => ShowView(new DashboardView());

        private void BtnCustomersClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("Customers")) return;
            ShowView(new CustomersView());
        }

        private void BtnLeadsClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("Leads")) return;
            ShowView(new LeadsView());
        }

        private void BtnPropertiesClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("Properties")) return;
            ShowView(new PropertiesView());
        }

        private void BtnDealsClick(object? sender, EventArgs e)
        {
            if (!CurrentSession.CanAccess("Deals")) return;
            ShowView(new DealsView());
        }
    }
}
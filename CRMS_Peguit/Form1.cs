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
            ShowView(new DashboardView());
        }

        private void ShowView(UserControl view)
        {
            mainPanel.Controls.Clear();
            view.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(view);
        }

        private void BtnDashboardClick(object? sender, EventArgs e) => ShowView(new DashboardView());
        private void BtnCustomersClick(object? sender, EventArgs e) => ShowView(new CustomersView());
        private void BtnLeadsClick(object? sender, EventArgs e) => ShowView(new LeadsView());
        private void BtnPropertiesClick(object? sender, EventArgs e) => ShowView(new PropertiesView());
        private void BtnDealsClick(object? sender, EventArgs e) => ShowView(new DealsView());
    }
}
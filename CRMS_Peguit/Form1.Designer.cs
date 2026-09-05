using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms
{
    partial class Form1   // ← no base class here, matches Form1.cs
    {
        private Panel sidebarPanel;
        private Panel mainPanel;
        private Button btnDashboard;
        private Button btnCustomers;
        private Button btnLeads;
        private Button btnProperties;
        private Button btnDeals;
        private Label lblLogo;

        private void InitializeComponent()
        {
            sidebarPanel = new Panel
            {
                BackColor = Theme.Surface,
                Dock = DockStyle.Left,
                Width = 220
            };

            mainPanel = new Panel
            {
                BackColor = Theme.Background,
                Dock = DockStyle.Fill
            };

            lblLogo = new Label
            {
                Text = "CRMS",
                ForeColor = Theme.Primary,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                Location = new Point(25, 25),
                AutoSize = true
            };

            btnDashboard = NavButton("Dashboard", 90);
            btnCustomers = NavButton("Customers", 145);
            btnLeads = NavButton("Leads", 200);
            btnProperties = NavButton("Properties", 255);
            btnDeals = NavButton("Deals", 310);

            btnDashboard.Click += BtnDashboardClick;
            btnCustomers.Click += BtnCustomersClick;
            btnLeads.Click += BtnLeadsClick;
            btnProperties.Click += BtnPropertiesClick;
            btnDeals.Click += BtnDealsClick;

            sidebarPanel.Controls.Add(lblLogo);
            sidebarPanel.Controls.Add(btnDashboard);
            sidebarPanel.Controls.Add(btnCustomers);
            sidebarPanel.Controls.Add(btnLeads);
            sidebarPanel.Controls.Add(btnProperties);
            sidebarPanel.Controls.Add(btnDeals);

            ClientSize = new Size(1200, 720);
            Controls.Add(mainPanel);
            Controls.Add(sidebarPanel);
            Text = "CRMS";
            StartPosition = FormStartPosition.CenterScreen;
        }

        private Button NavButton(string text, int y)
        {
            return new Button
            {
                Text = "  " + text,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 10.5f),
                Location = new Point(10, y),
                Size = new Size(200, 45),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }
    }
}
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms
{
    partial class Form1
    {
        private Panel sidebarPanel;
        private Panel mainPanel;

        private Button btnDashboard;
        private Button btnCustomers;
        private Button btnLeads;
        private Button btnProperties;
        private Button btnDeals;
        private Button btnLogout;

        private Label lblLogo;

        private void InitializeComponent()
        {
            // ==========================================
            // SIDEBAR
            // ==========================================

            sidebarPanel = new Panel
            {
                BackColor = Theme.Surface,
                Dock = DockStyle.Left,
                Width = 220
            };

            // ==========================================
            // MAIN PANEL
            // ==========================================

            mainPanel = new Panel
            {
                BackColor = Theme.Background,
                Dock = DockStyle.Fill
            };

            // ==========================================
            // LOGO
            // ==========================================

            lblLogo = new Label
            {
                Text = "CRMS",
                ForeColor = Theme.Primary,
                Font = new Font(
                    "Segoe UI",
                    22,
                    FontStyle.Bold
                ),
                Location = new Point(25, 25),
                AutoSize = true
            };

            // ==========================================
            // NAVIGATION BUTTONS
            // ==========================================

            btnDashboard = NavButton(
                "Dashboard",
                90
            );

            btnCustomers = NavButton(
                "Customers",
                145
            );

            btnLeads = NavButton(
                "Leads",
                200
            );

            btnProperties = NavButton(
                "Properties",
                255
            );

            btnDeals = NavButton(
                "Deals",
                310
            );

            // ==========================================
            // LOGOUT BUTTON
            // ==========================================

            btnLogout = new Button
            {
                Text = "  Logout",

                FlatStyle = FlatStyle.Flat,

                BackColor = Theme.Surface,

                ForeColor = Theme.TextPrimary,

                Font = new Font(
                    "Segoe UI",
                    10.5f
                ),

                Dock = DockStyle.Bottom,

                Height = 50,

                TextAlign =
                    ContentAlignment.MiddleLeft,

                Cursor = Cursors.Hand
            };

            btnLogout.FlatAppearance.BorderSize = 0;

            // ==========================================
            // EVENTS
            // ==========================================

            btnDashboard.Click += BtnDashboardClick;

            btnCustomers.Click += BtnCustomersClick;

            btnLeads.Click += BtnLeadsClick;

            btnProperties.Click += BtnPropertiesClick;

            btnDeals.Click += BtnDealsClick;

            btnLogout.Click += BtnLogoutClick;

            // ==========================================
            // ADD CONTROLS TO SIDEBAR
            // ==========================================

            sidebarPanel.Controls.Add(lblLogo);

            sidebarPanel.Controls.Add(btnDashboard);

            sidebarPanel.Controls.Add(btnCustomers);

            sidebarPanel.Controls.Add(btnLeads);

            sidebarPanel.Controls.Add(btnProperties);

            sidebarPanel.Controls.Add(btnDeals);

            // Logout remains at the bottom.
            sidebarPanel.Controls.Add(btnLogout);

            // ==========================================
            // FORM
            // ==========================================

            ClientSize = new Size(
                1200,
                720
            );

            Controls.Add(mainPanel);

            Controls.Add(sidebarPanel);

            Text = "CRMS";

            StartPosition =
                FormStartPosition.CenterScreen;
        }

        // ==========================================
        // CREATE NAVIGATION BUTTON
        // ==========================================

        private Button NavButton(
            string text,
            int y)
        {
            var button = new Button
            {
                Text = "  " + text,

                FlatStyle =
                    FlatStyle.Flat,

                BackColor =
                    Theme.Surface,

                ForeColor =
                    Theme.TextPrimary,

                Font = new Font(
                    "Segoe UI",
                    10.5f
                ),

                Location =
                    new Point(10, y),

                Size =
                    new Size(200, 45),

                TextAlign =
                    ContentAlignment.MiddleLeft,

                Cursor =
                    Cursors.Hand
            };

            button.FlatAppearance.BorderSize = 0;

            return button;
        }
    }
}
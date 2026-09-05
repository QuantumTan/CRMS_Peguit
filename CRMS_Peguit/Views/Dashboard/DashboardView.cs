using CRMS_Peguit.Models;

namespace CRMS_Peguit.winforms.Views.Dashboard
{
    public class DashboardView : UserControl
    {
        public DashboardView()
        {
            BackColor = AzureTints.BackgroundWash;

            Controls.Add(new Label
            {
                Text = "Dashboard",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = AzureShades.DeepInterface,
                Location = new Point(40, 40),
                AutoSize = true
            });

            Controls.Add(new Label
            {
                Text = "Overview of your real estate operations",
                Font = new Font("Segoe UI", 11),
                ForeColor = AzureTones.FaintBlueLabel,
                Location = new Point(40, 80),
                AutoSize = true
            });
        }
    }
}
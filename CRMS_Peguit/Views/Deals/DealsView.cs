namespace CRMS_Peguit.winforms.Views.Deals
{
    public class DealsView : UserControl
    {
        public DealsView()
        {
            BackColor = Color.FromArgb(245, 245, 250);
            Controls.Add(new Label
            {
                Text = "Deals",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 46),
                Location = new Point(40, 40),
                AutoSize = true
            });
        }
    }
}
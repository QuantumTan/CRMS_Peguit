namespace CRMS_Peguit.winforms.Views.Properties
{
    public class PropertiesView : UserControl
    {
        public PropertiesView()
        {
            BackColor = Color.FromArgb(245, 245, 250);
            Controls.Add(new Label
            {
                Text = "Properties",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 46),
                Location = new Point(40, 40),
                AutoSize = true
            });
        }
    }
}
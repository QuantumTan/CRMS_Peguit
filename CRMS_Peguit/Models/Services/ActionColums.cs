namespace CRMS_Peguit.winforms.Models.Services
{
    public class ActionsColumn : DataGridViewButtonColumn
    {
        public ActionsColumn()
        {
            Name = "Actions";
            HeaderText = string.Empty;
            Text = "⋮";
            UseColumnTextForButtonValue = true;
            Width = 60;
            FlatStyle = FlatStyle.Flat;
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment =
                    DataGridViewContentAlignment.MiddleCenter,

                Font = new Font(
                    "Segoe UI",
                    14,
                    FontStyle.Bold),

                ForeColor = Theme.TextPrimary,
                SelectionBackColor = Theme.Border,
                SelectionForeColor = Theme.TextPrimary,
                BackColor = Theme.Surface,
                Padding = new Padding(0)
            };
        }
    }
}
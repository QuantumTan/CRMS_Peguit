using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Controllers;

namespace CRMS_Peguit.winforms.Views.Shared
{
    public partial class AssignAgentDialog : Form
    {
        private readonly List<AgentPickerItem> _agents;

        public int? SelectedAgentId { get; private set; }
        public string? ReviewNotes { get; private set; }
        public bool ShouldApprove { get; private set; }
        public bool ApproveNow => ShouldApprove;

        public AssignAgentDialog() : this("Record", new List<AgentPickerItem>(), null)
        {
        }

        public AssignAgentDialog(string recordTitle, List<AgentPickerItem> agents, int? currentAgentId = null)
        {
            _agents = agents ?? new List<AgentPickerItem>();
            InitializeComponent();

            ApplyTheme();

            lblRecord.Text = $"📄  {recordTitle}";
            lblAgentCount.Text = _agents.Count > 0 ? $"{_agents.Count} agent{(_agents.Count == 1 ? "" : "s")} available" : "No agents found";

            PopulateAgents(currentAgentId);

            btnSave.Click += BtnSaveClick;
        }

        private void ApplyTheme()
        {
            UiRadiusHelper.StyleButton(btnSave, 8);
            UiRadiusHelper.StyleButton(btnCancel, 8);
            UiRadiusHelper.ApplyRoundedCorners(pnlApproveCard, 6);
            UiRadiusHelper.ApplyRoundedCorners(lblRecord, 6);

            // Hover feedback on approve card to indicate interactivity
            pnlApproveCard.Cursor = Cursors.Hand;
            pnlApproveCard.Click += (_, _) => chkApprove.Checked = !chkApprove.Checked;
        }

        private void PopulateAgents(int? currentAgentId)
        {
            cmbAgents.Items.Clear();
            cmbAgents.Items.Add("-- Unassigned --");

            AgentPickerItem? selected = null;
            foreach (var agent in _agents)
            {
                cmbAgents.Items.Add(agent);
                if (currentAgentId.HasValue && agent.UserId == currentAgentId.Value)
                {
                    selected = agent;
                }
            }

            if (selected != null)
            {
                cmbAgents.SelectedItem = selected;
            }
            else
            {
                cmbAgents.SelectedIndex = 0;
            }
        }

        private void BtnSaveClick(object? sender, EventArgs e)
        {
            if (cmbAgents.SelectedItem is AgentPickerItem agent)
            {
                SelectedAgentId = agent.UserId;
            }
            else
            {
                SelectedAgentId = null;
            }

            ReviewNotes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim();
            ShouldApprove = chkApprove.Checked;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}


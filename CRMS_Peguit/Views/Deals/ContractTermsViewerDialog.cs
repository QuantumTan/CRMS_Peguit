using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.Deals
{
    /// <summary>
    /// Professional in-app viewer for Real Estate Reservation Agreements & Commercial Terms.
    /// </summary>
    public sealed class ContractTermsViewerDialog : Form
    {
        private readonly Deal _deal;
        private readonly string _documentText;
        private Panel _pnlHeader = null!;
        private Panel _pnlFooter = null!;
        private TextBox _txtContent = null!;
        private Button _btnCopy = null!;
        private Button _btnExport = null!;
        private Button _btnClose = null!;

        public ContractTermsViewerDialog(Deal deal, DealController controller)
        {
            _deal = deal ?? throw new ArgumentNullException(nameof(deal));

            string buyer = _deal.Customer?.FullName
                ?? (controller.GetCustomerNames().TryGetValue(_deal.CustomerId, out string? b) ? b : $"Customer #{_deal.CustomerId}");
            string prop = _deal.Property?.Address
                ?? (controller.GetPropertyAddresses().TryGetValue(_deal.PropertyId, out string? p) ? p : $"Property #{_deal.PropertyId}");
            string agent = _deal.Agent?.FullName
                ?? (_deal.AgentId.HasValue && controller.GetAgentNames().TryGetValue(_deal.AgentId.Value, out string? a) ? a : "Unassigned");

            _documentText = DealClauseLibrary.FormatTermSheetText(_deal, buyer, prop, agent);

            SetupForm();
            BuildHeader(buyer, prop);
            BuildFooter();
            BuildContent();
            LayoutResponsiveComponents();
            Resize += (_, _) => LayoutResponsiveComponents();
        }

        private void SetupForm()
        {
            Text = $"Deal #{_deal.DealId} - Real Estate Contract & Terms";
            Size = new Size(820, 720);
            MinimumSize = new Size(640, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(244, 247, 251);
            DoubleBuffered = true;
        }

        private void BuildHeader(string buyer, string prop)
        {
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                BackColor = Color.White,
                Padding = new Padding(24, 16, 24, 16)
            };

            _pnlHeader.Paint += (s, e) =>
            {
                using var pen = new Pen(UiDetailCardHelper.BorderColor, 1f);
                e.Graphics.DrawLine(pen, 0, _pnlHeader.Height - 1, _pnlHeader.Width, _pnlHeader.Height - 1);
            };

            var pnlIcon = new Panel
            {
                Size = new Size(48, 48),
                Location = new Point(24, 17),
                BackColor = Color.FromArgb(239, 246, 255)
            };
            UiRadiusHelper.ApplyRoundedCorners(pnlIcon, 24);

            var lblIcon = new Label
            {
                Text = "📜",
                Font = new Font("Segoe UI Emoji", 18f),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            pnlIcon.Controls.Add(lblIcon);
            _pnlHeader.Controls.Add(pnlIcon);

            var lblTitle = new Label
            {
                Text = "Real Estate Contract & Terms Agreement",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(84, 17),
                AutoSize = true
            };
            _pnlHeader.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = $"Deal #{_deal.DealId}   •   Buyer: {buyer}   •   Property: {prop}",
                Font = new Font("Segoe UI", 9f),
                ForeColor = UiDetailCardHelper.LabelMutedColor,
                Location = new Point(84, 45),
                AutoSize = true
            };
            _pnlHeader.Controls.Add(lblSubtitle);

            Controls.Add(_pnlHeader);
        }

        private void BuildFooter()
        {
            _pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 62,
                BackColor = Color.White,
                Padding = new Padding(24, 12, 24, 12)
            };

            _pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(UiDetailCardHelper.BorderColor, 1f);
                e.Graphics.DrawLine(pen, 0, 0, _pnlFooter.Width, 0);
            };

            // Copy to Clipboard Button
            _btnCopy = new Button
            {
                Text = "📋 Copy to Clipboard",
                Size = new Size(160, 36),
                BackColor = Color.White,
                ForeColor = Theme.Primary,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnCopy, 8);
            _btnCopy.Paint += (s, e) =>
            {
                using var p = new Pen(UiDetailCardHelper.BorderColor, 1f);
                using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, _btnCopy.Width - 1, _btnCopy.Height - 1), 8);
                e.Graphics.DrawPath(p, path);
            };
            UiRadiusHelper.AttachHoverFeedback(_btnCopy, Color.White, Color.FromArgb(241, 245, 249));
            _btnCopy.Click += (_, _) =>
            {
                Clipboard.SetText(_documentText);
                MessageBox.Show("Contract & Terms copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            _pnlFooter.Controls.Add(_btnCopy);

            // Export to File Button
            _btnExport = new Button
            {
                Text = "💾 Export to File",
                Size = new Size(140, 36),
                BackColor = Color.White,
                ForeColor = Theme.Primary,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnExport, 8);
            _btnExport.Paint += (s, e) =>
            {
                using var p = new Pen(UiDetailCardHelper.BorderColor, 1f);
                using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, _btnExport.Width - 1, _btnExport.Height - 1), 8);
                e.Graphics.DrawPath(p, path);
            };
            UiRadiusHelper.AttachHoverFeedback(_btnExport, Color.White, Color.FromArgb(241, 245, 249));
            _btnExport.Click += (_, _) => ExportToFile();
            _pnlFooter.Controls.Add(_btnExport);

            // Close Button
            _btnClose = new Button
            {
                Text = "Close",
                Size = new Size(88, 36),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                DialogResult = DialogResult.OK,
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnClose, 8);
            UiRadiusHelper.AttachHoverFeedback(_btnClose, Color.FromArgb(241, 245, 249), Color.FromArgb(226, 232, 240));
            _pnlFooter.Controls.Add(_btnClose);

            AcceptButton = _btnClose;
            CancelButton = _btnClose;
            Controls.Add(_pnlFooter);
        }

        private void BuildContent()
        {
            var pnlWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(244, 247, 251),
                Padding = new Padding(24, 18, 24, 18)
            };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(UiDetailCardHelper.BorderColor, 1f);
                using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
                e.Graphics.DrawPath(pen, path);
            };
            UiRadiusHelper.ApplyRoundedCorners(card, 8);

            _txtContent = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Consolas", 9.75f, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                Text = _documentText
            };

            card.Controls.Add(_txtContent);
            pnlWrapper.Controls.Add(card);
            Controls.Add(pnlWrapper);
            pnlWrapper.BringToFront();
        }

        private void LayoutResponsiveComponents()
        {
            if (_pnlFooter != null)
            {
                // Left-aligned utility actions
                _btnCopy.Location = new Point(24, 13);
                _btnExport.Location = new Point(_btnCopy.Right + 10, 13);

                // Right-aligned dismiss action
                int right = _pnlFooter.ClientSize.Width - 24;
                _btnClose.Location = new Point(right - _btnClose.Width, 13);
            }
        }

        private void ExportToFile()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Text Document (*.txt)|*.txt",
                FileName = $"TermSheet_Deal_{_deal.DealId}_{DateTime.Now:yyyyMMdd}.txt",
                Title = "Save Real Estate Contract & Term Sheet"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, _documentText);
                MessageBox.Show(
                    $"Contract & Term Sheet saved successfully to:\n{sfd.FileName}",
                    "File Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
    }
}

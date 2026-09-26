using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Controls
{
    public class PaginationControl : UserControl
    {
        public event EventHandler<int>? PageChanged;
        public event EventHandler<int>? PageSizeChanged;

        private int _totalRecords = 0;
        private int _currentPage = 1;
        private int _pageSize = 25;
        private int _totalPages = 1;
        private string _itemLabel = "records";

        private readonly Label _lblInfo;
        private readonly Label _lblPageSize;
        private readonly ComboBox _cboPageSize;
        private readonly Button _btnFirst;
        private readonly Button _btnPrev;
        private readonly Label _lblPageIndicator;
        private readonly Button _btnNext;
        private readonly Button _btnLast;

        public int CurrentPage => _currentPage;
        public int PageSize => _pageSize;
        public int TotalRecords => _totalRecords;
        public int TotalPages => _totalPages;

        public PaginationControl()
        {
            this.Height = 44;
            this.Dock = DockStyle.Bottom;
            this.BackColor = Color.White;
            this.DoubleBuffered = true;

            _lblInfo = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(16, 13),
                Text = "Showing 0 records"
            };

            _lblPageSize = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Text = "Per page:"
            };

            _cboPageSize = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5f),
                Size = new Size(58, 24),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            _cboPageSize.Items.AddRange(new object[] { "15", "25", "50", "100" });
            _cboPageSize.SelectedItem = "25";

            _btnFirst = CreateNavButton("|◀", "First page");
            _btnPrev = CreateNavButton("◀", "Previous page");
            _btnNext = CreateNavButton("▶", "Next page");
            _btnLast = CreateNavButton("▶|", "Last page");

            _lblPageIndicator = new Label
            {
                AutoSize = false,
                Size = new Size(100, 26),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Page 1 of 1"
            };

            _btnFirst.Click += (_, _) => GoToPage(1);
            _btnPrev.Click += (_, _) => GoToPage(_currentPage - 1);
            _btnNext.Click += (_, _) => GoToPage(_currentPage + 1);
            _btnLast.Click += (_, _) => GoToPage(_totalPages);

            _cboPageSize.SelectedIndexChanged += (_, _) =>
            {
                if (int.TryParse(_cboPageSize.SelectedItem?.ToString(), out int newSize) && newSize != _pageSize)
                {
                    _pageSize = newSize;
                    _currentPage = 1;
                    PageSizeChanged?.Invoke(this, _pageSize);
                }
            };

            Controls.Add(_lblInfo);
            Controls.Add(_lblPageSize);
            Controls.Add(_cboPageSize);
            Controls.Add(_btnFirst);
            Controls.Add(_btnPrev);
            Controls.Add(_lblPageIndicator);
            Controls.Add(_btnNext);
            Controls.Add(_btnLast);

            this.Resize += (_, _) => LayoutControls();
            LayoutControls();
        }

        private Button CreateNavButton(string text, string toolTip)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Size = new Size(32, 28),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Padding = new Padding(0)
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            UiRadiusHelper.StyleButton(btn, 6);
            return btn;
        }

        public void SetItemLabel(string label)
        {
            _itemLabel = label;
            UpdateDisplay();
        }

        public void ResetPage()
        {
            _currentPage = 1;
        }

        public void UpdatePagination(int totalRecords, int currentPage, int pageSize)
        {
            _totalRecords = totalRecords;
            _pageSize = pageSize;
            _totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));

            _currentPage = Math.Max(1, Math.Min(currentPage, _totalPages));

            string sizeStr = pageSize.ToString();
            if (_cboPageSize.SelectedItem?.ToString() != sizeStr && _cboPageSize.Items.Contains(sizeStr))
            {
                _cboPageSize.SelectedItem = sizeStr;
            }

            UpdateDisplay();
        }

        private void GoToPage(int page)
        {
            int target = Math.Max(1, Math.Min(page, _totalPages));
            if (target != _currentPage)
            {
                _currentPage = target;
                UpdateDisplay();
                PageChanged?.Invoke(this, _currentPage);
            }
        }

        private void UpdateDisplay()
        {
            if (_totalRecords == 0)
            {
                _lblInfo.Text = $"Showing 0 {_itemLabel}";
            }
            else
            {
                int start = (_currentPage - 1) * _pageSize + 1;
                int end = Math.Min(_currentPage * _pageSize, _totalRecords);
                _lblInfo.Text = $"Showing {start:N0}–{end:N0} of {_totalRecords:N0} {_itemLabel}";
            }

            _lblPageIndicator.Text = $"Page {_currentPage} of {_totalPages}";

            bool hasPrev = _currentPage > 1;
            bool hasNext = _currentPage < _totalPages;

            _btnFirst.Enabled = hasPrev;
            _btnPrev.Enabled = hasPrev;
            _btnNext.Enabled = hasNext;
            _btnLast.Enabled = hasNext;

            _btnFirst.ForeColor = hasPrev ? Color.FromArgb(71, 85, 105) : Color.FromArgb(203, 213, 225);
            _btnPrev.ForeColor = hasPrev ? Color.FromArgb(71, 85, 105) : Color.FromArgb(203, 213, 225);
            _btnNext.ForeColor = hasNext ? Color.FromArgb(71, 85, 105) : Color.FromArgb(203, 213, 225);
            _btnLast.ForeColor = hasNext ? Color.FromArgb(71, 85, 105) : Color.FromArgb(203, 213, 225);

            LayoutControls();
        }

        private void LayoutControls()
        {
            int y = (this.Height - 28) / 2;
            _lblInfo.Location = new Point(16, (this.Height - _lblInfo.Height) / 2);

            int rightX = this.ClientSize.Width - 16;

            rightX -= _btnLast.Width;
            _btnLast.Location = new Point(rightX, y);

            rightX -= _btnNext.Width + 4;
            _btnNext.Location = new Point(rightX, y);

            rightX -= _lblPageIndicator.Width + 4;
            _lblPageIndicator.Location = new Point(rightX, (this.Height - _lblPageIndicator.Height) / 2);

            rightX -= _btnPrev.Width + 4;
            _btnPrev.Location = new Point(rightX, y);

            rightX -= _btnFirst.Width + 4;
            _btnFirst.Location = new Point(rightX, y);

            rightX -= _cboPageSize.Width + 16;
            _cboPageSize.Location = new Point(rightX, (this.Height - _cboPageSize.Height) / 2);

            rightX -= _lblPageSize.Width + 6;
            _lblPageSize.Location = new Point(rightX, (this.Height - _lblPageSize.Height) / 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.FromArgb(241, 245, 249), 1f);
            e.Graphics.DrawLine(pen, 0, 0, this.Width, 0);
        }
    }
}

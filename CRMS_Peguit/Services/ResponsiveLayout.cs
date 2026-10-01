using System.Runtime.CompilerServices;

namespace CRMS_Peguit.winforms.Services;

/// <summary>Shared, wrapping layouts. Control widths are measured after WinForms DPI scaling.</summary>
public static class ResponsiveLayout
{
    private sealed class LayoutState { public bool Busy; public bool EventsBound; }
    private static readonly ConditionalWeakTable<Control, LayoutState> States = new();

    public static IReadOnlyList<Rectangle> Wrap(int width, IEnumerable<Size> sizes, int gap = 8)
    {
        width = Math.Max(1, width);
        int x = 0, y = 0, rowHeight = 0;
        var result = new List<Rectangle>();
        foreach (var size in sizes)
        {
            int itemWidth = Math.Clamp(size.Width, 1, width);
            if (x > 0 && x + itemWidth > width)
            {
                x = 0;
                y += rowHeight + gap;
                rowHeight = 0;
            }
            result.Add(new Rectangle(x, y, itemWidth, Math.Max(1, size.Height)));
            x += itemWidth + gap;
            rowHeight = Math.Max(rowHeight, size.Height);
        }
        return result;
    }

    public static int Flow(Control parent, IEnumerable<Control?> controls, int top, int left = 0, int right = 0)
    {
        var visible = controls.Where(c => c != null && c.Visible).Cast<Control>().ToArray();
        int gap = Scale(parent, 8);
        var bounds = Wrap(parent.ClientSize.Width - left - right, visible.Select(c => c.Size), gap);
        for (int i = 0; i < visible.Length; i++)
        {
            visible[i].Anchor = AnchorStyles.Top | AnchorStyles.Left;
            visible[i].Bounds = new Rectangle(bounds[i].X + left, bounds[i].Y + top, bounds[i].Width, bounds[i].Height);
        }
        return bounds.Count == 0 ? top : top + bounds.Max(r => r.Bottom);
    }

    public static int Scale(Control control, int value) => (int)Math.Round(value * control.DeviceDpi / 96d);

    public static void BindToolbar(Panel panel, int inset, params Control[] controls)
    {
        var sizes = controls.ToDictionary(c => c, c => c.Size);
        bool busy = false;
        void Layout()
        {
            if (busy || panel.IsDisposed || panel.ClientSize.Width <= 0) return;
            busy = true;
            try
            {
                int margin = Scale(panel, inset), gap = Scale(panel, 8);
                var visible = controls.Where(c => c.Visible).ToArray();
                int width = Math.Max(1, panel.ClientSize.Width - margin * 2);
                var bounds = Wrap(width, visible.Select(c => c is Label
                    ? TextRenderer.MeasureText(c.Text, c.Font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl)
                    : c is Button ? new Size(Math.Max(sizes[c].Width, TextRenderer.MeasureText(c.Text, c.Font).Width + Scale(panel, 24)),
                        Math.Max(sizes[c].Height, c.Font.Height + Scale(panel, 12))) : sizes[c]), gap);
                panel.Height = margin * 2 + (bounds.Count == 0 ? 0 : bounds.Max(r => r.Bottom));
                for (int i = 0; i < visible.Length; i++)
                {
                    visible[i].Dock = DockStyle.None;
                    visible[i].Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    if (visible[i] is Label label)
                    {
                        label.AutoSize = false;
                        label.MaximumSize = Size.Empty;
                    }
                    visible[i].Bounds = new Rectangle(bounds[i].X + margin, bounds[i].Y + margin, bounds[i].Width, bounds[i].Height);
                }
            }
            finally { busy = false; }
        }
        panel.SizeChanged += (_, _) => Layout();
        panel.VisibleChanged += (_, _) => Layout();
        foreach (var control in controls) control.VisibleChanged += (_, _) => Layout();
        foreach (var label in controls.OfType<Label>()) label.TextChanged += (_, _) => Layout();
        foreach (var button in controls.OfType<Button>()) button.TextChanged += (_, _) => Layout();
        Layout();
    }

    public static void BindHeader(Panel panel, Label title, Label? subtitle, params Control[] actions)
    {
        var sizes = actions.ToDictionary(c => c, c => c.Size);
        bool busy = false;
        void Layout()
        {
            if (busy || panel.IsDisposed || panel.ClientSize.Width <= 0) return;
            busy = true;
            try
            {
                int margin = Scale(panel, 24), gap = Scale(panel, 12);
                int width = Math.Max(1, panel.ClientSize.Width - margin * 2);
                var visible = actions.Where(c => c.Visible).ToArray();
                foreach (var action in visible) action.Size = sizes[action];
                int actionWidth = visible.Sum(c => c.Width + gap);
                bool inline = width - actionWidth >= Scale(panel, 360);
                int labelWidth = inline ? width - actionWidth : width;
                int bottom = LabelBlock(title, margin, Scale(panel, 16), labelWidth);
                if (subtitle != null) bottom = LabelBlock(subtitle, margin, bottom + Scale(panel, 4), labelWidth);
                int actionTop = inline ? Scale(panel, 20) : bottom + gap;
                var bounds = Wrap(inline ? actionWidth : width, visible.Select(c => c.Size), gap);
                int required = bounds.Count == 0 ? bottom : Math.Max(bottom, actionTop + bounds.Max(r => r.Bottom));
                panel.Height = required + Scale(panel, 16);
                int left = inline ? panel.ClientSize.Width - margin - actionWidth : margin;
                for (int i = 0; i < visible.Length; i++)
                {
                    visible[i].Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    visible[i].Bounds = new Rectangle(left + bounds[i].X, actionTop + bounds[i].Y, bounds[i].Width, bounds[i].Height);
                }
            }
            finally { busy = false; }
        }
        panel.SizeChanged += (_, _) => Layout();
        panel.VisibleChanged += (_, _) => Layout();
        title.TextChanged += (_, _) => Layout();
        if (subtitle != null) subtitle.TextChanged += (_, _) => Layout();
        foreach (var action in actions) action.VisibleChanged += (_, _) => Layout();
        Layout();
    }

    public static int LabelBlock(Label label, int left, int top, int width)
    {
        label.AutoSize = false;
        label.AutoEllipsis = false;
        label.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        var size = TextRenderer.MeasureText(label.Text, label.Font, new Size(Math.Max(1, width), int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        label.Bounds = new Rectangle(left, top, Math.Max(1, width), Math.Max(label.Font.Height + 4, size.Height));
        return label.Bottom;
    }

    public static Panel FieldGroup(Label label, Control field, int? preferredWidth = null)
    {
        var group = new Panel { Width = preferredWidth ?? Math.Max(field.Width, 180), Height = label.Font.Height + field.Height + 12 };
        int fieldHeight = field.Height;
        group.Controls.Add(label);
        group.Controls.Add(field);
        void LayoutField()
        {
            int bottom = LabelBlock(label, 0, 0, group.ClientSize.Width);
            field.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            field.Bounds = new Rectangle(0, bottom + 4, Math.Max(1, group.ClientSize.Width), fieldHeight);
            group.Height = field.Bottom;
        }
        group.SizeChanged += (_, _) => LayoutField();
        label.TextChanged += (_, _) => LayoutField();
        LayoutField();
        return group;
    }

    public static void FormCard(Panel card)
    {
        var children = card.Controls.Cast<Control>().OrderBy(c => c.Top).ThenBy(c => c.Left).ToArray();
        var title = children.OfType<Label>().First();
        var labels = children.OfType<Label>().Where(l => l != title).ToList();
        var items = new List<(Control Control, int Width, bool Full)>();
        foreach (var field in children.Where(c => c is not Label))
        {
            var label = labels.LastOrDefault(l => l.Left == field.Left && l.Top < field.Top && field.Top - l.Top <= 32);
            int width = field.Width;
            bool full = width >= 500;
            Control item = field;
            if (label != null)
            {
                labels.Remove(label);
                item = FieldGroup(label, field, Math.Max(width, 180));
                card.Controls.Add(item);
            }
            items.Add((item, Math.Max(width, full ? 1 : 180), full));
        }
        bool busy = false;
        void LayoutCard()
        {
            if (busy || card.IsDisposed) return;
            busy = true;
            try
            {
                int margin = Scale(card, 16), gap = Scale(card, 12);
                int width = Math.Max(1, card.ClientSize.Width - margin * 2);
                int y = LabelBlock(title, margin, margin, width) + gap;
                foreach (var item in items) item.Control.Width = item.Full ? width : Math.Min(width, item.Width);
                // Recalculate field-group heights before wrapping, because labels can span lines.
                var bounds = Wrap(width, items.Select(i => i.Control.Size), gap);
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].Control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    items[i].Control.Location = new Point(margin + bounds[i].X, y + bounds[i].Y);
                }
                card.Height = y + (bounds.Count == 0 ? 0 : bounds.Max(r => r.Bottom)) + margin;
            }
            finally { busy = false; }
        }
        card.SizeChanged += (_, _) => LayoutCard();
        card.VisibleChanged += (_, _) => LayoutCard();
        LayoutCard();
    }

    public static void BindInputPanel(Panel panel)
    {
        var children = panel.Controls.Cast<Control>().OrderBy(c => c.Top).ThenBy(c => c.Left).ToArray();
        var labels = children.OfType<Label>().ToList();
        var items = new List<(Control Control, int Width, int Order, int X)>();
        var grouped = new HashSet<Control>();
        foreach (var field in children.Where(c => c is TextBoxBase or ComboBox or NumericUpDown or DateTimePicker))
        {
            var label = labels.LastOrDefault(l => l.Left == field.Left && l.Top < field.Top && field.Top - l.Top <= 40);
            if (label == null) continue;
            labels.Remove(label);
            grouped.Add(label);
            grouped.Add(field);
            int originalTop = label.Top, originalLeft = label.Left;
            int width = Math.Max(180, field.Width);
            var group = FieldGroup(label, field, width);
            panel.Controls.Add(group);
            items.Add((group, width, originalTop, originalLeft));
        }
        foreach (var decoration in children.Where(c => !grouped.Contains(c)))
            items.Add((decoration, Math.Max(1, decoration.Width), decoration.Top, decoration.Left));
        items = items.OrderBy(i => i.Order).ThenBy(i => i.X).ToList();
        bool busy = false;
        void LayoutInputs()
        {
            if (busy || panel.IsDisposed) return;
            busy = true;
            var scroll = panel.AutoScrollPosition;
            try
            {
                panel.AutoScrollPosition = Point.Empty;
                int margin = Scale(panel, 20);
                int width = Math.Max(1, panel.ClientSize.Width - margin * 2 - SystemInformation.VerticalScrollBarWidth);
                foreach (var item in items)
                {
                    if (item.Control is Label label)
                    {
                        label.MaximumSize = Size.Empty;
                        LabelBlock(label, 0, 0, width);
                    }
                    else item.Control.Width = Math.Min(width, item.Width);
                }
                int bottom = Flow(panel, items.Select(i => i.Control), margin, margin, panel.ClientSize.Width - margin - width);
                panel.AutoScroll = true;
                panel.AutoScrollMinSize = new Size(0, bottom + margin);
                panel.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
            }
            finally { busy = false; }
        }
        panel.SizeChanged += (_, _) => LayoutInputs();
        panel.VisibleChanged += (_, _) => LayoutInputs();
        foreach (var item in items)
        {
            item.Control.VisibleChanged += (_, _) => LayoutInputs();
            if (item.Control is Label) item.Control.TextChanged += (_, _) => LayoutInputs();
        }
        LayoutInputs();
    }

    public static void BindKpis(TableLayoutPanel grid, Panel wrapper)
    {
        bool busy = false;
        void Layout()
        {
            if (busy || wrapper.IsDisposed) return;
            busy = true;
            try
            {
                KpiGrid(grid, Math.Max(1, wrapper.ClientSize.Width - wrapper.Padding.Horizontal));
                wrapper.Height = grid.Height + wrapper.Padding.Vertical;
            }
            finally { busy = false; }
        }
        wrapper.SizeChanged += (_, _) => Layout();
        wrapper.VisibleChanged += (_, _) => Layout();
        Layout();
    }

    public static void ListPage(ScrollableControl host, Label title, Label subtitle, TableLayoutPanel? kpis,
        Control search, Control[] filters, Control?[] actions, Panel table)
    {
        var state = States.GetOrCreateValue(host);
        if (state.Busy || host.IsDisposed || host.ClientSize.Width <= 0) return;
        if (!state.EventsBound)
        {
            state.EventsBound = true;
            title.TextChanged += (_, _) => ListPage(host, title, subtitle, kpis, search, filters, actions, table);
            subtitle.TextChanged += (_, _) => ListPage(host, title, subtitle, kpis, search, filters, actions, table);
            foreach (var action in actions.Where(a => a != null))
                action!.VisibleChanged += (_, _) => ListPage(host, title, subtitle, kpis, search, filters, actions, table);
        }
        state.Busy = true;
        var scrollPosition = host.AutoScrollPosition;
        host.SuspendLayout();
        try
        {
            host.AutoScrollPosition = Point.Empty;
            title.Font = UiStyleConstants.PageTitleFont;
            subtitle.Font = UiStyleConstants.SubtitleFont;
            int margin = Scale(host, 24), gap = Scale(host, 12);
            int width = Math.Max(1, host.ClientSize.Width - margin * 2 - SystemInformation.VerticalScrollBarWidth);
            int y = LabelBlock(title, margin, Scale(host, 20), width);
            y = LabelBlock(subtitle, margin, y + Scale(host, 4), width) + gap;
            if (kpis != null)
            {
                KpiGrid(kpis, width);
                kpis.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                kpis.Location = new Point(margin, y);
                y = kpis.Bottom + gap;
            }
            search.Width = Math.Min(Scale(host, 320), width);
            var controls = new Control?[] { search }.Concat(filters).Concat(actions)
                .Where(c => c != null && c.Visible).Cast<Control>().ToArray();
            foreach (var control in controls)
            {
                // TextBox height is font-driven; buttons and pickers retain usable touch targets.
                if (control is Button) control.Height = Math.Max(Scale(host, 36), control.Font.Height + Scale(host, 12));
            }
            int bottom = Flow(host, controls, y, margin, host.ClientSize.Width - margin - width);
            table.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            table.Bounds = new Rectangle(margin, bottom + gap, width,
                Math.Max(Scale(host, 180), host.ClientSize.Height - bottom - gap - margin));
            host.AutoScroll = true;
            host.AutoScrollMinSize = new Size(0, table.Bottom + margin);
            host.AutoScrollPosition = new Point(-scrollPosition.X, -scrollPosition.Y);
        }
        finally
        {
            host.ResumeLayout();
            state.Busy = false;
        }
    }

    public static void KpiGrid(TableLayoutPanel grid, int width)
    {
        var cards = grid.Controls.Cast<Control>().OrderBy(c => grid.GetRow(c)).ThenBy(c => grid.GetColumn(c)).ToArray();
        if (cards.Length == 0) return;
        int columns = Math.Max(1, Math.Min(cards.Length, width / Scale(grid, 220)));
        int rows = (cards.Length + columns - 1) / columns;
        grid.SuspendLayout();
        try
        {
            if (grid.ColumnCount != columns || grid.RowCount != rows)
            {
                grid.Controls.Clear();
                grid.ColumnStyles.Clear();
                grid.RowStyles.Clear();
                grid.ColumnCount = columns;
                grid.RowCount = rows;
                for (int c = 0; c < columns; c++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
                for (int r = 0; r < rows; r++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));
                for (int i = 0; i < cards.Length; i++) grid.Controls.Add(cards[i], i % columns, i / columns);
            }
            grid.Size = new Size(Math.Max(1, width), rows * Scale(grid, 112));
            foreach (var card in cards) card.Dock = DockStyle.Fill;
        }
        finally { grid.ResumeLayout(); }
    }
}

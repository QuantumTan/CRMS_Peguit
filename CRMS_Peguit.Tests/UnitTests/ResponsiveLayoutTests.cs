using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.Tests.UnitTests;

public class ResponsiveLayoutTests
{
    [Theory]
    [InlineData(320, 1)]
    [InlineData(700, 1)]
    [InlineData(940, 1)]
    [InlineData(1440, 1)]
    [InlineData(700, 1.5)]
    [InlineData(940, 2)]
    public void ToolbarControlsNeverOverlapOrOverflow(int width, double scale)
    {
        var sizes = new[] { 320, 100, 120, 120, 120, 130, 150 }
            .Select(w => new Size((int)(w * scale), (int)(36 * scale)));
        var bounds = ResponsiveLayout.Wrap(width, sizes, (int)(8 * scale));
        foreach (var rectangle in bounds)
        {
            Assert.True(rectangle.Left >= 0);
            Assert.True(rectangle.Right <= width);
            Assert.True(rectangle.Height > 0);
        }
        for (int i = 0; i < bounds.Count; i++)
            for (int j = i + 1; j < bounds.Count; j++)
                Assert.False(bounds[i].IntersectsWith(bounds[j]));
    }

    [Fact]
    public void WrappingHonorsTheTallestControlInEachRow()
    {
        var bounds = ResponsiveLayout.Wrap(400, new[] { new Size(180, 70), new Size(180, 36), new Size(300, 36) });
        Assert.Equal(78, bounds[2].Top);
        Assert.Equal(300, bounds[2].Width);
    }

    [Fact]
    public void NarrowKpiGridPreservesOrderAcrossRepeatedResizes()
    {
        RunSta(() =>
        {
            using var grid = new TableLayoutPanel { ColumnCount = 5, RowCount = 1 };
            var cards = Enumerable.Range(0, 5).Select(i => new Panel { Name = i.ToString() }).ToArray();
            for (int i = 0; i < cards.Length; i++) grid.Controls.Add(cards[i], i, 0);
            foreach (int width in new[] { 420, 1200, 700, 1200 })
            {
                ResponsiveLayout.KpiGrid(grid, width);
                Assert.Equal(5, grid.Controls.Count);
                var ordered = grid.Controls.Cast<Control>().OrderBy(c => grid.GetRow(c)).ThenBy(c => grid.GetColumn(c));
                Assert.Equal(cards, ordered);
            }
        });
    }

    [Fact]
    public void ListPagesKeepFiltersAndTablesBelowLongHeadings()
    {
        RunSta(() =>
        {
            using var host = new UserControl { Size = new Size(700, 650) };
            var title = new Label { Text = "Customer and transaction administration", Font = new Font("Segoe UI", 20) };
            var subtitle = new Label { Text = new string('W', 140) };
            var search = new TextBox { Width = 320 };
            var filters = Enumerable.Range(0, 6).Select(i => new Button { Width = 120 }).ToArray();
            var actions = new[] { new Button { Width = 150 }, new Button { Width = 130 } };
            var table = new Panel();
            host.Controls.AddRange(new Control[] { title, subtitle, search, table }.Concat(filters).Concat(actions).ToArray());
            foreach (int width in new[] { 700, 500, 1200, 700 })
            {
                host.Width = width;
                ResponsiveLayout.ListPage(host, title, subtitle, null, search, filters, actions, table);
                var controls = new Control[] { title, subtitle, search, table }.Concat(filters).Concat(actions).ToArray();
                for (int i = 0; i < controls.Length; i++)
                    for (int j = i + 1; j < controls.Length; j++)
                        Assert.False(controls[i].Bounds.IntersectsWith(controls[j].Bounds), $"{i} overlaps {j} at width {width}");
                Assert.True(table.Right <= host.ClientSize.Width);
            }
        });
    }

    [Fact]
    public void FormCardsReflowFieldsWithoutClipping()
    {
        RunSta(() =>
        {
            using var card = new Panel { Width = 686 };
            card.Controls.Add(new Label { Text = "Transaction details", Location = new Point(14, 12), AutoSize = true });
            foreach (int x in new[] { 16, 350 })
            {
                card.Controls.Add(new Label { Text = "Required customer or subject property", Location = new Point(x, 44), AutoSize = true });
                card.Controls.Add(new ComboBox { Location = new Point(x, 66), Width = 320 });
            }
            ResponsiveLayout.FormCard(card);
            foreach (int width in new[] { 686, 450, 800 })
            {
                card.Width = width;
                var children = card.Controls.Cast<Control>().ToArray();
                foreach (var child in children) Assert.True(child.Right <= card.Width && child.Bottom <= card.Height);
                for (int i = 0; i < children.Length; i++)
                    for (int j = i + 1; j < children.Length; j++) Assert.False(children[i].Bounds.IntersectsWith(children[j].Bounds));
            }
        });
    }

    [Fact]
    public void AdminHeadersWrapTitlesAndActionsWithoutCollisions()
    {
        RunSta(() =>
        {
            using var header = new Panel { Width = 900 };
            var title = new Label { Text = "Multi-Tenant Sync Health Monitoring", Font = new Font("Segoe UI", 20) };
            var subtitle = new Label { Text = "Queue health, pending operations, and sync statuses across tenant databases." };
            var search = new TextBox { Width = 260 };
            var button = new Button { Width = 140, Height = 36 };
            header.Controls.AddRange(new Control[] { title, subtitle, search, button });
            ResponsiveLayout.BindHeader(header, title, subtitle, search, button);
            foreach (int width in new[] { 900, 500, 1200, 700 })
            {
                header.Width = width;
                AssertContainedAndDisjoint(header);
            }
        });
    }

    [Fact]
    public void DynamicToolbarCaptionsDoNotOverlapActions()
    {
        RunSta(() =>
        {
            using var toolbar = new Panel { Width = 700 };
            var label = new Label { Text = "0 records", AutoSize = true };
            var search = new TextBox { Width = 320 };
            var save = new Button { Width = 140, Height = 36 };
            toolbar.Controls.AddRange(new Control[] { search, label, save });
            ResponsiveLayout.BindToolbar(toolbar, 12, search, label, save);
            label.Text = "Last backup completed successfully with 123,456 synchronized records.";
            AssertContainedAndDisjoint(toolbar);
            toolbar.Width = 450;
            AssertContainedAndDisjoint(toolbar);
        });
    }

    [Fact]
    public void InputPanelsKeepLabelsWithTheirFieldsAndAllowScrolling()
    {
        RunSta(() =>
        {
            using var content = new Panel { Size = new Size(520, 120) };
            foreach (int x in new[] { 20, 260 })
            {
                content.Controls.Add(new Label { Text = "First or last name", AutoSize = true, Location = new Point(x, 20) });
                content.Controls.Add(new TextBox { Width = 220, Location = new Point(x, 45) });
            }
            ResponsiveLayout.BindInputPanel(content);
            foreach (int width in new[] { 520, 380, 700 })
            {
                content.Width = width;
                var groups = content.Controls.Cast<Control>().ToArray();
                Assert.Equal(2, groups.Length);
                Assert.False(groups[0].Bounds.IntersectsWith(groups[1].Bounds));
                foreach (Control group in groups)
                {
                    Assert.True(group.Right <= content.ClientSize.Width);
                    Assert.Equal(2, group.Controls.Count);
                }
                Assert.True(content.AutoScrollMinSize.Height >= groups.Max(g => g.Bottom));
            }
        });
    }

    [Fact]
    public void DetailRowsWrapLongValuesAndStackNarrowColumns()
    {
        RunSta(() =>
        {
            using var row = CRMS_Peguit.winforms.Models.Services.UiDetailCardHelper.CreateKeyValueRow(
                "Email address", "very.long.customer.address@example-company.com", "Subject property", new string('W', 70));
            foreach (int width in new[] { 650, 380, 800 })
            {
                row.Width = width;
                AssertContainedAndDisjoint(row);
            }
        });
    }

    [Fact]
    public void DatabaseFreeDialogsKeepRealFieldsAccessibleAtNarrowWidths()
    {
        RunSta(() =>
        {
            using var customer = new CRMS_Peguit.winforms.Views.Customers.CustomerInputForm();
            using var property = new CRMS_Peguit.winforms.Views.Properties.PropertyInputForm();
            using var tenant = new CRMS_Peguit.winforms.Views.SuperAdmin.CreateTenantDialog();
            using var admin = new CRMS_Peguit.winforms.Views.SuperAdmin.CreateAdminDialog(new());
            foreach (var dialog in new Form[] { customer, property, tenant, admin })
            {
                var content = dialog.Controls.OfType<Panel>().Single(p => p.Dock == DockStyle.Fill);
                dialog.Controls.Remove(content); // Exercise the actual UI tree without showing a window or starting application services.
                using (content)
                {
                    content.Dock = DockStyle.None;
                    content.Visible = true;
                    foreach (int width in new[] { 480, 700 })
                    {
                        content.Size = new Size(width, 600);
                        content.Height = Math.Max(600, content.AutoScrollMinSize.Height);
                        content.PerformLayout();
                        AssertContainedAndDisjoint(content);
                        foreach (var group in content.Controls.OfType<Panel>().Where(p => p.Controls.OfType<TextBoxBase>().Any() || p.Controls.OfType<ComboBox>().Any()))
                            AssertContainedAndDisjoint(group);
                        string? directory = Environment.GetEnvironmentVariable("CRMS_UI_PREVIEW_DIRECTORY");
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            Directory.CreateDirectory(directory);
                            using var bitmap = new Bitmap(content.Width, content.Height);
                            content.DrawToBitmap(bitmap, content.ClientRectangle);
                            bitmap.Save(Path.Combine(directory, $"{dialog.GetType().Name}-{width}.png"), System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                }
            }
        });
    }

    [Fact]
    public void SuperAdminReportsAndSubscriptionsDoNotOverlapAtSupportedWidths()
    {
        RunSta(() =>
        {
            using var reports = new CRMS_Peguit.winforms.Views.SuperAdmin.AdminPanelMasterView(openReports: true, loadData: false);
            using var terms = new CRMS_Peguit.winforms.Views.SuperAdmin.AdminPanelMasterView(openTerms: true, loadData: false);
            using var subscriptions = new CRMS_Peguit.winforms.Views.SuperAdmin.SubscriptionsView(loadData: false);
            foreach (var view in new UserControl[] { reports, terms, subscriptions })
            {
                view.Dock = DockStyle.None;
                foreach (int width in new[] { 900, 1200, 700 })
                {
                    view.Size = new Size(width, 750);
                    view.PerformLayout();
                    AssertNoVisibleOverlap(view);
                }
            }
        });
    }

    [Fact]
    public void PaginationReservesSpaceBelowTheGridWhenItWraps()
    {
        RunSta(() =>
        {
            using var card = new Panel { Size = new Size(1000, 500) };
            var grid = new DataGridView { Dock = DockStyle.Fill };
            var pagination = new CRMS_Peguit.winforms.Controls.PaginationControl();
            card.Controls.Add(grid);
            card.Controls.Add(pagination);
            pagination.SendToBack();
            foreach (int width in new[] { 1000, 320, 700, 1000 })
            {
                card.Width = width;
                card.PerformLayout();
                Assert.False(grid.Bounds.IntersectsWith(pagination.Bounds));
                Assert.Equal(pagination.Top, grid.Bottom);
                AssertContainedAndDisjoint(pagination);
            }
        });
    }

    private static void AssertNoVisibleOverlap(Control parent)
    {
        var children = parent.Controls.Cast<Control>().Where(c => c.Visible).ToArray();
        for (int i = 0; i < children.Length; i++)
        {
            for (int j = i + 1; j < children.Length; j++)
                Assert.False(children[i].Bounds.IntersectsWith(children[j].Bounds),
                    $"{parent.GetType().Name}: {children[i].Text} {children[i].Bounds} overlaps {children[j].Text} {children[j].Bounds}");
            if (children[i] is Panel && children[i] is not TableLayoutPanel && children[i] is not FlowLayoutPanel)
                AssertNoVisibleOverlap(children[i]);
        }
    }

    private static void AssertContainedAndDisjoint(Control parent)
    {
        var children = parent.Controls.Cast<Control>().ToArray();
        foreach (var child in children)
            Assert.True(child.Left >= 0 && child.Top >= 0 && child.Right <= parent.Width && child.Bottom <= parent.Height,
                $"{child.Text} is outside {parent.Size}: {child.Bounds}");
        for (int i = 0; i < children.Length; i++)
            for (int j = i + 1; j < children.Length; j++)
                Assert.False(children[i].Bounds.IntersectsWith(children[j].Bounds), $"{children[i].Text} overlaps {children[j].Text}");
    }

    private static void RunSta(Action test)
    {
        Exception? error = null;
        var thread = new Thread(() => { try { test(); } catch (Exception ex) { error = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}

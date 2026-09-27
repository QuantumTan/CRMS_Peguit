using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.Models;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Roles;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Models.ViewModels;
using CRMS_Peguit.winforms.Services;
using CRMS_Peguit.winforms.Views.Customers;
using CRMS_Peguit.winforms.Views.FollowUps;
using CRMS_Peguit.winforms.Views.Leads;
using CRMS_Peguit.winforms.Views.Shared;
using Color = System.Drawing.Color;

namespace CRMS_Peguit.winforms.Views.Dashboard
{
    public partial class DashboardView : UserControl
    {
        public event Action<string>? NavigationRequested;

        private readonly Panel _spacerLeft = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill, Margin = Padding.Empty };
        private readonly Panel _spacerRight = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill, Margin = Padding.Empty };
        private string _chartNavigationTarget = "Analytics";

        private UserRole _currentRole = UserRole.SalesStaff;
        private Panel? _pnlAdminContainer;
        private Panel? _cardTeamRoster;
        private Panel? _pnlTeamRosterList;
        private Panel? _cardTicketBreakdown;
        private ScottPlot.WinForms.FormsPlot? _plotAdminTicketBreakdown;
        private Label? _lblAdminTicketBreakdownFooter;
        private Panel? _cardCommissionTrend;
        private ScottPlot.WinForms.FormsPlot? _plotAdminCommissionTrend;
        private Label? _lblAdminCommissionFooter;
        private Panel? _cardTicketsAttention;
        private Panel? _pnlTicketsAttentionList;
        private Panel? _cardRecentActivity;
        private Panel? _pnlRecentActivityList;
        private ListSkeletonOverlay? _leftListSkeleton;
        private ListSkeletonOverlay? _rightListSkeleton;
        private ChartSkeletonOverlay? _chartSkeleton;

        public DashboardView()
        {
            InitializeComponent();
            SetupStyling();
            LoadData();

            Load += (_, _) => LayoutControls();
            Resize += (_, _) => LayoutControls();
        }

        private void SetupStyling()
        {
            this.BackColor = Theme.Background;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblSubtitle.ForeColor = Theme.TextSecondary;
            lblLoading.Visible = false;

            UiRadiusHelper.StyleCard(pnlLeftCard, 12);
            UiRadiusHelper.StyleCard(pnlRightCard, 12);
            UiRadiusHelper.StyleCard(pnlChartCard, 12);

            lblLeftTitle.ForeColor = Theme.TextPrimary;
            lblLeftSubtitle.ForeColor = Theme.TextSecondary;
            lblLeftEmpty.ForeColor = Theme.TextSecondary;

            lblRightTitle.ForeColor = Theme.TextPrimary;
            lblRightSubtitle.ForeColor = Theme.TextSecondary;
            lblRightEmpty.ForeColor = Theme.TextSecondary;

            lblChartTitle.ForeColor = Theme.TextPrimary;
            lblChartSubtitle.ForeColor = Theme.TextSecondary;
            lblChartFooter.ForeColor = Theme.Primary;

            pnlLeftList.AutoScroll = true;
            pnlRightList.AutoScroll = true;

            _leftListSkeleton = ListSkeletonOverlay.CreateForContainer(pnlLeftList);
            _rightListSkeleton = ListSkeletonOverlay.CreateForContainer(pnlRightList);

            _chartSkeleton = new ChartSkeletonOverlay();
            _chartSkeleton.Location = plotGlanceable.Location;
            _chartSkeleton.Size = plotGlanceable.Size;
            _chartSkeleton.Anchor = plotGlanceable.Anchor;
            pnlChartCard.Controls.Add(_chartSkeleton);
            _chartSkeleton.BringToFront();

            pnlLeftList.Resize += (_, _) => ResizeListItems(pnlLeftList);
            pnlRightList.Resize += (_, _) => ResizeListItems(pnlRightList);

            BiDisplayConstants.ConfigureStandardPlot(plotGlanceable);
            plotGlanceable.Cursor = Cursors.Hand;
            plotGlanceable.Click += (_, _) => RequestNavigation(_chartNavigationTarget);

            lblChartFooter.Cursor = Cursors.Hand;
            lblChartFooter.Click += (_, _) => RequestNavigation(_chartNavigationTarget);
            pnlChartCard.Cursor = Cursors.Hand;
            pnlChartCard.Click += (_, _) => RequestNavigation(_chartNavigationTarget);
        }

        public async void LoadData()
        {
            try
            {
                lblLoading.Visible = false;

                kpi1.ShowLoadingSkeleton();
                kpi2.ShowLoadingSkeleton();
                kpi3.ShowLoadingSkeleton();
                kpi4.ShowLoadingSkeleton();
                _chartSkeleton?.ShowSkeleton(ChartSkeletonType.Bars);
                _leftListSkeleton?.ShowSkeleton(4);
                _rightListSkeleton?.ShowSkeleton(4);

                var user = CurrentSession.CurrentUser;
                var role = user?.Role ?? UserRole.SalesStaff;
                _currentRole = role;

                object? snapshot = null;
                await System.Threading.Tasks.Task.Run(() =>
                {
                    using var ctrl = new DashboardController();
                    snapshot = role switch
                    {
                        UserRole.SalesStaff => (object)ctrl.GetAgentSnapshot(CurrentSession.UserId),
                        UserRole.Manager => (object)ctrl.GetManagerSnapshot(),
                        UserRole.Admin => (object)ctrl.GetAdminSnapshot(),
                        UserRole.SuperAdmin => (object)ctrl.GetSuperAdminSnapshot(),
                        _ => (object)ctrl.GetAgentSnapshot(CurrentSession.UserId)
                    };
                });

                if (IsDisposed) return;

                switch (role)
                {
                    case UserRole.SalesStaff:
                        if (snapshot is AgentDashboardDto agentSnap) RenderAgentDashboard(agentSnap);
                        break;
                    case UserRole.Manager:
                        if (snapshot is ManagerDashboardDto mgrSnap) RenderManagerDashboard(mgrSnap);
                        break;
                    case UserRole.Admin:
                        if (snapshot is AdminDashboardDto adminSnap) RenderAdminDashboard(adminSnap);
                        break;
                    case UserRole.SuperAdmin:
                        if (snapshot is SuperAdminDashboardDto superSnap) RenderSuperAdminDashboard(superSnap);
                        break;
                    default:
                        if (snapshot is AgentDashboardDto defSnap) RenderAgentDashboard(defSnap);
                        break;
                }
                LayoutControls();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardView.LoadData] Error: {ex.Message}");
            }
            finally
            {
                kpi1.HideLoadingSkeleton();
                kpi2.HideLoadingSkeleton();
                kpi3.HideLoadingSkeleton();
                kpi4.HideLoadingSkeleton();
                _chartSkeleton?.HideSkeleton();
                _leftListSkeleton?.HideSkeleton();
                _rightListSkeleton?.HideSkeleton();
                lblLoading.Visible = false;
                lblSubtitle.Visible = true;
            }
        }

        // =========================================================================
        // 1. AGENT DASHBOARD RENDERER (Owner-scoped only)
        // =========================================================================
        private void RenderAgentDashboard(AgentDashboardDto snapshot)
        {
            lblTitle.Text = snapshot.Greeting;
            lblSubtitle.Text = snapshot.DateText;
            _chartNavigationTarget = "Analytics";
            ConfigureContentLayout(0);

            // Quick Actions: "+ New Lead", "+ New Customer", "+ Log Activity"
            pnlQuickActions.Controls.Clear();

            var btnAddLead = CreateQuickActionButton("+ New Lead", Theme.Primary, Color.White, (_, _) =>
            {
                using var form = new LeadInputForm();
                if (form.ShowDialog(this) == DialogResult.OK && form.Result != null)
                {
                    using var leadCtrl = new LeadController();
                    leadCtrl.Add(form.Result);
                    LoadData();
                }
            });

            var btnAddCustomer = CreateQuickActionButton("+ New Customer", Color.White, Theme.TextPrimary, (_, _) =>
            {
                using var form = new CustomerInputForm();
                if (form.ShowDialog(this) == DialogResult.OK && form.Result != null)
                {
                    using var custCtrl = new CustomerController();
                    custCtrl.Add(form.Result);
                    LoadData();
                }
            }, hasBorder: true);

            var btnLogActivity = CreateQuickActionButton("+ Log Activity", Color.White, Theme.TextPrimary, (_, _) =>
            {
                using var dlg = new LogActivityDialog();
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadData();
                }
            }, hasBorder: true);

            pnlQuickActions.Controls.Add(btnAddLead);
            pnlQuickActions.Controls.Add(btnAddCustomer);
            pnlQuickActions.Controls.Add(btnLogActivity);

            // 4 KPI Cards
            ConfigureKpiCard(kpi1, "MY ACTIVE LEADS", snapshot.ActiveLeadsCount, "Pipeline leads", BiDisplayConstants.PrimaryAccent, KpiIconType.Target, () => RequestNavigation("Leads:All"));
            ConfigureKpiCard(kpi2, "MY OPEN DEALS", snapshot.OpenDealsCount, "Active pipeline", BiDisplayConstants.HighlightAccent, KpiIconType.Briefcase, () => RequestNavigation("Deals:Offer"));
            if (CurrentSession.CanAccessActions)
            {
                ConfigureKpiCard(kpi3, "FOLLOW-UPS TODAY", snapshot.FollowUpsDueTodayCount, "Due & overdue", BiDisplayConstants.StatusPending, KpiIconType.Clock, () => RequestNavigation("FollowUps:Today"));
            }
            else
            {
                ConfigureKpiCard(kpi3, "PROPERTIES", snapshot.ActivePropertiesCount, "Active inventory", BiDisplayConstants.HighlightAccent, KpiIconType.Building, () => RequestNavigation("Properties"));
            }

            ConfigureKpiCard(kpi4, "MY OPEN TICKETS", snapshot.OpenSupportTicketsCount, "Awaiting triage", BiDisplayConstants.SkyAccent, KpiIconType.Ticket, () => RequestNavigation("SupportTickets:Open"));

            // Glanceable Sparkline (last 30 days)
            _chartNavigationTarget = "Analytics:Deals";
            RenderAgentSparkline(snapshot.SparklineDealsClosed);

            // Left Card: "Today's Follow-Ups" (gated to Tenant B and Tenant C with Actions access)
            if (CurrentSession.CanAccessActions)
            {
                pnlLeftCard.Visible = true;
                lblLeftTitle.Text = "Today's Follow-Ups";
                lblLeftSubtitle.Text = snapshot.FollowUpsToday.Count > 0 ? $"{snapshot.FollowUpsToday.Count} due today" : "Due today";
                pnlLeftList.Controls.Clear();

                if (snapshot.FollowUpsToday.Count == 0)
                {
                    lblLeftEmpty.Text = "✓  No follow-ups due today. You're all caught up!";
                    lblLeftEmpty.Visible = true;
                }
                else
                {
                    lblLeftEmpty.Visible = false;
                    int y = 0;
                    foreach (var item in snapshot.FollowUpsToday)
                    {
                        string sub = string.IsNullOrWhiteSpace(item.RelatedName) ? $"Priority: {item.Priority}" : $"{item.RelatedName} · {item.Priority}";

                        var row = CreateItemRow(
                            iconText: GetActivityIcon(item.Type),
                            title: item.Title,
                            subtitle: sub,
                            statusText: item.Status,
                            timeAgo: item.DueTimeText,
                            onClick: () =>
                            {
                                using var fuCtrl = new FollowUpController();
                                var reminder = fuCtrl.GetById(item.TaskReminderId);
                                if (reminder != null)
                                {
                                    using var dlg = new FollowUpInputForm(fuCtrl, reminder);
                                    if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
                                    {
                                        fuCtrl.Update(dlg.Result);
                                        LoadData();
                                    }
                                }
                            }
                        );

                        row.Location = new Point(0, y);
                        row.Width = Math.Max(200, pnlLeftList.ClientSize.Width - 4);
                        pnlLeftList.Controls.Add(row);
                        y += row.Height + 8;
                    }
                }
            }
            else
            {
                pnlLeftCard.Visible = false;
            }

            // Middle Card: "My Recent Activity" (last 5, AvatarLabel, StatusText, relative time)
            lblRightTitle.Text = "My Recent Activity";
            lblRightSubtitle.Text = "Last 5 activities logged (read-only)";
            pnlRightList.Controls.Clear();

            if (snapshot.RecentActivities.Count == 0)
            {
                lblRightEmpty.Text = "📋  No recent activities logged yet.";
                lblRightEmpty.Visible = true;
            }
            else
            {
                lblRightEmpty.Visible = false;
                int y = 0;
                string currentUserName = CurrentSession.CurrentUser?.FullName ?? "Agent";
                foreach (var act in snapshot.RecentActivities)
                {
                    var row = CreateItemRow(
                        iconText: null,
                        title: currentUserName,
                        subtitle: $"{act.Type}: {act.Notes}",
                        statusText: act.Status,
                        timeAgo: act.TimeAgo,
                        onClick: null,
                        useAvatar: true,
                        avatarName: currentUserName
                    );

                    row.Location = new Point(0, y);
                    row.Width = Math.Max(200, pnlRightList.ClientSize.Width - 4);
                    pnlRightList.Controls.Add(row);
                    y += row.Height + 8;
                }
            }
        }

        // =========================================================================
        // 2. MANAGER DASHBOARD RENDERER (Team-wide, zero individual follow-up visibility)
        // =========================================================================
        private void RenderManagerDashboard(ManagerDashboardDto snapshot)
        {
            lblTitle.Text = snapshot.Greeting;
            lblSubtitle.Text = snapshot.DateText;
            _chartNavigationTarget = "Analytics";
            ConfigureContentLayout(0);

            // Quick Action: "View Full Team Dashboard"
            pnlQuickActions.Controls.Clear();
            var btnTeamDashboard = CreateQuickActionButton("📊 View Full Team Dashboard", BiDisplayConstants.PrimaryAccent, Color.White, (_, _) =>
            {
                RequestNavigation("Analytics");
            });
            pnlQuickActions.Controls.Add(btnTeamDashboard);

            // 4 KPI Cards
            ConfigureKpiCard(kpi1, "TEAM DEALS CLOSED", snapshot.TeamDealsThisMonthCount, "Current month", BiDisplayConstants.StatusWon, KpiIconType.Briefcase, () => RequestNavigation("Deals:Closed"));
            ConfigureKpiCard(kpi2, "TEAM OPEN TICKETS", snapshot.TeamOpenTicketsCount, "Across all agents", BiDisplayConstants.StatusLost, KpiIconType.Ticket, () => RequestNavigation("SupportTickets:Open"));
            ConfigureKpiCard(kpi3, "PENDING ASSIGNMENTS", snapshot.PendingAssignmentsCount, "Awaiting manager action", BiDisplayConstants.StatusPending, KpiIconType.Users, () => RequestNavigation("Approvals"));
            ConfigureKpiCard(kpi4, "LEAD CONVERSION", $"{snapshot.TeamConversionRate:F1}%", "Team conversion rate", BiDisplayConstants.PrimaryAccent, KpiIconType.Target, () => RequestNavigation("Leads:Converted"));

            // Glanceable Donut Chart (Won vs Lost)
            _chartNavigationTarget = "Analytics:Won";
            RenderManagerDonut(snapshot.DealsWonThisMonthCount, snapshot.DealsLostThisMonthCount);

            // Left Card: "Pending Assignments" (max 5, with "Assign" button and AvatarLabel)
            pnlLeftCard.Visible = true;
            lblLeftTitle.Text = "Pending Assignments";
            lblLeftSubtitle.Text = $"{snapshot.PendingAssignmentsCount} awaiting manager action";
            pnlLeftList.Controls.Clear();

            if (snapshot.PendingAssignments.Count == 0)
            {
                lblLeftEmpty.Text = "✓  No pending assignments awaiting review.";
                lblLeftEmpty.Visible = true;
            }
            else
            {
                lblLeftEmpty.Visible = false;
                int y = 0;
                foreach (var item in snapshot.PendingAssignments)
                {
                    var row = CreateItemRow(
                        iconText: null,
                        title: item.Name,
                        subtitle: $"Submitted by {item.SubmitterName} · {item.TimeAgo}",
                        statusText: "Pending",
                        timeAgo: null,
                        onClick: null,
                        actionBtnText: "Assign",
                        onActionClick: () =>
                        {
                            using var appCtrl = new ApprovalController();
                            var agents = appCtrl.GetAgents();
                            using var dlg = new AssignAgentDialog(item.Name, agents, item.AssignedAgentId);
                            if (dlg.ShowDialog(this) == DialogResult.OK)
                            {
                                var approvalItem = new PendingApprovalItem
                                {
                                    Id = item.Id,
                                    Type = item.Type,
                                    Title = item.Name,
                                    AssignedAgentId = item.AssignedAgentId,
                                    OriginalEntity = item.OriginalEntity!
                                };
                                appCtrl.AssignAgent(approvalItem, dlg.SelectedAgentId, dlg.ApproveNow, dlg.ReviewNotes);
                                LoadData();
                            }
                        },
                        useAvatar: true,
                        avatarName: item.Name
                    );

                    row.Location = new Point(0, y);
                    row.Width = Math.Max(200, pnlLeftList.ClientSize.Width - 4);
                    pnlLeftList.Controls.Add(row);
                    y += row.Height + 8;
                }
            }

            // Middle Card: "Team Recent Activity" (max 5 deals closed / tickets resolved, AvatarLabel for agent, StatusText for outcome)
            lblRightTitle.Text = "Team Recent Activity";
            lblRightSubtitle.Text = "Last 5 team events (deals closed & tickets resolved)";
            pnlRightList.Controls.Clear();

            if (snapshot.TeamRecentActivity.Count == 0)
            {
                lblRightEmpty.Text = "📋  No recent team events found.";
                lblRightEmpty.Visible = true;
            }
            else
            {
                lblRightEmpty.Visible = false;
                int y = 0;
                foreach (var evt in snapshot.TeamRecentActivity)
                {
                    var row = CreateItemRow(
                        iconText: null,
                        title: evt.AgentName,
                        subtitle: $"{evt.ActionTitle} · {evt.Description}",
                        statusText: evt.Outcome,
                        timeAgo: evt.TimeAgo,
                        onClick: null,
                        useAvatar: true,
                        avatarName: evt.AgentName
                    );

                    row.Location = new Point(0, y);
                    row.Width = Math.Max(200, pnlRightList.ClientSize.Width - 4);
                    pnlRightList.Controls.Add(row);
                    y += row.Height + 8;
                }
            }
        }

        // =========================================================================
        // 3. ADMIN DASHBOARD RENDERER (Business oversight, no direct record editing)
        // =========================================================================
        private void RenderAdminDashboard(AdminDashboardDto snapshot)
        {
            lblTitle.Text = snapshot.Greeting;
            lblSubtitle.Text = snapshot.DateText;

            // Quick Actions: "Manage Users", "View Reports" / "View Deals"
            pnlQuickActions.Controls.Clear();
            var btnManageUsers = CreateQuickActionButton("👥 Manage Users", BiDisplayConstants.PrimaryAccent, Color.White, (_, _) =>
            {
                RequestNavigation("SalesStaff");
            });
            pnlQuickActions.Controls.Add(btnManageUsers);

            if (CurrentSession.CanAccessBusinessIntelligence)
            {
                var btnReports = CreateQuickActionButton("📊 View Reports", Color.White, Theme.TextPrimary, (_, _) =>
                {
                    RequestNavigation("Reports");
                }, hasBorder: true);
                pnlQuickActions.Controls.Add(btnReports);
            }
            else
            {
                var btnDeals = CreateQuickActionButton("💼 View Deals", Color.White, Theme.TextPrimary, (_, _) =>
                {
                    RequestNavigation("Deals");
                }, hasBorder: true);
                pnlQuickActions.Controls.Add(btnDeals);
            }

            // 4 KPI Cards: Real, clear titles with one-line subtext for context
            ConfigureKpiCard(kpi1, "Total Active Users", snapshot.TotalActiveUsersCount, snapshot.ActiveUsersSubtext, BiDisplayConstants.PrimaryAccent, KpiIconType.Users, () => RequestNavigation("SalesStaff"));
            ConfigureKpiCard(kpi2, "Open Support Tickets", snapshot.OpenTicketsCount, snapshot.OpenTicketsSubtext, BiDisplayConstants.StatusLost, KpiIconType.Ticket, () => RequestNavigation("SupportTickets:Open"), null, string.IsNullOrWhiteSpace(snapshot.OpenTicketsSubtext) ? null : BiDisplayConstants.StatusLost);
            ConfigureKpiCard(kpi3, "Deals Closed This Month", snapshot.DealsClosedThisMonthCount, snapshot.DealsClosedSubtext, BiDisplayConstants.HighlightAccent, KpiIconType.Currency, () => RequestNavigation("Deals:Closed"));
            
            if (CurrentSession.CanAccessBusinessIntelligence)
            {
                ConfigureKpiCard(kpi4, "Subscription Status", snapshot.SubscriptionStatus, snapshot.SubscriptionExpiryText, BiDisplayConstants.StatusWon, KpiIconType.Building, () => RequestNavigation("Reports"), StatusColorHelper.GetTextColor(snapshot.SubscriptionStatus));
            }
            else
            {
                ConfigureKpiCard(kpi4, "Subscription Tier", "Tenant A (Standard)", "Transactions & Data Collection", BiDisplayConstants.StatusWon, KpiIconType.Building, () => RequestNavigation("Deals"));
            }

            // Multi-section layout container for Admin
            BuildOrGetAdminContainer();

            _pnlAdminContainer?.SuspendLayout();
            _pnlTeamRosterList?.SuspendLayout();
            _pnlTicketsAttentionList?.SuspendLayout();
            _pnlRecentActivityList?.SuspendLayout();

            try
            {
                // 1. Team Roster Snapshot
                if (_pnlTeamRosterList != null)
                {
                    _pnlTeamRosterList.Controls.Clear();
                    if (_cardTeamRoster?.Controls["lblRosterSub"] is Label lblSub)
                    {
                        lblSub.Text = $"{snapshot.TeamRoster.Count} members listed · Oversight only";
                    }

                    if (snapshot.TeamRoster.Count == 0)
                    {
                        var lblEmpty = new Label
                        {
                            Text = "👥  No team members found.",
                            Font = new Font("Segoe UI", 9.5f),
                            ForeColor = Theme.TextSecondary,
                            TextAlign = ContentAlignment.MiddleCenter,
                            Dock = DockStyle.Fill
                        };
                        _pnlTeamRosterList.Controls.Add(lblEmpty);
                    }
                    else
                    {
                        int y = 0;
                        foreach (var item in snapshot.TeamRoster)
                        {
                            var row = CreateTeamRosterRow(item, () => RequestNavigation("SalesStaff"));
                            row.Location = new Point(0, y);
                            row.Width = Math.Max(200, _pnlTeamRosterList.ClientSize.Width - 4);
                            _pnlTeamRosterList.Controls.Add(row);
                            y += row.Height + 8;
                        }
                    }
                }

                // 4. Tickets Needing Attention
                if (_pnlTicketsAttentionList != null)
                {
                    _pnlTicketsAttentionList.Controls.Clear();
                    if (_cardTicketsAttention?.Controls["lblAttnSub"] is Label lblSub)
                    {
                        lblSub.Text = $"{snapshot.TicketsNeedingAttention.Count} oldest open support tickets";
                    }

                    if (snapshot.TicketsNeedingAttention.Count == 0)
                    {
                        var lblEmpty = new Label
                        {
                            Text = "✓  No open tickets needing attention. Support queue clear!",
                            Font = new Font("Segoe UI", 9.5f),
                            ForeColor = Theme.TextSecondary,
                            TextAlign = ContentAlignment.MiddleCenter,
                            Dock = DockStyle.Fill
                        };
                        _pnlTicketsAttentionList.Controls.Add(lblEmpty);
                    }
                    else
                    {
                        int y = 0;
                        foreach (var item in snapshot.TicketsNeedingAttention)
                        {
                            var row = CreateTicketAttentionRow(item, () => RequestNavigation("SupportTickets"));
                            row.Location = new Point(0, y);
                            row.Width = Math.Max(200, _pnlTicketsAttentionList.ClientSize.Width - 4);
                            _pnlTicketsAttentionList.Controls.Add(row);
                            y += row.Height + 8;
                        }
                    }
                }

                // 5. Recent System Activity (Real entries only; fully omitted if no data exists)
                bool hasRealData = snapshot.RecentSystemActivities.Count > 0;
                if (_cardRecentActivity != null)
                {
                    _cardRecentActivity.Visible = hasRealData;
                    if (hasRealData && _pnlRecentActivityList != null)
                    {
                        _pnlRecentActivityList.Controls.Clear();
                        int y = 0;
                        foreach (var log in snapshot.RecentSystemActivities)
                        {
                            var row = CreateItemRow(
                                iconText: log.UseAvatar ? null : log.Icon,
                                title: log.Title,
                                subtitle: log.Details,
                                statusText: log.Status,
                                timeAgo: log.TimeAgo,
                                onClick: null,
                                useAvatar: log.UseAvatar,
                                avatarName: log.AvatarName
                            );

                            row.Location = new Point(0, y);
                            row.Width = Math.Max(200, _pnlRecentActivityList.ClientSize.Width - 4);
                            _pnlRecentActivityList.Controls.Add(row);
                            y += row.Height + 8;
                        }
                    }
                }
            }
            finally
            {
                _pnlTeamRosterList?.ResumeLayout(false);
                _pnlTicketsAttentionList?.ResumeLayout(false);
                _pnlRecentActivityList?.ResumeLayout(false);
                _pnlAdminContainer?.ResumeLayout(false);
            }

            if (_pnlAdminContainer != null)
            {
                LayoutAdminControls();
            }

            // Defer chart rendering slightly to ensure cards & layout paint immediately without freezing UI thread
            BeginInvoke(() =>
            {
                if (IsDisposed) return;

                // 2. Ticket Breakdown Donut Chart
                RenderAdminTicketDonut(snapshot.OpenTicketsBreakdown, snapshot.InProgressTicketsBreakdown, snapshot.ResolvedTicketsBreakdown);

                // 3. Commission Trend Bar Chart
                RenderAdminCommissionTrend(snapshot.CommissionTrendLast6Months);
            });
        }

        private Panel BuildOrGetAdminContainer()
        {
            if (_pnlAdminContainer != null) return _pnlAdminContainer;

            _pnlAdminContainer = new Panel
            {
                AutoScroll = true,
                BackColor = Color.Transparent,
                Visible = false
            };

            // 1. Team Roster Snapshot Card
            _cardTeamRoster = new Panel { BackColor = Color.White };
            UiRadiusHelper.StyleCard(_cardTeamRoster, 12);

            var lblRosterTitle = new Label
            {
                Text = "Team Roster Snapshot",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(16, 14),
                AutoSize = true
            };
            var lblRosterSub = new Label
            {
                Name = "lblRosterSub",
                Text = "Staff oversight",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(17, 36),
                AutoSize = true
            };
            var btnRosterViewAll = new Label
            {
                Name = "btnRosterViewAll",
                Text = "View All →",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                Cursor = Cursors.Hand,
                AutoSize = true
            };
            btnRosterViewAll.Click += (_, _) => RequestNavigation("SalesStaff");

            _pnlTeamRosterList = new Panel
            {
                AutoScroll = true,
                Location = new Point(16, 58)
            };

            _cardTeamRoster.Controls.Add(lblRosterTitle);
            _cardTeamRoster.Controls.Add(lblRosterSub);
            _cardTeamRoster.Controls.Add(btnRosterViewAll);
            _cardTeamRoster.Controls.Add(_pnlTeamRosterList);

            // 2. Ticket Breakdown Card
            _cardTicketBreakdown = new Panel { BackColor = Color.White };
            UiRadiusHelper.StyleCard(_cardTicketBreakdown, 12);

            var lblTicketTitle = new Label
            {
                Text = "Ticket Breakdown",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(16, 14),
                AutoSize = true
            };
            var lblTicketSub = new Label
            {
                Text = "Support queue status",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(17, 36),
                AutoSize = true
            };
            _plotAdminTicketBreakdown = new ScottPlot.WinForms.FormsPlot
            {
                Location = new Point(12, 54),
                Cursor = Cursors.Hand
            };
            BiDisplayConstants.ConfigureStandardPlot(_plotAdminTicketBreakdown);
            _plotAdminTicketBreakdown.Click += (_, _) => RequestNavigation("SupportTickets:Open");

            _lblAdminTicketBreakdownFooter = new Label
            {
                Text = "View all support tickets →",
                Font = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = Theme.Primary,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _lblAdminTicketBreakdownFooter.Click += (_, _) => RequestNavigation("SupportTickets:Open");
            _cardTicketBreakdown.Click += (_, _) => RequestNavigation("SupportTickets:Open");
            _cardTicketBreakdown.Cursor = Cursors.Hand;

            _cardTicketBreakdown.Controls.Add(lblTicketTitle);
            _cardTicketBreakdown.Controls.Add(lblTicketSub);
            _cardTicketBreakdown.Controls.Add(_plotAdminTicketBreakdown);
            _cardTicketBreakdown.Controls.Add(_lblAdminTicketBreakdownFooter);

            // 3. Commission Trend Card
            _cardCommissionTrend = new Panel { BackColor = Color.White, Cursor = Cursors.Hand };
            UiRadiusHelper.StyleCard(_cardCommissionTrend, 12);

            var lblCommTitle = new Label
            {
                Text = "Commission Trend",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(16, 14),
                AutoSize = true
            };
            var lblCommSub = new Label
            {
                Text = "Commission Earned — Last 6 Months",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(17, 36),
                AutoSize = true
            };
            _plotAdminCommissionTrend = new ScottPlot.WinForms.FormsPlot
            {
                Location = new Point(12, 54),
                Cursor = Cursors.Hand
            };
            BiDisplayConstants.ConfigureStandardPlot(_plotAdminCommissionTrend);
            _plotAdminCommissionTrend.Click += (_, _) => RequestNavigation("Reports:Commission");

            _lblAdminCommissionFooter = new Label
            {
                Text = "View Commission Report →",
                Font = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = Theme.Primary,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _lblAdminCommissionFooter.Click += (_, _) => RequestNavigation("Reports:Commission");
            _cardCommissionTrend.Click += (_, _) => RequestNavigation("Reports:Commission");

            _cardCommissionTrend.Controls.Add(lblCommTitle);
            _cardCommissionTrend.Controls.Add(lblCommSub);
            _cardCommissionTrend.Controls.Add(_plotAdminCommissionTrend);
            _cardCommissionTrend.Controls.Add(_lblAdminCommissionFooter);

            // 4. Tickets Needing Attention Card
            _cardTicketsAttention = new Panel { BackColor = Color.White };
            UiRadiusHelper.StyleCard(_cardTicketsAttention, 12);

            var lblAttnTitle = new Label
            {
                Text = "Tickets Needing Attention",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(16, 14),
                AutoSize = true
            };
            var lblAttnSub = new Label
            {
                Name = "lblAttnSub",
                Text = "Oldest open support tickets",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(17, 36),
                AutoSize = true
            };
            var btnAttnViewAll = new Label
            {
                Name = "btnAttnViewAll",
                Text = "View Support Tickets →",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                Cursor = Cursors.Hand,
                AutoSize = true
            };
            btnAttnViewAll.Click += (_, _) => RequestNavigation("SupportTickets");

            _pnlTicketsAttentionList = new Panel
            {
                AutoScroll = true,
                Location = new Point(16, 58)
            };

            _cardTicketsAttention.Controls.Add(lblAttnTitle);
            _cardTicketsAttention.Controls.Add(lblAttnSub);
            _cardTicketsAttention.Controls.Add(btnAttnViewAll);
            _cardTicketsAttention.Controls.Add(_pnlTicketsAttentionList);

            // 5. Recent System Activity Card
            _cardRecentActivity = new Panel { BackColor = Color.White, Visible = false };
            UiRadiusHelper.StyleCard(_cardRecentActivity, 12);

            var lblActTitle = new Label
            {
                Text = "Recent System Activity",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(16, 14),
                AutoSize = true
            };
            var lblActSub = new Label
            {
                Text = "Live system audit events & configuration updates",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(17, 36),
                AutoSize = true
            };
            _pnlRecentActivityList = new Panel
            {
                AutoScroll = true,
                Location = new Point(16, 58)
            };

            _cardRecentActivity.Controls.Add(lblActTitle);
            _cardRecentActivity.Controls.Add(lblActSub);
            _cardRecentActivity.Controls.Add(_pnlRecentActivityList);

            // Add all cards to _pnlAdminContainer
            _pnlAdminContainer.Controls.Add(_cardTeamRoster);
            _pnlAdminContainer.Controls.Add(_cardTicketBreakdown);
            _pnlAdminContainer.Controls.Add(_cardCommissionTrend);
            _pnlAdminContainer.Controls.Add(_cardTicketsAttention);
            _pnlAdminContainer.Controls.Add(_cardRecentActivity);

            Controls.Add(_pnlAdminContainer);
            _pnlAdminContainer.BringToFront();

            _pnlAdminContainer.Resize += (_, _) => LayoutAdminControls();

            return _pnlAdminContainer;
        }

        private void LayoutAdminControls()
        {
            if (_pnlAdminContainer == null || !_pnlAdminContainer.Visible) return;

            _pnlAdminContainer.SuspendLayout();
            try
            {
                int scrollY = -_pnlAdminContainer.AutoScrollPosition.Y;
                _pnlAdminContainer.AutoScrollPosition = Point.Empty;

                int totalWidth = _pnlAdminContainer.ClientSize.Width;
                if (totalWidth <= 0) return;

                int gap = 16;
                int colWidth = Math.Max(280, (totalWidth - gap) / 2);

                int y = 0;

                // Row 1: [Team Roster | Ticket Breakdown]
                int row1Height = 360;
                if (_cardTeamRoster != null)
                {
                    _cardTeamRoster.Location = new Point(0, y);
                    _cardTeamRoster.Size = new Size(colWidth, row1Height);

                    if (_cardTeamRoster.Controls["btnRosterViewAll"] is Label btnViewAll)
                    {
                        btnViewAll.Location = new Point(_cardTeamRoster.Width - btnViewAll.Width - 16, 16);
                    }
                    if (_pnlTeamRosterList != null)
                    {
                        _pnlTeamRosterList.Size = new Size(_cardTeamRoster.Width - 32, _cardTeamRoster.Height - 74);
                        ResizeListItems(_pnlTeamRosterList);
                    }
                }
                if (_cardTicketBreakdown != null)
                {
                    _cardTicketBreakdown.Location = new Point(colWidth + gap, y);
                    _cardTicketBreakdown.Size = new Size(colWidth, row1Height);

                    if (_plotAdminTicketBreakdown != null)
                    {
                        _plotAdminTicketBreakdown.Size = new Size(_cardTicketBreakdown.Width - 24, _cardTicketBreakdown.Height - 82);
                    }
                    if (_lblAdminTicketBreakdownFooter != null)
                    {
                        _lblAdminTicketBreakdownFooter.Location = new Point(12, _cardTicketBreakdown.Height - 30);
                        _lblAdminTicketBreakdownFooter.Size = new Size(_cardTicketBreakdown.Width - 24, 22);
                    }
                }
                y += row1Height + gap;

                // Row 2: [Commission Trend | Tickets Needing Attention]
                int row2Height = 300;
                if (_cardCommissionTrend != null)
                {
                    _cardCommissionTrend.Location = new Point(0, y);
                    _cardCommissionTrend.Size = new Size(colWidth, row2Height);

                    if (_plotAdminCommissionTrend != null)
                    {
                        _plotAdminCommissionTrend.Size = new Size(_cardCommissionTrend.Width - 24, _cardCommissionTrend.Height - 82);
                    }
                    if (_lblAdminCommissionFooter != null)
                    {
                        _lblAdminCommissionFooter.Location = new Point(12, _cardCommissionTrend.Height - 30);
                        _lblAdminCommissionFooter.Size = new Size(_cardCommissionTrend.Width - 24, 22);
                    }
                }
                if (_cardTicketsAttention != null)
                {
                    _cardTicketsAttention.Location = new Point(colWidth + gap, y);
                    _cardTicketsAttention.Size = new Size(colWidth, row2Height);

                    if (_cardTicketsAttention.Controls["btnAttnViewAll"] is Label btnAttnViewAll)
                    {
                        btnAttnViewAll.Location = new Point(_cardTicketsAttention.Width - btnAttnViewAll.Width - 16, 16);
                    }
                    if (_pnlTicketsAttentionList != null)
                    {
                        _pnlTicketsAttentionList.Size = new Size(_cardTicketsAttention.Width - 32, _cardTicketsAttention.Height - 74);
                        ResizeListItems(_pnlTicketsAttentionList);
                    }
                }
                y += row2Height + gap;

                // Row 3: [Recent System Activity] (rendered if real data exists, omitted otherwise)
                if (_cardRecentActivity != null && _cardRecentActivity.Visible)
                {
                    int row3Height = 260;
                    _cardRecentActivity.Location = new Point(0, y);
                    _cardRecentActivity.Size = new Size(totalWidth, row3Height);

                    if (_pnlRecentActivityList != null)
                    {
                        _pnlRecentActivityList.Size = new Size(_cardRecentActivity.Width - 32, _cardRecentActivity.Height - 74);
                        ResizeListItems(_pnlRecentActivityList);
                    }
                    y += row3Height + gap;
                }

                if (scrollY > 0)
                {
                    _pnlAdminContainer.AutoScrollPosition = new Point(0, scrollY);
                }
            }
            finally
            {
                _pnlAdminContainer.ResumeLayout(true);
            }
        }

        private Panel CreateTeamRosterRow(AdminTeamRosterItemDto item, Action onClick)
        {
            var panel = new Panel
            {
                Height = 50,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.ApplyRoundedCorners(panel, 8);

            var avatar = new AvatarLabel(item.FullName, item.RoleName)
            {
                Location = new Point(8, 6),
                Height = 38,
                AvatarSize = 32,
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(avatar);

            var status = new StatusText(item.Status)
            {
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(status);

            void Reposition()
            {
                avatar.Width = Math.Max(120, panel.Width - status.PreferredWidth - 28);
                status.Location = new Point(panel.Width - status.PreferredWidth - 14, (panel.Height - status.Height) / 2);
            }

            panel.SizeChanged += (_, _) => Reposition();
            Reposition();

            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 8);
                e.Graphics.DrawPath(pen, path);
            };

            void SetHover(bool hovered)
            {
                panel.BackColor = hovered ? Color.FromArgb(248, 250, 252) : Color.White;
            }

            panel.MouseEnter += (_, _) => SetHover(true);
            panel.MouseLeave += (_, _) => SetHover(false);
            avatar.MouseEnter += (_, _) => SetHover(true);
            avatar.MouseLeave += (_, _) => SetHover(false);
            status.MouseEnter += (_, _) => SetHover(true);
            status.MouseLeave += (_, _) => SetHover(false);

            panel.Click += (_, _) => onClick();
            avatar.Click += (_, _) => onClick();
            status.Click += (_, _) => onClick();

            return panel;
        }

        private Panel CreateTicketAttentionRow(AdminTicketAttentionItemDto item, Action onClick)
        {
            var panel = new Panel
            {
                Height = 54,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.ApplyRoundedCorners(panel, 8);

            string sub = $"#{item.TicketNumber} · {item.Category} · {item.OpenedAgoText}";
            var avatar = new AvatarLabel(item.CustomerName, sub)
            {
                Location = new Point(8, 7),
                Height = 40,
                AvatarSize = 32,
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(avatar);

            var status = new StatusText(item.Priority)
            {
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(status);

            void Reposition()
            {
                avatar.Width = Math.Max(120, panel.Width - status.PreferredWidth - 28);
                status.Location = new Point(panel.Width - status.PreferredWidth - 14, (panel.Height - status.Height) / 2);
            }

            panel.SizeChanged += (_, _) => Reposition();
            Reposition();

            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 8);
                e.Graphics.DrawPath(pen, path);
            };

            void SetHover(bool hovered)
            {
                panel.BackColor = hovered ? Color.FromArgb(248, 250, 252) : Color.White;
            }

            panel.MouseEnter += (_, _) => SetHover(true);
            panel.MouseLeave += (_, _) => SetHover(false);
            avatar.MouseEnter += (_, _) => SetHover(true);
            avatar.MouseLeave += (_, _) => SetHover(false);
            status.MouseEnter += (_, _) => SetHover(true);
            status.MouseLeave += (_, _) => SetHover(false);

            panel.Click += (_, _) => onClick();
            avatar.Click += (_, _) => onClick();
            status.Click += (_, _) => onClick();

            return panel;
        }

        private void RenderAdminTicketDonut(int open, int inProgress, int resolved)
        {
            if (_plotAdminTicketBreakdown == null) return;

            _plotAdminTicketBreakdown.Plot.Clear();
            BiDisplayConstants.ConfigureStandardPlot(_plotAdminTicketBreakdown);

            if (open == 0 && inProgress == 0 && resolved == 0)
            {
                BiDisplayConstants.ShowPlotEmpty(_plotAdminTicketBreakdown, "No support tickets recorded");
                return;
            }

            var slices = new List<(string label, double value, Color color)>
            {
                ("Open", open, BiDisplayConstants.StatusNeutral),
                ("In Progress", inProgress, BiDisplayConstants.StatusPending),
                ("Resolved", resolved, BiDisplayConstants.StatusWon)
            };
            BiDisplayConstants.RenderDonutPlot(_plotAdminTicketBreakdown, slices, 3);
        }

        private void RenderAdminCommissionTrend(List<AdminCommissionTrendPointDto> trend)
        {
            if (_plotAdminCommissionTrend == null) return;

            _plotAdminCommissionTrend.Plot.Clear();
            BiDisplayConstants.ConfigureStandardPlot(_plotAdminCommissionTrend);

            if (trend == null || trend.Count == 0 || trend.All(t => t.CommissionAmount <= 0.0001))
            {
                BiDisplayConstants.ShowPlotEmpty(_plotAdminCommissionTrend, "No commission recorded in past 6 months");
                return;
            }

            var bars = new List<ScottPlot.Bar>();
            var ticks = new List<ScottPlot.Tick>();

            for (int i = 0; i < trend.Count; i++)
            {
                bars.Add(new ScottPlot.Bar
                {
                    Position = i,
                    Value = trend[i].CommissionAmount,
                    FillColor = ScottPlot.Color.FromColor(Theme.Primary),
                    LineWidth = 0
                });
                ticks.Add(new ScottPlot.Tick(i, trend[i].MonthLabel));
            }

            _plotAdminCommissionTrend.Plot.Add.Bars(bars);
            var tickGen = new ScottPlot.TickGenerators.NumericManual(ticks.ToArray());
            _plotAdminCommissionTrend.Plot.Axes.Bottom.TickGenerator = tickGen;
            _plotAdminCommissionTrend.Plot.Axes.Bottom.MinimumSize = 25;

            // Frameless, glanceable visual without Y axis labels or grid clutter
            _plotAdminCommissionTrend.Plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.EmptyTickGenerator();
            _plotAdminCommissionTrend.Plot.Axes.Left.MinimumSize = 0;
            _plotAdminCommissionTrend.Plot.Axes.Frameless();
            _plotAdminCommissionTrend.Plot.HideGrid();

            double maxVal = trend.Max(t => t.CommissionAmount);
            _plotAdminCommissionTrend.Plot.Axes.SetLimits(-0.6, trend.Count - 0.4, 0, Math.Max(1.0, maxVal * 1.15));
            _plotAdminCommissionTrend.Refresh();
        }

        // =========================================================================
        // 4. SUPER ADMIN DASHBOARD RENDERER (Platform oversight)
        // =========================================================================
        private void RenderSuperAdminDashboard(SuperAdminDashboardDto snapshot)
        {
            lblTitle.Text = snapshot.Greeting;
            lblSubtitle.Text = snapshot.DateText;
            _chartNavigationTarget = "Reports";
            ConfigureContentLayout(1);

            // Quick Actions: "Manage Administrators", "System Settings"
            pnlQuickActions.Controls.Clear();
            var btnAdmins = CreateQuickActionButton("👥 Manage Administrators", Color.White, Theme.TextPrimary, (_, _) =>
            {
                RequestNavigation("SalesStaff");
            }, hasBorder: true);

            var btnSettings = CreateQuickActionButton("⚙️ System Settings", BiDisplayConstants.PrimaryAccent, Color.White, (_, _) =>
            {
                RequestNavigation("Settings");
            });

            pnlQuickActions.Controls.Add(btnSettings);
            pnlQuickActions.Controls.Add(btnAdmins);

            // 4 KPI Cards
            Color backupColor = snapshot.LastBackupStatus.Equals("Success", StringComparison.OrdinalIgnoreCase) ? BiDisplayConstants.StatusWon : BiDisplayConstants.StatusLost;

            ConfigureKpiCard(kpi1, "TOTAL TENANTS", snapshot.TotalTenantsCount, "Active client databases", BiDisplayConstants.PrimaryAccent, KpiIconType.Building, () => RequestNavigation("Reports"));
            ConfigureKpiCard(kpi2, "ACTIVE SUBSCRIPTIONS", snapshot.ActiveSubscriptionsCount, "Current paid plans", BiDisplayConstants.StatusWon, KpiIconType.Currency, () => RequestNavigation("Reports"));
            ConfigureKpiCard(kpi3, "EXPIRING THIS MONTH", snapshot.SubscriptionsExpiringThisMonthCount, "Needs renewal soon", BiDisplayConstants.StatusPending, KpiIconType.Clock, () => RequestNavigation("Reports"));
            ConfigureKpiCard(kpi4, "LAST BACKUP", snapshot.LastBackupStatus, snapshot.LastBackupTimeText, BiDisplayConstants.SkyAccent, KpiIconType.Refresh, () => RequestNavigation("Reports"), backupColor);

            // Glanceable Bar Chart
            RenderSuperAdminBar(snapshot.ActiveSubscriptionsCount, snapshot.SubscriptionsExpiringThisMonthCount, snapshot.ExpiredSubscriptionsCount);

            // Left Card: "Recent Platform Activity" (registrations, subscription changes, backups)
            pnlLeftCard.Visible = true;
            lblLeftTitle.Text = "Recent Platform Activity";
            lblLeftSubtitle.Text = "Registrations, renewals & backups";
            pnlLeftList.Controls.Clear();

            if (snapshot.RecentPlatformActivities.Count == 0)
            {
                lblLeftEmpty.Text = "📋  No platform events recorded yet.";
                lblLeftEmpty.Visible = true;
            }
            else
            {
                lblLeftEmpty.Visible = false;
                int y = 0;
                foreach (var evt in snapshot.RecentPlatformActivities)
                {
                    bool isUser = evt.Icon == "👤";
                    var row = CreateItemRow(
                        iconText: isUser ? null : evt.Icon,
                        title: evt.Title,
                        subtitle: evt.Details,
                        statusText: evt.Status,
                        timeAgo: evt.TimeAgo,
                        onClick: null,
                        useAvatar: isUser,
                        avatarName: evt.Title.Replace("User Registration: ", "")
                    );

                    row.Location = new Point(0, y);
                    row.Width = Math.Max(200, pnlLeftList.ClientSize.Width - 4);
                    pnlLeftList.Controls.Add(row);
                    y += row.Height + 8;
                }
            }
        }

        // =========================================================================
        // GLANCEABLE CHARTS (<2s comprehension, frameless, no legend clutter)
        // =========================================================================
        private void RenderAgentSparkline(List<double> data)
        {
            plotGlanceable.Plot.Clear();
            BiDisplayConstants.ConfigureStandardPlot(plotGlanceable);
            lblChartTitle.Text = "Deals Closed Trend";
            lblChartSubtitle.Text = "Last 30 days (daily volume)";
            lblChartFooter.Text = $"Total: {data.Sum():N0} closed · View analytics →";

            if (data == null || data.Count == 0 || data.All(v => v <= 0))
            {
                BiDisplayConstants.ShowPlotEmpty(plotGlanceable, "No closed deals in past 30 days");
                return;
            }

            double[] xs = Enumerable.Range(0, data.Count).Select(i => (double)i).ToArray();
            double[] ys = data.ToArray();

            var scatter = plotGlanceable.Plot.Add.Scatter(xs, ys);
            scatter.Color = ScottPlot.Color.FromColor(Theme.Primary);
            scatter.LineWidth = 3.0f;
            scatter.MarkerSize = 0; // pure sparkline
            scatter.FillY = true;
            scatter.FillYColor = ScottPlot.Color.FromColor(Color.FromArgb(40, Theme.Primary.R, Theme.Primary.G, Theme.Primary.B));

            plotGlanceable.Plot.Axes.Frameless();
            plotGlanceable.Plot.HideGrid();
            plotGlanceable.Plot.Axes.SetLimits(-0.5, data.Count - 0.5, 0, Math.Max(1.0, ys.Max() * 1.25));
            plotGlanceable.Refresh();
        }

        private void RenderManagerDonut(int dealsWon, int dealsLost)
        {
            lblChartTitle.Text = "Deals Won vs. Lost";
            lblChartSubtitle.Text = "This month's outcome";
            lblChartFooter.Text = $"{dealsWon} Won · {dealsLost} Lost · View analytics →";

            if (dealsWon == 0 && dealsLost == 0)
            {
                BiDisplayConstants.ShowPlotEmpty(plotGlanceable, "No closed deals this month");
                return;
            }

            var slices = new List<(string label, double value, Color color)>
            {
                ("Won", dealsWon, BiDisplayConstants.StatusWon),
                ("Lost", dealsLost, BiDisplayConstants.StatusLost)
            };
            BiDisplayConstants.RenderDonutPlot(plotGlanceable, slices, 2);
        }

        private void RenderAdminDonut(int open, int inProgress, int resolved)
        {
            lblChartTitle.Text = "Ticket Breakdown";
            lblChartSubtitle.Text = "Support queue status";
            lblChartFooter.Text = "View all support tickets →";

            if (open == 0 && inProgress == 0 && resolved == 0)
            {
                BiDisplayConstants.ShowPlotEmpty(plotGlanceable, "No support tickets recorded");
                return;
            }

            var slices = new List<(string label, double value, Color color)>
            {
                ("Open", open, BiDisplayConstants.StatusNeutral),
                ("In Progress", inProgress, BiDisplayConstants.StatusPending),
                ("Resolved", resolved, BiDisplayConstants.StatusWon)
            };
            BiDisplayConstants.RenderDonutPlot(plotGlanceable, slices, 3);
        }

        private void RenderSuperAdminBar(int active, int expiring, int expired)
        {
            lblChartTitle.Text = "Subscriptions";
            lblChartSubtitle.Text = "Tenant licensing status";
            lblChartFooter.Text = "View all subscriptions →";

            if (active == 0 && expiring == 0 && expired == 0)
            {
                BiDisplayConstants.ShowPlotEmpty(plotGlanceable, "No subscription data");
                return;
            }

            var items = new List<(string label, double value, Color color)>
            {
                ("Active", active, BiDisplayConstants.StatusWon),
                ("Expiring", expiring, BiDisplayConstants.StatusPending),
                ("Expired", expired, BiDisplayConstants.StatusLost)
            };
            BiDisplayConstants.RenderBarPlot(plotGlanceable, items);
        }

        // =========================================================================
        // UI HELPERS & ITEM ROW GENERATOR (StatusText & AvatarLabel standard)
        // =========================================================================
        private static Button CreateQuickActionButton(string text, Color bg, Color fg, EventHandler onClick, bool hasBorder = false)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = bg,
                ForeColor = fg,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Height = 36,
                AutoSize = true,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(4, 0, 0, 0),
                Padding = new Padding(12, 0, 12, 0)
            };

            if (hasBorder)
            {
                btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
                btn.FlatAppearance.BorderSize = 1;
            }
            else
            {
                btn.FlatAppearance.BorderSize = 0;
            }

            btn.Click += onClick;
            UiRadiusHelper.StyleButton(btn, 8);
            return btn;
        }

        private void ConfigureContentLayout(int layoutMode)
        {
            pnlContentSplit.SuspendLayout();
            pnlContentSplit.Controls.Clear();
            pnlContentSplit.ColumnStyles.Clear();

            switch (layoutMode)
            {
                case 0: // 3 columns (Agent, Manager): 38%, 34%, 28%
                    pnlContentSplit.ColumnCount = 3;
                    pnlContentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
                    pnlContentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
                    pnlContentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));

                    pnlLeftCard.Margin = new Padding(0, 0, 8, 0);
                    pnlRightCard.Margin = new Padding(8, 0, 8, 0);
                    pnlChartCard.Margin = new Padding(8, 0, 0, 0);

                    pnlLeftCard.Visible = true;
                    pnlRightCard.Visible = true;
                    pnlChartCard.Visible = true;

                    pnlContentSplit.Controls.Add(pnlLeftCard, 0, 0);
                    pnlContentSplit.Controls.Add(pnlRightCard, 1, 0);
                    pnlContentSplit.Controls.Add(pnlChartCard, 2, 0);
                    break;

                case 1: // 2 columns (Super Admin, or Admin with system logs): 60%, 40%
                    pnlContentSplit.ColumnCount = 2;
                    pnlContentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
                    pnlContentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

                    pnlLeftCard.Margin = new Padding(0, 0, 8, 0);
                    pnlChartCard.Margin = new Padding(8, 0, 0, 0);

                    pnlLeftCard.Visible = true;
                    pnlRightCard.Visible = false;
                    pnlChartCard.Visible = true;

                    pnlContentSplit.Controls.Add(pnlLeftCard, 0, 0);
                    pnlContentSplit.Controls.Add(pnlChartCard, 1, 0);
                    break;

                case 2: // 1 centered card (Admin without system logs): 22%, 56%, 22%
                    pnlContentSplit.ColumnCount = 3;
                    pnlContentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
                    pnlContentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
                    pnlContentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));

                    pnlChartCard.Margin = Padding.Empty;

                    pnlLeftCard.Visible = false;
                    pnlRightCard.Visible = false;
                    pnlChartCard.Visible = true;

                    pnlContentSplit.Controls.Add(_spacerLeft, 0, 0);
                    pnlContentSplit.Controls.Add(pnlChartCard, 1, 0);
                    pnlContentSplit.Controls.Add(_spacerRight, 2, 0);
                    break;
            }

            pnlContentSplit.ResumeLayout(true);
        }

        private static void ConfigureKpiCard(KpiCard card, string title, object value, string subtitle, Color accentColor, KpiIconType icon, Action onClick, Color? valueColor = null, Color? subtitleColor = null)
        {
            card.SetTitle(title);
            if (value is int intVal)
            {
                card.SetValue(intVal);
            }
            else
            {
                card.SetValue(value?.ToString() ?? "0");
            }

            if (valueColor.HasValue)
            {
                card.SetValueColor(valueColor.Value);
            }
            else
            {
                card.SetValueColor(Color.FromArgb(15, 23, 42));
            }

            card.SetSubtitle(subtitle, subtitleColor);
            card.SetIcon(icon, accentColor);
            card.ClickMode = KpiClickMode.Navigate;
            card.Cursor = Cursors.Hand;
            card.SetAction(onClick);
        }

        private Panel CreateItemRow(
            string? iconText,
            string title,
            string subtitle,
            string? statusText = null,
            string? timeAgo = null,
            Action? onClick = null,
            string? actionBtnText = null,
            Action? onActionClick = null,
            bool useAvatar = false,
            string? avatarName = null)
        {
            var panel = new Panel
            {
                Height = 58,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 8)
            };
            UiRadiusHelper.ApplyRoundedCorners(panel, 8);

            int textLeft = 12;

            if (useAvatar)
            {
                string personName = string.IsNullOrWhiteSpace(avatarName) ? title : avatarName;
                var avatar = new AvatarLabel(personName, subtitle)
                {
                    Location = new Point(10, 10),
                    Height = 38,
                    AvatarSize = 32,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
                };
                panel.Controls.Add(avatar);
                textLeft = avatar.Right + 8;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(iconText))
                {
                    var lblIcon = new Label
                    {
                        Text = iconText,
                        Font = new Font("Segoe UI Emoji", 11f),
                        Location = new Point(10, 16),
                        Size = new Size(28, 26),
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = Color.Transparent
                    };
                    panel.Controls.Add(lblIcon);
                    textLeft = 42;
                }

                var lblTitle = new Label
                {
                    Text = title,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 23, 42),
                    Location = new Point(textLeft, 9),
                    AutoSize = true,
                    AutoEllipsis = true,
                    BackColor = Color.Transparent
                };

                var lblSubtitle = new Label
                {
                    Text = subtitle,
                    Font = new Font("Segoe UI", 8.25f),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Location = new Point(textLeft, 31),
                    AutoSize = true,
                    AutoEllipsis = true,
                    BackColor = Color.Transparent
                };

                panel.Controls.Add(lblTitle);
                panel.Controls.Add(lblSubtitle);
            }

            // Right-side controls: Action Button, StatusText, TimeAgo
            Button? btnAction = null;
            if (!string.IsNullOrWhiteSpace(actionBtnText) && onActionClick != null)
            {
                btnAction = new Button
                {
                    Text = actionBtnText,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(14, 116, 144),
                    BackColor = Color.FromArgb(240, 249, 255),
                    Size = new Size(68, 28),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnAction.FlatAppearance.BorderColor = Color.FromArgb(186, 230, 253);
                btnAction.Click += (_, _) => onActionClick();
                UiRadiusHelper.StyleButton(btnAction, 6);
                panel.Controls.Add(btnAction);
            }

            StatusText? lblStatus = null;
            if (!string.IsNullOrWhiteSpace(statusText))
            {
                lblStatus = new StatusText(statusText);
                panel.Controls.Add(lblStatus);
            }

            Label? lblTime = null;
            if (!string.IsNullOrWhiteSpace(timeAgo))
            {
                lblTime = new Label
                {
                    Text = timeAgo,
                    Font = new Font("Segoe UI", 8.25f),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleRight
                };
                panel.Controls.Add(lblTime);
            }

            void RepositionRightControls()
            {
                int currentX = panel.Width - 14;
                if (btnAction != null)
                {
                    btnAction.Location = new Point(currentX - btnAction.Width, (panel.Height - btnAction.Height) / 2);
                    currentX = btnAction.Left - 8;
                }
                if (lblStatus != null)
                {
                    lblStatus.Location = new Point(currentX - lblStatus.PreferredWidth, (panel.Height - lblStatus.Height) / 2);
                    currentX = lblStatus.Left - 8;
                }
                if (lblTime != null)
                {
                    lblTime.Location = new Point(currentX - lblTime.PreferredWidth, (panel.Height - lblTime.Height) / 2);
                }
            }

            panel.SizeChanged += (_, _) => RepositionRightControls();
            RepositionRightControls();

            // Border painting
            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                using var path = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 8);
                e.Graphics.DrawPath(pen, path);
            };

            // Click handling and hover effects
            if (onClick != null)
            {
                panel.Cursor = Cursors.Hand;
                foreach (Control c in panel.Controls)
                {
                    if (c != btnAction) c.Cursor = Cursors.Hand;
                }

                void SetHover(bool hovered)
                {
                    panel.BackColor = hovered ? Color.FromArgb(248, 250, 252) : Color.White;
                }

                panel.MouseEnter += (_, _) => SetHover(true);
                panel.MouseLeave += (_, _) => SetHover(false);
                foreach (Control c in panel.Controls)
                {
                    if (c != btnAction)
                    {
                        c.MouseEnter += (_, _) => SetHover(true);
                        c.MouseLeave += (_, _) => SetHover(false);
                        c.Click += (_, _) => onClick();
                    }
                }
                panel.Click += (_, _) => onClick();
            }

            return panel;
        }

        private static string GetActivityIcon(string type) => (type ?? "").ToLowerInvariant() switch
        {
            "call" => "📞",
            "email" => "✉️",
            "meeting" => "🤝",
            "showing" => "🏠",
            "ticket" => "🎟",
            "deal" => "💼",
            "lead" => "🎯",
            _ => "📋"
        };

        private static void ResizeListItems(Panel pnl)
        {
            if (pnl.Width <= 0) return;
            int targetWidth = Math.Max(150, pnl.ClientSize.Width - 4);
            foreach (Control c in pnl.Controls)
            {
                if (c is Panel row)
                {
                    row.Width = targetWidth;
                }
            }
        }

        private void LayoutControls()
        {
            if (IsDisposed) return;

            int leftMargin = 30;
            int rightMargin = 30;
            int totalWidth = ClientSize.Width;

            lblTitle.Location = new Point(leftMargin, 20);
            lblSubtitle.Location = new Point(leftMargin + 2, lblTitle.Bottom + 4);
            lblLoading.Location = lblSubtitle.Location;

            pnlQuickActions.Location = new Point(totalWidth - rightMargin - pnlQuickActions.Width, 22);

            int kpiTop = Math.Max(84, lblSubtitle.Bottom + 16);
            pnlKpiContainer.Location = new Point(leftMargin, kpiTop);
            pnlKpiContainer.Size = new Size(totalWidth - leftMargin - rightMargin, 104);

            int splitTop = pnlKpiContainer.Bottom + 16;
            int minContentHeight = _currentRole == UserRole.Admin ? 300 : 460;
            int splitHeight = Math.Max(minContentHeight, ClientSize.Height - splitTop - 24);

            if (_currentRole == UserRole.Admin)
            {
                pnlContentSplit.Visible = false;
                if (_pnlAdminContainer != null)
                {
                    _pnlAdminContainer.Visible = true;
                    _pnlAdminContainer.Location = new Point(leftMargin, splitTop);
                    _pnlAdminContainer.Size = new Size(totalWidth - leftMargin - rightMargin, splitHeight);
                    LayoutAdminControls();
                }
            }
            else
            {
                if (_pnlAdminContainer != null)
                {
                    _pnlAdminContainer.Visible = false;
                }
                pnlContentSplit.Visible = true;
                pnlContentSplit.Location = new Point(leftMargin, splitTop);
                pnlContentSplit.Size = new Size(totalWidth - leftMargin - rightMargin, splitHeight);

                ResizeListItems(pnlLeftList);
                ResizeListItems(pnlRightList);
            }

            AutoScrollMinSize = new Size(0, splitTop + splitHeight + 20);
        }

        private void RequestNavigation(string module)
        {
            if (NavigationRequested is not null)
            {
                NavigationRequested.Invoke(module);
                return;
            }

            if (FindForm() is MainForm form)
            {
                form.NavigateTo(module);
            }
        }
    }
}
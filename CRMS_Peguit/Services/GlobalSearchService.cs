using CRMS_Peguit.winforms.Controllers;

namespace CRMS_Peguit.winforms.Services
{
    /// <summary>
    /// A single cross-module search result returned by GlobalSearchService.
    /// </summary>
    public sealed record GlobalSearchResult(
        string Module,
        string Icon,
        string Title,
        string Subtitle,
        int RecordId
    );

    /// <summary>
    /// Lightweight in-memory cross-module search across Customers, Leads, Properties, and Deals.
    /// Runs against data already loaded by each module controller.
    /// </summary>
    public static class GlobalSearchService
    {
        /// <summary>
        /// Search all 4 modules. Returns up to <paramref name="maxPerModule"/> per module.
        /// Returns empty list if query is blank or under 2 characters.
        /// </summary>
        public static List<GlobalSearchResult> Search(string query, int maxPerModule = 5)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new List<GlobalSearchResult>();

            var results = new List<GlobalSearchResult>();

            // Customers
            try
            {
                var ctl = new CustomerController();
                foreach (var c in ctl.GetAll()
                    .Where(c => Hits(c.FullName, query) || Hits(c.Email, query) || Hits(c.Phone, query))
                    .Take(maxPerModule))
                {
                    results.Add(new GlobalSearchResult(
                        Module: "customers",
                        Icon: "👤",
                        Title: c.FullName,
                        Subtitle: $"{Cap(c.Type)} · {Cap(c.Status)}",
                        RecordId: c.CustomerId
                    ));
                }
            }
            catch { }

            // Leads
            try
            {
                var ctl = new LeadController();
                foreach (var l in ctl.GetAll()
                    .Where(l => Hits(l.FullName, query) || Hits(l.Email, query) || Hits(l.Phone, query))
                    .Take(maxPerModule))
                {
                    results.Add(new GlobalSearchResult(
                        Module: "leads",
                        Icon: "🎯",
                        Title: l.FullName,
                        Subtitle: $"{Cap(l.Stage)} · {(l.ExpectedValue.HasValue ? string.Format("₱{0:N2}", l.ExpectedValue.Value) : "No value")}",
                        RecordId: l.LeadId
                    ));
                }
            }
            catch { }

            // Properties
            try
            {
                var ctl = new PropertyController();
                foreach (var p in ctl.GetAll()
                    .Where(p => Hits(p.Address, query) || Hits(p.PropertyType, query) || Hits(p.Status, query))
                    .Take(maxPerModule))
                {
                    results.Add(new GlobalSearchResult(
                        Module: "properties",
                        Icon: "🏠",
                        Title: p.Address,
                        Subtitle: string.Format("{0} · ₱{1:N2}", Cap(p.PropertyType), p.Price),
                        RecordId: p.PropertyId
                    ));
                }
            }
            catch { }

            // Deals
            try
            {
                var ctl = new DealController();
                var deals = ctl.GetAll();
                var custNames = ctl.GetCustomerNames();   // Dictionary<int, string>
                var propAddresses = ctl.GetPropertyAddresses(); // Dictionary<int, string>
                foreach (var d in deals
                    .Where(d => Hits(GetName(custNames, d.CustomerId), query) ||
                                Hits(GetName(propAddresses, d.PropertyId), query) ||
                                Hits(d.Stage, query))
                    .Take(maxPerModule))
                {
                    results.Add(new GlobalSearchResult(
                        Module: "deals",
                        Icon: "🤝",
                        Title: GetName(custNames, d.CustomerId),
                        Subtitle: string.Format("Deal · {0} · ₱{1:N2}", Cap(d.Stage), d.Value),
                        RecordId: d.DealId
                    ));
                }
            }
            catch { }

            // Support Tickets
            try
            {
                var ctl = new SupportTicketController();
                foreach (var t in ctl.GetAll()
                    .Where(t => Hits(t.TicketNumber, query) ||
                                Hits(t.Category, query) ||
                                Hits(t.Description, query) ||
                                Hits(t.Customer?.FullName, query))
                    .Take(maxPerModule))
                {
                    results.Add(new GlobalSearchResult(
                        Module: "supporttickets",
                        Icon: "🎫",
                        Title: $"{t.TicketNumber} — {t.Category}",
                        Subtitle: $"{t.Customer?.FullName ?? "Client"} · {Cap(t.Status)} ({t.Priority})",
                        RecordId: t.TicketId
                    ));
                }
            }
            catch { }

            return results;
        }

        private static bool Hits(string? v, string q) =>
            !string.IsNullOrWhiteSpace(v) && v.Contains(q, StringComparison.OrdinalIgnoreCase);

        private static string Cap(string? s) =>
            string.IsNullOrWhiteSpace(s) ? "-" : char.ToUpper(s[0]) + s.Substring(1).ToLower();

        private static string GetName(Dictionary<int, string> dict, int id) =>
            dict.TryGetValue(id, out var name) ? name : "—";
    }
}
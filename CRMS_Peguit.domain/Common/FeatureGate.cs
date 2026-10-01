using System;
using System.Collections.Generic;
using System.Linq;
using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.domain.Common
{
    /// <summary>
    /// Metadata definition for a platform feature or module.
    /// </summary>
    public class FeatureDefinition
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool DefaultTenantA { get; set; }
        public bool DefaultTenantB { get; set; }
        public bool DefaultTenantC { get; set; }
    }

    /// <summary>
    /// Centralized subscription feature gating service.
    /// Provides consistent feature-level permissions across client UI and backend APIs.
    /// Supports dynamic runtime overrides configured by Super Admin via the Entitlements Matrix.
    /// </summary>
    public static class FeatureGate
    {
        // Feature Key Constants
        public const string DealsPipeline = "DealsPipeline";
        public const string DataCollection = "DataCollection";
        public const string PropertyInventory = "PropertyInventory";
        public const string LocalAndCloudSync = "LocalAndCloudSync";
        public const string RoleBasedAccess = "RoleBasedAccess";
        public const string CustomLogo = "CustomLogo";
        public const string BusinessIntelligence = "BusinessIntelligence";
        public const string ActionApprovals = "ActionApprovals";
        public const string DataExport = "DataExport";
        public const string MultiBranching = "MultiBranching";
        public const string AccentColor = "AccentColor";
        public const string HidePoweredBy = "HidePoweredBy";
        public const string DisasterRecovery = "DisasterRecovery";

        private static readonly object _lock = new();
        private static readonly Dictionary<string, bool> _dynamicOverrides = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// All registered system modules and their out-of-the-box tier entitlement defaults.
        /// </summary>
        public static readonly IReadOnlyList<FeatureDefinition> RegisteredFeatures = new List<FeatureDefinition>
        {
            new FeatureDefinition
            {
                Key = DealsPipeline,
                DisplayName = "Core Deals Pipeline",
                Description = "Deals tracking, pipeline stages, revenue calculations, and sales lifecycle",
                Category = "Sales & CRM",
                DefaultTenantA = true,
                DefaultTenantB = true,
                DefaultTenantC = true
            },
            new FeatureDefinition
            {
                Key = DataCollection,
                DisplayName = "Customer & Lead Management",
                Description = "Customer records, lead intake, contact details, and interaction logging",
                Category = "Sales & CRM",
                DefaultTenantA = true,
                DefaultTenantB = true,
                DefaultTenantC = true
            },
            new FeatureDefinition
            {
                Key = PropertyInventory,
                DisplayName = "Property & Inventory Catalog",
                Description = "Property listings, unit inventory, pricing, and availability statuses",
                Category = "Sales & CRM",
                DefaultTenantA = true,
                DefaultTenantB = true,
                DefaultTenantC = true
            },
            new FeatureDefinition
            {
                Key = LocalAndCloudSync,
                DisplayName = "Local SQLite & Cloud Sync",
                Description = "Offline-first resilient local database with seamless background cloud sync",
                Category = "Infrastructure",
                DefaultTenantA = true,
                DefaultTenantB = true,
                DefaultTenantC = true
            },
            new FeatureDefinition
            {
                Key = RoleBasedAccess,
                DisplayName = "Role-Based Access Control",
                Description = "Granular roles (Admin, Manager, Agent) and module-level permission enforcement",
                Category = "Security",
                DefaultTenantA = true,
                DefaultTenantB = true,
                DefaultTenantC = true
            },
            new FeatureDefinition
            {
                Key = CustomLogo,
                DisplayName = "Custom Business Logo",
                Description = "Tenant business logo branding displayed across main window and sidebar",
                Category = "White-Label",
                DefaultTenantA = true,
                DefaultTenantB = true,
                DefaultTenantC = true
            },
            new FeatureDefinition
            {
                Key = BusinessIntelligence,
                DisplayName = "BI Dashboard & Visual Analytics",
                Description = "Executive dashboard, visual charts, sales performance metrics, and KPI tracking",
                Category = "Analytics",
                DefaultTenantA = false,
                DefaultTenantB = true,
                DefaultTenantC = true
            },
            new FeatureDefinition
            {
                Key = ActionApprovals,
                DisplayName = "Managerial Action Approvals",
                Description = "Workflow approval hub for deals, assignments, and sensitive status transitions",
                Category = "Governance",
                DefaultTenantA = false,
                DefaultTenantB = true,
                DefaultTenantC = true
            },
            new FeatureDefinition
            {
                Key = DataExport,
                DisplayName = "PDF & Audit Trail Export",
                Description = "One-click export of deals, customer catalogs, and audit logs to PDF documents",
                Category = "Compliance",
                DefaultTenantA = false,
                DefaultTenantB = true,
                DefaultTenantC = true
            },
            new FeatureDefinition
            {
                Key = MultiBranching,
                DisplayName = "Multi-Branch Operations",
                Description = "Multiple business branch locations, live branch switching, and location scoping",
                Category = "Enterprise",
                DefaultTenantA = false,
                DefaultTenantB = false,
                DefaultTenantC = true
            },
            new FeatureDefinition
            {
                Key = AccentColor,
                DisplayName = "Custom UI Accent Color Theme",
                Description = "Customized primary color theme for buttons, headers, and UI elements",
                Category = "White-Label",
                DefaultTenantA = false,
                DefaultTenantB = false,
                DefaultTenantC = true
            },
            new FeatureDefinition
            {
                Key = HidePoweredBy,
                DisplayName = "White-Label 'Powered by NEXA' Removal",
                Description = "Completely remove the 'Powered by NEXA' platform footer badge for clean branding",
                Category = "White-Label",
                DefaultTenantA = false,
                DefaultTenantB = false,
                DefaultTenantC = true
            },
            new FeatureDefinition
            {
                Key = DisasterRecovery,
                DisplayName = "Database Snapshots & Restore Points",
                Description = "Manual and automated local database snapshot backups and point-in-time recovery",
                Category = "Disaster Recovery",
                DefaultTenantA = false,
                DefaultTenantB = false,
                DefaultTenantC = true
            }
        };

        public static string GetMatrixKey(TenantTier tier, string featureKey) => $"{tier}:{featureKey}";

        /// <summary>
        /// Applies runtime matrix overrides (typically loaded from MasterDb GlobalSettings).
        /// </summary>
        public static void SetOverrides(IDictionary<string, bool>? matrix)
        {
            lock (_lock)
            {
                _dynamicOverrides.Clear();
                if (matrix != null)
                {
                    foreach (var kvp in matrix)
                    {
                        _dynamicOverrides[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Clears all dynamic matrix overrides, reverting to standard factory tier defaults.
        /// </summary>
        public static void ClearOverrides()
        {
            lock (_lock)
            {
                _dynamicOverrides.Clear();
            }
        }

        /// <summary>
        /// Gets a snapshot of the current dynamic matrix overrides.
        /// </summary>
        public static Dictionary<string, bool> GetCurrentMatrix()
        {
            lock (_lock)
            {
                var dict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

                // Populate with default values first
                foreach (var feature in RegisteredFeatures)
                {
                    dict[GetMatrixKey(TenantTier.TenantA, feature.Key)] = feature.DefaultTenantA;
                    dict[GetMatrixKey(TenantTier.TenantB, feature.Key)] = feature.DefaultTenantB;
                    dict[GetMatrixKey(TenantTier.TenantC, feature.Key)] = feature.DefaultTenantC;
                }

                // Apply active overrides
                foreach (var kvp in _dynamicOverrides)
                {
                    dict[kvp.Key] = kvp.Value;
                }

                return dict;
            }
        }

        /// <summary>
        /// Evaluates whether a tenant tier is entitled to use a specific feature.
        /// Master tier always retains unconditional access to all modules.
        /// </summary>
        public static bool IsFeatureEnabled(TenantTier tier, string featureKey)
        {
            if (string.IsNullOrWhiteSpace(featureKey)) return false;

            // Master platform tier always has access to all platform features
            if (tier == TenantTier.Master) return true;

            string matrixKey = GetMatrixKey(tier, featureKey);

            lock (_lock)
            {
                if (_dynamicOverrides.TryGetValue(matrixKey, out bool isOverridden))
                {
                    return isOverridden;
                }
            }

            // Fallback to standard tier default
            var def = RegisteredFeatures.FirstOrDefault(f => f.Key.Equals(featureKey, StringComparison.OrdinalIgnoreCase));
            if (def != null)
            {
                return tier switch
                {
                    TenantTier.TenantA => def.DefaultTenantA,
                    TenantTier.TenantB => def.DefaultTenantB,
                    TenantTier.TenantC => def.DefaultTenantC,
                    _ => false
                };
            }

            // Fallback for legacy branding keys if not matching above
            return featureKey switch
            {
                CustomLogo => true,
                AccentColor => tier >= TenantTier.TenantC,
                HidePoweredBy => tier >= TenantTier.TenantC,
                _ => false
            };
        }

        // Direct convenience accessors
        public static bool CanAccessDealsPipeline(TenantTier tier) => IsFeatureEnabled(tier, DealsPipeline);
        public static bool CanAccessDataCollection(TenantTier tier) => IsFeatureEnabled(tier, DataCollection);
        public static bool CanAccessPropertyInventory(TenantTier tier) => IsFeatureEnabled(tier, PropertyInventory);
        public static bool CanAccessBusinessIntelligence(TenantTier tier) => IsFeatureEnabled(tier, BusinessIntelligence);
        public static bool CanAccessActions(TenantTier tier) => IsFeatureEnabled(tier, ActionApprovals);
        public static bool CanAccessBranching(TenantTier tier) => IsFeatureEnabled(tier, MultiBranching);
        public static bool CanUseCustomLogo(TenantTier tier) => IsFeatureEnabled(tier, CustomLogo);
        public static bool CanUseAccentColor(TenantTier tier) => IsFeatureEnabled(tier, AccentColor);
        public static bool CanHidePoweredBy(TenantTier tier) => IsFeatureEnabled(tier, HidePoweredBy);
        public static bool CanExportData(TenantTier tier) => IsFeatureEnabled(tier, DataExport);
        public static bool CanAccessDisasterRecovery(TenantTier tier) => IsFeatureEnabled(tier, DisasterRecovery);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace CRMS_Peguit.winforms.Controls
{
    /// <summary>
    /// Enforces the NEXA single-active-filter rule on a per-screen basis:
    /// "ONE active filter per screen at a time — no multi-select filtering,
    /// no combining a KPI-filter with a chart-filter with a manual dropdown
    /// filter simultaneously; whichever is activated most recently clears the others."
    ///
    /// Each Case 1 screen creates ONE ScreenFilterCoordinator during initialization,
    /// registers all its KpiCard and ChartWrapperControl instances, and wires
    /// the FilterChanged event to apply/clear the grid filter.
    ///
    /// Usage:
    ///   _filterCoord = new ScreenFilterCoordinator();
    ///   _filterCoord.Register(kpi1, kpi2, kpi3, kpi4);
    ///   _filterCoord.Register(chartWrapper);
    ///   _filterCoord.FilterChanged += (source, filterKey) => ApplyGridFilter(filterKey);
    /// </summary>
    public sealed class ScreenFilterCoordinator
    {
        private readonly List<KpiCard> _kpiCards = new();
        private readonly List<ChartWrapperControl> _charts = new();

        private object? _activeSource;
        private string? _activeFilterKey;

        /// <summary>
        /// Fires when the active filter changes. Parameters:
        /// - source: The KpiCard or ChartWrapperControl that activated the filter (null if cleared).
        /// - filterKey: The filter key string to apply to the grid (null if filter cleared).
        /// </summary>
        public event Action<object?, string?>? FilterChanged;

        /// <summary>
        /// The currently active filter key, or null if no filter is active.
        /// </summary>
        public string? ActiveFilterKey => _activeFilterKey;

        /// <summary>
        /// Whether any filter is currently active on this screen.
        /// </summary>
        public bool IsFilterActive => _activeFilterKey != null;

        /// <summary>
        /// Registers one or more KpiCards for coordinated single-active-filter enforcement.
        /// Each card's click is intercepted: activating one deselects all others.
        /// </summary>
        public void Register(params KpiCard[] cards)
        {
            foreach (var card in cards)
            {
                if (_kpiCards.Contains(card)) continue;

                _kpiCards.Add(card);
                card.ClickMode = KpiClickMode.InPlaceFilter;
                card.AutoToggleOnFilterClick = true;

                // Capture card reference for the closure
                var capturedCard = card;
                card.SetAction(() => OnKpiCardClicked(capturedCard));
            }
        }

        /// <summary>
        /// Registers a ChartWrapperControl for coordinated single-active-filter enforcement.
        /// When the chart's in-place filter activates, all KPI card selections clear, and vice versa.
        /// </summary>
        public void Register(ChartWrapperControl chart)
        {
            if (_charts.Contains(chart)) return;

            _charts.Add(chart);
            chart.SetMode(KpiClickMode.InPlaceFilter);

            chart.InPlaceFilterChanged += (filterKey) => OnChartFilterChanged(chart, filterKey);
        }

        /// <summary>
        /// Programmatically activates a filter from an external source (e.g., a manual dropdown).
        /// Clears all KPI and chart selections before applying.
        /// </summary>
        public void SetActive(object source, string? filterKey)
        {
            if (filterKey == null)
            {
                ClearAll();
                return;
            }

            ClearAllVisuals(source);
            _activeSource = source;
            _activeFilterKey = filterKey;
            FilterChanged?.Invoke(source, filterKey);
        }

        /// <summary>
        /// Clears all active filters across all registered components on this screen.
        /// </summary>
        public void ClearAll()
        {
            _activeSource = null;
            _activeFilterKey = null;

            foreach (var card in _kpiCards)
            {
                card.SetSelected(false);
            }

            foreach (var chart in _charts)
            {
                chart.ClearFilter();
            }

            FilterChanged?.Invoke(null, null);
        }

        // =========================================================================
        // INTERNAL COORDINATION
        // =========================================================================

        private void OnKpiCardClicked(KpiCard clickedCard)
        {
            if (clickedCard.IsSelected)
            {
                // Card toggled ON — it's the new active filter
                ClearAllVisuals(clickedCard);
                _activeSource = clickedCard;
                _activeFilterKey = clickedCard.FilterKey;
                FilterChanged?.Invoke(clickedCard, clickedCard.FilterKey);
            }
            else
            {
                // Card toggled OFF — clear everything
                ClearAllVisuals(null);
                _activeSource = null;
                _activeFilterKey = null;
                FilterChanged?.Invoke(null, null);
            }
        }

        private void OnChartFilterChanged(ChartWrapperControl chart, string? filterKey)
        {
            if (filterKey != null)
            {
                // Chart filter activated — clear all KPI selections
                ClearAllVisuals(chart);
                _activeSource = chart;
                _activeFilterKey = filterKey;
                FilterChanged?.Invoke(chart, filterKey);
            }
            else
            {
                // Chart filter cleared
                _activeSource = null;
                _activeFilterKey = null;
                FilterChanged?.Invoke(null, null);
            }
        }

        /// <summary>
        /// Deselects all registered components except the specified source.
        /// </summary>
        private void ClearAllVisuals(object? exceptSource)
        {
            foreach (var card in _kpiCards)
            {
                if (!ReferenceEquals(card, exceptSource))
                {
                    card.SetSelected(false);
                }
            }

            foreach (var chart in _charts)
            {
                if (!ReferenceEquals(chart, exceptSource))
                {
                    chart.ClearFilter();
                }
            }
        }
    }
}

// // Copyright (c) Microsoft Corporation.
// // Licensed under the MIT License.

using EventLogExpert.Library.Models;
using EventLogExpert.Store.FilterPane;
using Microsoft.AspNetCore.Components;
using System.Linq.Dynamic.Core;

namespace EventLogExpert.Components;

public partial class FilterPane
{
    private readonly FilterDateModel _model = new();

    private Timer? _advancedFilterDebounceTimer = null;
    private string _advancedFilterErrorMessage = string.Empty;
    private string? _advancedFilterValue = null;
    private bool _canEditAdvancedFilter = true;
    private bool _canEditDate = true;
    private bool _isDateFilterVisible;
    private bool _isAdvancedFilterValid;
    private bool _isAdvancedFilterVisible;
    private bool _isFilterListVisible;

    private string MenuState
    {
        get
        {
            if (FilterPaneState.Value.CurrentFilters.Any() || _isDateFilterVisible || _isAdvancedFilterVisible)
            {
                return _isFilterListVisible.ToString().ToLower();
            }

            return "false";
        }
    }

    private void AddDateFilter()
    {
        _isFilterListVisible = true;
        _canEditDate = true;

        // Offset by 1 minute to make sure we don't drop events
        // since HTML input DateTime does not go lower than minutes
        _model.Before = EventLogState.Value.Events.FirstOrDefault()?.TimeCreated
                .AddMinutes(1).ConvertTimeZone(SettingsState.Value.TimeZone) ??
            DateTime.Now;

        _model.After = EventLogState.Value.Events.LastOrDefault()?.TimeCreated
                .AddMinutes(-1).ConvertTimeZone(SettingsState.Value.TimeZone) ??
            DateTime.Now;

        _isDateFilterVisible = true;
    }

    private void AddFilter()
    {
        Dispatcher.Dispatch(new FilterPaneAction.AddFilter());
        _isFilterListVisible = true;
    }

    private void AddAdvancedFilter()
    {
        _isFilterListVisible = true;
        _canEditAdvancedFilter = true;
        _isAdvancedFilterVisible = true;
    }

    private void AdvancedFilterChanged(ChangeEventArgs e)
    {
        _advancedFilterValue = e.Value as string;
        if (_advancedFilterDebounceTimer != null)
        {
            _advancedFilterDebounceTimer.Dispose();
        }

        _advancedFilterDebounceTimer = new(s =>
        {
            _isAdvancedFilterValid = TryParseExpression(s as string, out var message);
            _advancedFilterErrorMessage = message;
            this.InvokeAsync(() => StateHasChanged());
        }, e.Value as string, 250, 0);
    }

    private void ApplyAdvancedFilter()
    {
        if (_advancedFilterValue != null && TryParseExpression(_advancedFilterValue, out var message))
        {
            _canEditAdvancedFilter = false;
            Dispatcher.Dispatch(new FilterPaneAction.SetAdvancedFilter(_advancedFilterValue));
        }
    }

    private void EditAdvancedFilter() => _canEditAdvancedFilter = true;

    private void RemoveAdvancedFilter()
    {
        Dispatcher.Dispatch(new FilterPaneAction.SetAdvancedFilter(string.Empty));
        _isAdvancedFilterVisible = false;
    }

    private void ApplyDateFilter()
    {
        FilterDateModel model = new()
        {
            After = _model.After.ToUniversalTime(), Before = _model.Before.ToUniversalTime()
        };

        Dispatcher.Dispatch(new FilterPaneAction.SetFilterDateRange(model));
        _canEditDate = false;
    }

    private void RemoveDateFilter()
    {
        Dispatcher.Dispatch(new FilterPaneAction.SetFilterDateRange(null));
        _isDateFilterVisible = false;
    }

    private void EditDateFilter() => _canEditDate = true;

    private void ToggleMenu() => _isFilterListVisible = !_isFilterListVisible;

    private bool TryParseExpression(string? expression, out string message)
    {
        message = string.Empty;

        if (string.IsNullOrEmpty(expression)) return false;

        var testQueryable = new List<DisplayEventModel>();
        try
        {
            var result = testQueryable.AsQueryable().Where(expression).ToList();
            return true;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return false;
        }
    }
}

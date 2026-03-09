using Microsoft.AspNetCore.Components;

#pragma warning disable IDE0011

namespace Bud.BlazorWasm.Components.Common;

public partial class DateRangePicker
{
    [Parameter] public DateTime StartDate { get; set; }
    [Parameter] public DateTime EndDate { get; set; }
    [Parameter] public EventCallback<DateTime> StartDateChanged { get; set; }
    [Parameter] public EventCallback<DateTime> EndDateChanged { get; set; }

    private bool _isOpen;
    private DateTime _viewDate;
    private DateTime? _selectionStart;
    private DateTime? _hoverDate;

    protected override void OnParametersSet()
    {
        if (!_isOpen)
        {
            _viewDate = new DateTime(StartDate.Year, StartDate.Month, 1);
        }
    }

    private void ToggleCalendar()
    {
        _isOpen = !_isOpen;
        if (_isOpen)
        {
            _viewDate = new DateTime(StartDate.Year, StartDate.Month, 1);
            _selectionStart = null;
            _hoverDate = null;
        }
    }

    private void PreviousMonth() => _viewDate = _viewDate.AddMonths(-1);
    private void NextMonth() => _viewDate = _viewDate.AddMonths(1);

    private async Task SelectDay(DateTime day)
    {
        if (_selectionStart == null)
        {
            _selectionStart = day;
        }
        else
        {
            var start = _selectionStart.Value;
            var end = day;
            if (end < start) (start, end) = (end, start);

            _selectionStart = null;
            _hoverDate = null;
            _isOpen = false;

            await StartDateChanged.InvokeAsync(start);
            await EndDateChanged.InvokeAsync(end);
        }
    }

    private void HoverDay(DateTime day)
    {
        if (_selectionStart != null) _hoverDate = day;
    }

    private List<DateTime?> GetCalendarDays()
    {
        var days = new List<DateTime?>();
        var firstDay = _viewDate;
        var daysInMonth = DateTime.DaysInMonth(firstDay.Year, firstDay.Month);
        var startDow = (int)firstDay.DayOfWeek;

        for (var i = 0; i < startDow; i++) days.Add(null);
        for (var d = 1; d <= daysInMonth; d++) days.Add(new DateTime(firstDay.Year, firstDay.Month, d));
        return days;
    }

    private string GetDayCss(DateTime day)
    {
        var classes = new List<string>();

        if (_selectionStart != null && _hoverDate != null)
        {
            var rangeStart = _selectionStart.Value < _hoverDate.Value ? _selectionStart.Value : _hoverDate.Value;
            var rangeEnd = _selectionStart.Value < _hoverDate.Value ? _hoverDate.Value : _selectionStart.Value;

            if (day == _selectionStart.Value) classes.Add("range-edge");
            else if (day == _hoverDate.Value) classes.Add("range-edge");
            else if (day > rangeStart && day < rangeEnd) classes.Add("in-range");
        }
        else if (_selectionStart == null)
        {
            if (day == StartDate.Date) classes.Add("range-edge");
            else if (day == EndDate.Date) classes.Add("range-edge");
            else if (day > StartDate.Date && day < EndDate.Date) classes.Add("in-range");
        }

        if (day == DateTime.Today) classes.Add("today");

        return string.Join(" ", classes);
    }

    private string FormatRange()
    {
        return $"{StartDate:dd/MM/yyyy} - {EndDate:dd/MM/yyyy}";
    }
}

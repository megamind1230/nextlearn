using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NextLearn.Desktop.Models;

#pragma warning disable SA1402 // File may only contain a single type

public class FocusTimerData
{
    public List<TodoItem> Todos { get; set; } = [];

    public List<PomodoroLogEntry> Log { get; set; } = [];
}

public partial class TodoItem : ObservableObject
{
    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private bool _done;
}

public class PomodoroLogEntry
{
    public string Timestamp { get; set; } = string.Empty;

    public string Phase { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public string Tasks { get; set; } = string.Empty;
}

#pragma warning restore SA1402

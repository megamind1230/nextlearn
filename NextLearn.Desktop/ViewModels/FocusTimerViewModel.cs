using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextLearn.Desktop.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#pragma warning disable CA1031

namespace NextLearn.Desktop.ViewModels;

public partial class FocusTimerViewModel : ViewModelBase
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private readonly string _filePath;
    private DispatcherTimer? _timer;
    private int _sessionWorkMinutes;

    [ObservableProperty]
    private int _workDuration = 25;

    [ObservableProperty]
    private int _breakDuration = 5;

    [ObservableProperty]
    private int _remainingSeconds = 1500;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotRunning))]
    private bool _isRunning;

    public bool IsNotRunning => !IsRunning;

    [ObservableProperty]
    private string _phase = "READY";

    [ObservableProperty]
    private ObservableCollection<TodoItem> _todos = [];

    [ObservableProperty]
    private ObservableCollection<PomodoroLogEntry> _sessionLog = [];

    [ObservableProperty]
    private string _newTodoText = string.Empty;

    [ObservableProperty]
    private TodoItem? _editingItem;

    public bool IsEditing => EditingItem is not null;

    public string AddButtonText => IsEditing ? "Save" : "Add";

    partial void OnEditingItemChanged(TodoItem? value)
    {
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(AddButtonText));
    }

    [ObservableProperty]
    private string _logContent = string.Empty;

    [ObservableProperty]
    private bool _isTimerTabSelected = true;

    [ObservableProperty]
    private bool _isTasksTabSelected;

    [ObservableProperty]
    private bool _isLogTabSelected;

    public bool HasActiveTodos => ActiveTodos.Count > 0;

    public bool HasCompletedTodos => CompletedTodos.Count > 0;

    public bool HasNoTodos => Todos.Count == 0;

    public ObservableCollection<TodoItem> ActiveTodos { get; } = [];

    public ObservableCollection<TodoItem> CompletedTodos { get; } = [];

    public string RemainingDisplay => $"{RemainingSeconds / 60:D2}:{RemainingSeconds % 60:D2}";

    partial void OnRemainingSecondsChanged(int value) => OnPropertyChanged(nameof(RemainingDisplay));

    partial void OnWorkDurationChanged(int value)
    {
        if (!IsRunning && Phase is "READY" or "WORK")
        {
            RemainingSeconds = value * 60;
        }
    }

    partial void OnBreakDurationChanged(int value)
    {
        if (!IsRunning && Phase == "BREAK")
        {
            RemainingSeconds = value * 60;
        }
    }

    public FocusTimerViewModel()
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "nextlearn");
        Directory.CreateDirectory(configDir);
        _filePath = Path.Combine(configDir, "focus-timer.yaml");
        Load();
    }

    public void SetDefaults(int workMin, int breakMin)
    {
        WorkDuration = workMin;
        BreakDuration = breakMin;
    }

    [RelayCommand]
    private void StartTimer()
    {
        if (IsRunning)
        {
            return;
        }

        if (RemainingSeconds <= 0)
        {
            RemainingSeconds = Phase == "BREAK" ? BreakDuration * 60 : WorkDuration * 60;
        }

        if (Phase == "READY")
        {
            Phase = "WORK";
        }

        _sessionWorkMinutes = WorkDuration;
        IsRunning = true;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    [RelayCommand]
    private void PauseTimer()
    {
        IsRunning = false;
        _timer?.Stop();
        _timer = null;
    }

    [RelayCommand]
    private void ResetTimer()
    {
        PauseTimer();
        Phase = "READY";
        RemainingSeconds = WorkDuration * 60;
    }

    private static void PlayTimerSound()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "timer-sound.mp3");
            if (File.Exists(path))
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo(path)
                {
                    UseShellExecute = true,
                };
                process.Start();
            }
        }
        catch
        {
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (RemainingSeconds > 0)
        {
            RemainingSeconds--;
            return;
        }

        _timer?.Stop();
        IsRunning = false;

        if (Phase == "WORK")
        {
            var activeTasks = Todos.Where(t => !t.Done).Select(t => t.Text).ToList();
            var tasksStr = activeTasks.Count > 0 ? string.Join(", ", activeTasks) : "(no tasks)";
            SessionLog.Add(new PomodoroLogEntry
            {
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Phase = "Work",
                DurationMinutes = _sessionWorkMinutes,
                Tasks = tasksStr,
            });
            Save();
            RefreshLogContent();
            PlayTimerSound();

            Phase = "BREAK";
            RemainingSeconds = BreakDuration * 60;
            IsRunning = true;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }
        else
        {
            Phase = "READY";
            RemainingSeconds = WorkDuration * 60;
        }
    }

    [RelayCommand]
    private void AddTodo()
    {
        if (string.IsNullOrWhiteSpace(NewTodoText))
        {
            return;
        }

        if (EditingItem is not null)
        {
            EditingItem.Text = NewTodoText.Trim();
            EditingItem = null;
        }
        else
        {
            Todos.Add(new TodoItem { Text = NewTodoText.Trim(), Done = false });
        }

        NewTodoText = string.Empty;
        RefreshTodoLists();
        Save();
    }

    [RelayCommand]
    private void ToggleTodo(TodoItem? item)
    {
        if (item == null)
        {
            return;
        }

        item.Done = !item.Done;
        RefreshTodoLists();
        Save();
    }

    [RelayCommand]
    private void DeleteTodo(TodoItem? item)
    {
        if (item == null)
        {
            return;
        }

        if (EditingItem == item)
        {
            EditingItem = null;
            NewTodoText = string.Empty;
        }

        Todos.Remove(item);
        RefreshTodoLists();
        Save();
    }

    [RelayCommand]
    private void StartEditTodo(TodoItem? item)
    {
        if (item == null)
        {
            return;
        }

        NewTodoText = item.Text;
        EditingItem = item;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        NewTodoText = string.Empty;
        EditingItem = null;
    }

    private void RefreshTodoLists()
    {
        ActiveTodos.Clear();
        CompletedTodos.Clear();
        foreach (var t in Todos)
        {
            if (t.Done)
            {
                CompletedTodos.Add(t);
            }
            else
            {
                ActiveTodos.Add(t);
            }
        }

        OnPropertyChanged(nameof(HasActiveTodos));
        OnPropertyChanged(nameof(HasCompletedTodos));
        OnPropertyChanged(nameof(HasNoTodos));
    }

    partial void OnTodosChanged(ObservableCollection<TodoItem> value)
    {
        RefreshTodoLists();
    }

    [RelayCommand]
    private void SelectTimerTab()
    {
        IsTimerTabSelected = true;
        IsTasksTabSelected = false;
        IsLogTabSelected = false;
    }

    [RelayCommand]
    private void SelectTasksTab()
    {
        IsTimerTabSelected = false;
        IsTasksTabSelected = true;
        IsLogTabSelected = false;
    }

    [RelayCommand]
    private void SelectLogTab()
    {
        IsTimerTabSelected = false;
        IsTasksTabSelected = false;
        IsLogTabSelected = true;
        RefreshLogContent();
    }

    private void RefreshLogContent()
    {
        if (SessionLog.Count == 0)
        {
            LogContent = "No completed sessions yet. Start a timer to begin.";
            return;
        }

        LogContent = string.Join("\n", SessionLog.Select(entry =>
            $"{entry.Timestamp} — {entry.Phase} {entry.DurationMinutes}m — {entry.Tasks}"));
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var yaml = File.ReadAllText(_filePath);
                var data = YamlDeserializer.Deserialize<FocusTimerData>(yaml);
                if (data != null)
                {
                    Todos = new ObservableCollection<TodoItem>(data.Todos ?? []);
                    SessionLog = new ObservableCollection<PomodoroLogEntry>(data.Log ?? []);
                }
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or IOException or YamlDotNet.Core.YamlException)
        {
            Serilog.Log.Error(ex, "Failed to load focus timer data");
        }

        RefreshLogContent();
    }

    private void Save()
    {
        try
        {
            var data = new FocusTimerData
            {
                Todos = [.. Todos],
                Log = [.. SessionLog],
            };
            var yaml = YamlSerializer.Serialize(data);
            File.WriteAllText(_filePath, yaml);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or YamlDotNet.Core.YamlException)
        {
            Serilog.Log.Error(ex, "Failed to save focus timer data");
        }
    }
}

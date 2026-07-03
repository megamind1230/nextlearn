using System;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextLearn.Desktop.Services;
using Serilog;

#pragma warning disable CA2007
#pragma warning disable CA1031

namespace NextLearn.Desktop.ViewModels;

public partial class McqPanelViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly McqFileService _mcqFileService;

    [ObservableProperty]
    private bool _isGenerateTabSelected = true;

    [ObservableProperty]
    private bool _isQuizTabSelected;

    [ObservableProperty]
    private bool _isLogTabSelected;

    [ObservableProperty]
    private bool _isQuizActive;

    [ObservableProperty]
    private McqGenerationViewModel _generation;

    [ObservableProperty]
    private McqQuizViewModel _quiz;

    [ObservableProperty]
    private ObservableCollection<McqFileInfo> _savedMcqFiles = [];

    [ObservableProperty]
    private McqFileInfo? _selectedMcqFile;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _hasSavedFiles;

    [ObservableProperty]
    private bool _hasNoSavedFiles = true;

    [ObservableProperty]
    private string _logContent = string.Empty;

    public bool IsNotQuizActive => !IsQuizActive;

    partial void OnIsQuizActiveChanged(bool value) => OnPropertyChanged(nameof(IsNotQuizActive));

    partial void OnSavedMcqFilesChanged(ObservableCollection<McqFileInfo> value)
    {
        HasSavedFiles = value.Count > 0;
        HasNoSavedFiles = value.Count == 0;
    }

    public McqPanelViewModel(
        ISettingsService settingsService,
        McqFileService mcqFileService,
        McqGenerationViewModel generation,
        McqQuizViewModel quiz)
    {
        _settingsService = settingsService;
        _mcqFileService = mcqFileService;
        _generation = generation;
        _quiz = quiz;

        _quiz.QuizFinished += OnQuizFinished;
        EnsureLogFileExists();
    }

    private static void EnsureLogFileExists()
    {
        try
        {
            var configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "nextlearn");
            var logPath = Path.Combine(configDir, ".nextlearn-mcq-results.org");
            if (!File.Exists(logPath))
            {
                Directory.CreateDirectory(configDir);
                var header = "#+TITLE: MCQ Quiz Results\n\n"
                    + "| Timestamp | Score | Title | Source Deck | Questions | Time Taken |\n"
                    + "|-----------+-------+-------+-------------+-----------+------------|\n";
                File.WriteAllText(logPath, header);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to ensure MCQ result log file exists");
        }
    }

    public void LoadMcqFiles()
    {
        var mcqsPath = _settingsService.ResolvedMcqsPath;
        var files = _mcqFileService.ListMcqFiles(mcqsPath);
        SavedMcqFiles = new ObservableCollection<McqFileInfo>(files);
    }

    private void OnQuizFinished(object? sender, McqQuizResultArgs args)
    {
        try
        {
            var configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "nextlearn");
            var logPath = Path.Combine(configDir, ".nextlearn-mcq-results.org");
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            McqResultLogger.LogResult(
                logPath,
                timestamp,
                args.Score,
                args.Title,
                args.SourceDeck,
                args.TotalCount,
                args.TimeTaken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to log MCQ quiz result");
        }
    }

    private void LoadLog()
    {
        try
        {
            var configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "nextlearn");
            var logPath = Path.Combine(configDir, ".nextlearn-mcq-results.org");
            LogContent = McqResultLogger.ReadLog(logPath);

            if (string.IsNullOrEmpty(LogContent))
            {
                LogContent = "No quiz results yet. Complete a quiz to see your history here.";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load MCQ result log");
            LogContent = "Failed to load quiz log.";
        }
    }

    [RelayCommand]
    private void SelectGenerateTab()
    {
        IsGenerateTabSelected = true;
        IsQuizTabSelected = false;
        IsLogTabSelected = false;
    }

    [RelayCommand]
    private void SelectQuizTab()
    {
        IsGenerateTabSelected = false;
        IsQuizTabSelected = true;
        IsLogTabSelected = false;
        LoadMcqFiles();
    }

    [RelayCommand]
    private void SelectLogTab()
    {
        IsGenerateTabSelected = false;
        IsQuizTabSelected = false;
        IsLogTabSelected = true;
        LoadLog();
    }

    [RelayCommand]
    private void StartQuiz(McqFileInfo? file)
    {
        SelectedMcqFile = file;
        if (SelectedMcqFile == null)
        {
            return;
        }

        HasError = false;
        ErrorMessage = null;

        if (Quiz.StartQuiz(SelectedMcqFile.FilePath))
        {
            IsQuizActive = true;
        }
        else
        {
            HasError = true;
            ErrorMessage = "Failed to load quiz file. It may be empty or corrupted.";
        }
    }

    [RelayCommand]
    private void QuitQuiz()
    {
        Quiz.StopQuiz();
        IsQuizActive = false;
        LoadMcqFiles();
    }

    [RelayCommand]
    private void DeleteMcqFile()
    {
        if (SelectedMcqFile == null)
        {
            return;
        }

        _mcqFileService.DeleteMcq(SelectedMcqFile.FilePath);
        LoadMcqFiles();

        if (SavedMcqFiles.Count > 0)
        {
            SelectedMcqFile = SavedMcqFiles[0];
        }
    }
}

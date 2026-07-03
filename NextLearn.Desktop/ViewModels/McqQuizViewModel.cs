using System;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextLearn.Desktop.Models;
using NextLearn.Desktop.Services;
using Serilog;

#pragma warning disable CA2007
#pragma warning disable CA1031

namespace NextLearn.Desktop.ViewModels;

public partial class McqQuizViewModel : ViewModelBase
{
    private readonly McqFileService _mcqFileService;
    private readonly ISettingsService _settingsService;
    private McqDocument? _document;
    private DispatcherTimer? _timer;

    [ObservableProperty]
    private string? _currentQuestionHtml;

    [ObservableProperty]
    private int _currentIndex;

    [ObservableProperty]
    private int _totalQuestions;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private string? _reviewHtml;

    [ObservableProperty]
    private TimeSpan _elapsed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLastQuestion))]
    private bool _isTimerRunning;

    [ObservableProperty]
    private string _elapsedDisplay = "0:00";

    [ObservableProperty]
    private bool _canGoNext;

    [ObservableProperty]
    private bool _canGoPrev;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private string? _errorText;

    [ObservableProperty]
    private bool _hasError;

    public bool IsLastQuestion => CurrentIndex == TotalQuestions - 1 && TotalQuestions > 0;

    public bool ShowSeeResults => IsLastQuestion && !IsCompleted;

    public event EventHandler<McqQuizResultArgs>? QuizFinished;

    public McqQuizViewModel(McqFileService mcqFileService, ISettingsService settingsService)
    {
        _mcqFileService = mcqFileService;
        _settingsService = settingsService;
    }

    partial void OnCurrentIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsLastQuestion));
        OnPropertyChanged(nameof(ShowSeeResults));
    }

    partial void OnTotalQuestionsChanged(int value)
    {
        OnPropertyChanged(nameof(IsLastQuestion));
        OnPropertyChanged(nameof(ShowSeeResults));
    }

    public bool StartQuiz(string filePath)
    {
        try
        {
            _document = _mcqFileService.LoadMcq(filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load MCQ file: {Path}", filePath);
            return false;
        }

        if (_document == null || _document.Questions.Count == 0)
        {
            Log.Error("MCQ file has no questions: {Path}", filePath);
            return false;
        }

        _document.Questions.ForEach(q => q.SelectedIndex = null);
        CurrentIndex = 0;
        TotalQuestions = _document.Questions.Count;
        IsCompleted = false;
        ReviewHtml = null;
        Elapsed = TimeSpan.Zero;
        ElapsedDisplay = "0:00";
        HasError = false;
        ErrorText = null;

        StartTimer();
        ShowCurrentQuestion();
        return true;
    }

    public void StopQuiz()
    {
        StopTimer();
        IsCompleted = false;
        CurrentQuestionHtml = null;
        ReviewHtml = null;
        _document = null;
        HasError = false;
        ErrorText = null;
    }

    private void StartTimer()
    {
        StopTimer();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
        IsTimerRunning = true;
    }

    private void StopTimer()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }

        IsTimerRunning = false;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        Elapsed = Elapsed.Add(TimeSpan.FromSeconds(1));
        ElapsedDisplay = $"{(int)Elapsed.TotalMinutes}:{Elapsed.Seconds:D2}";
    }

    private void ShowCurrentQuestion()
    {
        if (_document == null || _document.Questions.Count == 0)
        {
            return;
        }

        var question = _document.Questions[CurrentIndex];
        CurrentQuestionHtml = McqQuizHtmlBuilder.BuildQuestionHtml(
            question, CurrentIndex, TotalQuestions, Elapsed, showAnswer: false, _settingsService.KeyBindingsProfile);

        CanGoNext = !IsLastQuestion;
        CanGoPrev = CurrentIndex > 0;
        ProgressText = $"Question {CurrentIndex + 1} of {TotalQuestions}";
        HasError = false;
        ErrorText = null;
    }

    [RelayCommand]
    private void AnswerQuestion(string letter)
    {
        if (_document == null || IsCompleted)
        {
            return;
        }

        var index = letter.ToUpperInvariant() switch
        {
            "A" => 0,
            "B" => 1,
            "C" => 2,
            "D" => 3,
            _ => -1,
        };

        if (index < 0 || index >= _document.Questions[CurrentIndex].Options.Count)
        {
            return;
        }

        _document.Questions[CurrentIndex].SelectedIndex = index;
        ShowCurrentQuestion();
    }

    [RelayCommand]
    private void NextQuestion()
    {
        if (_document == null || IsCompleted)
        {
            return;
        }

        if (!_document.Questions[CurrentIndex].IsAnswered)
        {
            _document.Questions[CurrentIndex].SelectedIndex = null;
        }

        if (CurrentIndex < TotalQuestions - 1)
        {
            CurrentIndex++;
            ShowCurrentQuestion();
        }
    }

    [RelayCommand]
    private void PreviousQuestion()
    {
        if (_document == null || IsCompleted)
        {
            return;
        }

        if (CurrentIndex > 0)
        {
            CurrentIndex--;
            ShowCurrentQuestion();
        }
    }

    [RelayCommand]
    private void SeeResults()
    {
        if (_document == null)
        {
            return;
        }

        if (_document.Questions.Any(q => !q.IsAnswered))
        {
            HasError = true;
            ErrorText = "Please answer all questions before viewing results.";
            return;
        }

        StopTimer();
        IsCompleted = true;
        ReviewHtml = McqQuizHtmlBuilder.BuildReviewHtml(_document.Questions, Elapsed, _settingsService.KeyBindingsProfile);
        CurrentQuestionHtml = null;

        var correct = _document.Questions.Count(q => q.IsCorrect);
        var total = _document.Questions.Count;
        var score = $"{correct}/{total}";
        var timeTaken = $"{(int)Elapsed.TotalMinutes}:{Elapsed.Seconds:D2}";

        QuizFinished?.Invoke(this, new McqQuizResultArgs
        {
            Score = score,
            CorrectCount = correct,
            TotalCount = total,
            Title = _document.Title ?? string.Empty,
            SourceDeck = _document.SourceDeck ?? string.Empty,
            TimeTaken = timeTaken,
        });
    }

    [RelayCommand]
    private void QuitQuiz()
    {
        StopTimer();
        IsCompleted = false;
        CurrentQuestionHtml = null;
        ReviewHtml = null;
        _document = null;
        HasError = false;
        ErrorText = null;
    }

    public void HandleAnswer(string letter)
    {
        AnswerQuestionCommand.Execute(letter);
    }

    public void HandleNext()
    {
        if (IsCompleted)
        {
            return;
        }

        NextQuestionCommand.Execute(null);
    }

    public void HandlePrev()
    {
        if (IsCompleted)
        {
            return;
        }

        PreviousQuestionCommand.Execute(null);
    }
}

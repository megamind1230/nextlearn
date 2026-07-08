using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextLearn.Desktop.Models;
using NextLearn.Desktop.Services;
using Serilog;

#pragma warning disable CA2007
#pragma warning disable CA1031

namespace NextLearn.Desktop.ViewModels;

public partial class McqGenerationViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IMcqGenerationService _mcqService;
    private readonly McqFileService _mcqFileService;
    private readonly string _decksPath;
    private List<Deck> _allDecks = [];
    private int _generationId;

    [ObservableProperty]
    private ObservableCollection<Deck> _decks = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _useRegex;

    [ObservableProperty]
    private string? _generationStatus;

    [ObservableProperty]
    private double _generationProgress;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private bool _isPreviewVisible;

    [ObservableProperty]
    private string _previewText = string.Empty;

    [ObservableProperty]
    private string _previewHeader = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    private Deck? _currentDeck;
    private List<McqQuestion> _generatedQuestions = [];

    public bool HasDecks => Decks.Count > 0;

    public bool ShowDeckList => !IsGenerating && !IsPreviewVisible && !HasError;

    partial void OnIsGeneratingChanged(bool value) => OnPropertyChanged(nameof(ShowDeckList));

    partial void OnIsPreviewVisibleChanged(bool value) => OnPropertyChanged(nameof(ShowDeckList));

    partial void OnHasErrorChanged(bool value) => OnPropertyChanged(nameof(ShowDeckList));

    partial void OnDecksChanged(ObservableCollection<Deck> value) => OnPropertyChanged(nameof(HasDecks));

    public McqGenerationViewModel(
        ISettingsService settingsService,
        IMcqGenerationService mcqService,
        McqFileService mcqFileService,
        string? decksPath = null)
    {
        _settingsService = settingsService;
        _mcqService = mcqService;
        _mcqFileService = mcqFileService;
        _decksPath = Constants.GetDecksPath(decksPath);
    }

    public void LoadDecks()
    {
        if (!Directory.Exists(_decksPath))
        {
            Directory.CreateDirectory(_decksPath);
            return;
        }

        _allDecks = [];
        var extensions = new[] { "*.md", "*.org" };
        foreach (var ext in extensions)
        {
            foreach (var file in Directory.GetFiles(_decksPath, ext, SearchOption.AllDirectories))
            {
                var deck = DeckFileParser.LoadDeckFromFile(file, _decksPath);
                if (deck != null)
                {
                    _allDecks.Add(deck);
                }
            }
        }

        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnUseRegexChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Decks = new ObservableCollection<Deck>(_allDecks.OrderBy(d => d.FileName));
            return;
        }

        var tokens = DeckFilter.Tokenize(SearchText);
        var filtered = _allDecks
            .Where(deck => tokens.All(token => DeckFilter.TokenMatch(deck, token, UseRegex)))
            .ToList();

        Decks = new ObservableCollection<Deck>(filtered.OrderBy(d => d.FileName));
    }

    [RelayCommand]
    private async Task GenerateMcq(Deck? deck)
    {
        if (deck == null)
        {
            return;
        }

        var apiKey = _settingsService.GeminiApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Log.Error("MCQ generation failed: no API key configured");
            ErrorMessage = "Configure your Gemini API key in Settings first.";
            HasError = true;
            return;
        }

        var filePath = Path.Combine(_decksPath, deck.FileName);
        if (!File.Exists(filePath))
        {
            Log.Error("MCQ generation failed: deck file not found: {Path}", filePath);
            ErrorMessage = "Deck file not found.";
            HasError = true;
            return;
        }

        var myId = Interlocked.Increment(ref _generationId);
        _currentDeck = deck;
        _generatedQuestions = [];
        PreviewHeader = string.Empty;
        PreviewText = string.Empty;
        IsPreviewVisible = false;
        IsGenerating = true;
        HasError = false;
        ErrorMessage = null;

        try
        {
            GenerationStatus = "Parsing deck content…";
            GenerationProgress = 10;
            await Task.Delay(100);

            var textContent = string.Join("\n", deck.Pages.Select(p => p.TextContent));

            if (string.IsNullOrWhiteSpace(textContent))
            {
                Log.Error("MCQ generation failed: deck has no text content");
                ErrorMessage = "Deck has no content to generate MCQs from.";
                HasError = true;
                return;
            }

            GenerationStatus = "Contacting Gemini…";
            GenerationProgress = 30;

            var result = await _mcqService.GenerateMcqAsync(textContent, apiKey);

            if (_generationId != myId)
            {
                return;
            }

            if (!result.Success)
            {
                Log.Error("MCQ generation failed: {Error}", result.Error);
                ErrorMessage = result.Error;
                HasError = true;
                return;
            }

            _generatedQuestions = result.Questions;

            GenerationStatus = "Formatting results…";
            GenerationProgress = 90;
            await Task.Delay(100);

            PreviewHeader = $"MCQ Questions ({result.Count} questions)";
            PreviewText = string.Join("\n\n", result.Questions.Select((q, i) =>
                $"Q{i + 1}: {q.Question}\n" +
                string.Join("\n", q.Options.Select((o, j) => $"  {(char)('A' + j)}. {o}")) +
                $"\nAnswer: {(char)('A' + q.CorrectIndex)}" +
                (q.Explanation != null ? $"\nExplanation: {q.Explanation}" : string.Empty)));

            GenerationStatus = "Ready";
            GenerationProgress = 100;
            IsPreviewVisible = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MCQ generation unexpected error");
            ErrorMessage = $"Unexpected error: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private async Task AcceptMcq()
    {
        if (_currentDeck == null || _generatedQuestions.Count == 0)
        {
            return;
        }

        var mcqsPath = _settingsService.ResolvedMcqsPath;
        Directory.CreateDirectory(mcqsPath);

        var baseName = Path.GetFileName(_currentDeck.FileName);
        var savePath = Path.Combine(mcqsPath, baseName + ".mcq");

        var doc = new McqDocument
        {
            Title = _currentDeck.Title,
            SourceDeck = _currentDeck.FileName,
            GeneratedDate = DateTime.Now,
            Questions = _generatedQuestions,
        };

        try
        {
            _mcqFileService.SaveMcq(savePath, doc);
            CancelPreview();
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save MCQ file");
            ErrorMessage = $"Failed to save MCQ file: {ex.Message}";
            HasError = true;
        }
    }

    [RelayCommand]
    private void CancelPreview()
    {
        IsPreviewVisible = false;
        GenerationStatus = null;
        GenerationProgress = 0;
        _currentDeck = null;
        _generatedQuestions = [];
        PreviewHeader = string.Empty;
        PreviewText = string.Empty;
        HasError = false;
        ErrorMessage = null;
    }

    public void FocusSearch()
    {
        SearchText = string.Empty;
    }
}

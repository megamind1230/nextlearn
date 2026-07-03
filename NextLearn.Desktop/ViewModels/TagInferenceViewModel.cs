using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextLearn.Desktop.Models;
using NextLearn.Desktop.Services;
using Serilog;

#pragma warning disable CA2007 // ConfigureAwait — UI thread dispatch
#pragma warning disable CA1031 // catch specific — we want to catch all

namespace NextLearn.Desktop.ViewModels;

public partial class TagInferenceViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ITagInferenceService _tagInferenceService;
    private readonly IDeckFileWriter _deckFileWriter;
    private readonly string _decksPath;
    private List<Deck> _allDecks = [];

    [ObservableProperty]
    private ObservableCollection<Deck> _decks = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _useRegex;

    [ObservableProperty]
    private string? _inferenceStatus;

    [ObservableProperty]
    private double _inferenceProgress;

    [ObservableProperty]
    private bool _isInferring;

    [ObservableProperty]
    private bool _isPreviewVisible;

    [ObservableProperty]
    private string _existingTagsDisplay = string.Empty;

    [ObservableProperty]
    private string _proposedTagsDisplay = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    public bool HasDecks => Decks.Count > 0;

    public bool ShowDeckList => !IsInferring && !IsPreviewVisible && !HasError;

    partial void OnIsInferringChanged(bool value) => OnPropertyChanged(nameof(ShowDeckList));

    partial void OnIsPreviewVisibleChanged(bool value) => OnPropertyChanged(nameof(ShowDeckList));

    partial void OnHasErrorChanged(bool value) => OnPropertyChanged(nameof(ShowDeckList));

    partial void OnDecksChanged(ObservableCollection<Deck> value) => OnPropertyChanged(nameof(HasDecks));

    private Deck? _currentDeck;
    private List<string> _suggestedTags = [];

    public TagInferenceViewModel(
        ISettingsService settingsService,
        ITagInferenceService tagInferenceService,
        IDeckFileWriter deckFileWriter,
        string? decksPath = null)
    {
        _settingsService = settingsService;
        _tagInferenceService = tagInferenceService;
        _deckFileWriter = deckFileWriter;
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

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnUseRegexChanged(bool value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Decks = new ObservableCollection<Deck>(_allDecks.OrderBy(d => d.FileName));
            return;
        }

        var tokens = SearchText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var filtered = _allDecks.Where(deck =>
        {
            var deckTags = (deck.Tags ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLowerInvariant())
                .ToHashSet();

            return tokens.All(token => TokenMatchesDeck(deck, deckTags, token));
        }).ToList();

        Decks = new ObservableCollection<Deck>(filtered.OrderBy(d => d.FileName));
    }

    private bool TokenMatchesDeck(Deck deck, HashSet<string> deckTags, string token)
    {
        if (token.StartsWith('#'))
        {
            var tag = token[1..].ToLowerInvariant();

            if (UseRegex)
            {
                try
                {
                    return deckTags.Any(t => Regex.IsMatch(t, tag, RegexOptions.IgnoreCase));
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }

            return deckTags.Any(t => t.StartsWith(tag));
        }

        if (UseRegex)
        {
            try
            {
                return Regex.IsMatch(deck.Title, token, RegexOptions.IgnoreCase) ||
                       Regex.IsMatch(deck.Description, token, RegexOptions.IgnoreCase) ||
                       Regex.IsMatch(deck.FileName, token, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        return deck.Title.Contains(token, StringComparison.OrdinalIgnoreCase) ||
               deck.Description.Contains(token, StringComparison.OrdinalIgnoreCase) ||
               deck.FileName.Contains(token, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private async Task InferTags(Deck? deck)
    {
        if (deck == null)
        {
            return;
        }

        var apiKey = _settingsService.GeminiApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Log.Error("Tag inference failed: no API key configured");
            ErrorMessage = "Configure your Gemini API key in Settings first.";
            HasError = true;
            return;
        }

        var filePath = Path.Combine(_decksPath, deck.FileName);
        if (!File.Exists(filePath))
        {
            Log.Error("Tag inference failed: deck file not found: {Path}", filePath);
            ErrorMessage = "Deck file not found.";
            HasError = true;
            return;
        }

        _currentDeck = deck;
        _suggestedTags = [];
        IsPreviewVisible = false;
        IsInferring = true;
        HasError = false;
        ErrorMessage = null;

        // Ensure frontmatter is healthy before inferencing
        InferenceStatus = "Checking frontmatter…";
        _deckFileWriter.EnsureHealthyFrontmatter(filePath, out _);

        // Reload deck so tags/desc/title reflect any changes
        var reloaded = DeckFileParser.LoadDeckFromFile(filePath, _decksPath);
        if (reloaded != null)
        {
            _currentDeck = reloaded;
        }

        try
        {
            InferenceStatus = "Parsing deck content…";
            InferenceProgress = 10;
            await Task.Delay(100);

            var textContent = string.Join("\n", deck.Pages.Select(p => p.TextContent));
            var existingTags = deck.Tags ?? string.Empty;

            InferenceStatus = "Contacting Gemini…";
            InferenceProgress = 30;

            var result = await _tagInferenceService.InferTagsAsync(textContent, existingTags, apiKey);

            if (!result.Success)
            {
                Log.Error("Tag inference failed: {Error}", result.Error);
                ErrorMessage = result.Error;
                HasError = true;
                IsInferring = false;
                InferenceStatus = null;
                return;
            }

            _suggestedTags = result.SuggestedTags;

            InferenceStatus = "Formatting results…";
            InferenceProgress = 90;
            await Task.Delay(100);

            ExistingTagsDisplay = string.IsNullOrWhiteSpace(existingTags) ? "(none)" : existingTags;
            ProposedTagsDisplay = string.Join(", ", _suggestedTags);

            InferenceStatus = "Ready";
            InferenceProgress = 100;
            IsPreviewVisible = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Tag inference unexpected error");
            ErrorMessage = $"Unexpected error: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsInferring = false;
        }
    }

    [RelayCommand]
    private async Task ApplyTags()
    {
        if (_currentDeck == null || _suggestedTags.Count == 0)
        {
            return;
        }

        var filePath = Path.Combine(_decksPath, _currentDeck.FileName);
        if (!File.Exists(filePath))
        {
            Log.Error("Tag apply failed: deck file not found: {Path}", filePath);
            ErrorMessage = "Deck file not found. It may have been moved or deleted.";
            HasError = true;
            return;
        }

        if (_deckFileWriter.AppendTags(filePath, _suggestedTags, out var error))
        {
            var reloaded = DeckFileParser.LoadDeckFromFile(filePath, _decksPath);
            if (reloaded != null)
            {
                var idx = _allDecks.FindIndex(d => d.Id == _currentDeck.Id);
                if (idx >= 0)
                {
                    _allDecks[idx] = reloaded;
                }

                var idx2 = Decks.IndexOf(_currentDeck);
                if (idx2 >= 0)
                {
                    Decks[idx2] = reloaded;
                }
            }

            CancelPreview();
        }
        else
        {
            Log.Error("Tag apply failed to write tags: {Error}", error);
            ErrorMessage = error ?? "Failed to write tags to file.";
            HasError = true;
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void CancelPreview()
    {
        IsPreviewVisible = false;
        InferenceStatus = null;
        InferenceProgress = 0;
        _currentDeck = null;
        _suggestedTags = [];
        ExistingTagsDisplay = string.Empty;
        ProposedTagsDisplay = string.Empty;
        HasError = false;
        ErrorMessage = null;
    }

    public void FocusSearch()
    {
        SearchText = string.Empty;
    }
}

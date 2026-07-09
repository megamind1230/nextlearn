using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Serilog;

// UI ViewModel — awaits must return to UI thread, no ConfigureAwait
#pragma warning disable CA2007
using NextLearn.Desktop.Data;
using NextLearn.Desktop.Models;
using NextLearn.Desktop.Services;
using SkiaSharp;

namespace NextLearn.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly IUserService _userService;
    private readonly IDeckService _deckService;
    private readonly IDeckFileService _deckFileService;
    private readonly ISettingsService _settingsService;
    private readonly IKeyBindingService _keyBindingService;
    private readonly ITagInferenceService _tagInferenceService;
    private readonly IFlashcardService _flashcardService;
    private readonly IMcqGenerationService _mcqGenerationService;
    private readonly IDeckFileWriter _deckFileWriter;
    private List<CommandPaletteEntry> _allCommandPaletteEntries;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _title = "NextLearn";

    [ObservableProperty]
    private bool _isLearning;

    [ObservableProperty]
    private bool _isEditorOpen;

    [ObservableProperty]
    private Deck? _editingDeck;

    [ObservableProperty]
    private bool _isSidebarOpen;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private bool _isPinnedViewOpen;

    [ObservableProperty]
    private bool _isArchivedViewOpen;

    [ObservableProperty]
    private bool _isHeatmapOpen;

    [ObservableProperty]
    private bool _isTagInferenceOpen;

    [ObservableProperty]
    private bool _isFlashcardOpen;

    [ObservableProperty]
    private bool _isMcqOpen;

    [ObservableProperty]
    private bool _isFocusTimerOpen;

    [ObservableProperty]
    private int _todayMinutes;

    [ObservableProperty]
    private int _todayPages;

    [ObservableProperty]
    private int _todayDecks;

    [ObservableProperty]
    private int _todayStreak;

    [ObservableProperty]
    private ObservableCollection<HeatmapCell> _heatmapCells = new();

    [ObservableProperty]
    private double _heatmapCellScale = 1.0;

    [ObservableProperty]
    private bool _isMarketplaceOpen;

    [ObservableProperty]
    private bool _isFalconEyeEnabled;

    [ObservableProperty]
    private string _theme = string.Empty;

    [ObservableProperty]
    private string _font = string.Empty;

    [ObservableProperty]
    private string _decksPath = string.Empty;

    [ObservableProperty]
    private string _flashcardsPath = string.Empty;

    [ObservableProperty]
    private string _mcqsPath = string.Empty;

    [ObservableProperty]
    private string _keyBindingsProfile = string.Empty;

    [ObservableProperty]
    private string _geminiApiKey = string.Empty;

    [ObservableProperty]
    private int _focusWorkDuration = 25;

    [ObservableProperty]
    private int _focusBreakDuration = 5;

    [ObservableProperty]
    private string _settingsStatus = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowChordDisplay))]
    private string? _chordDisplayText;

    public bool ShowChordDisplay
    {
        get { return ChordDisplayText is not null; }
    }

    [ObservableProperty]
    private bool _isShortcutsHandbookOpen;

    [ObservableProperty]
    private List<ShortcutSection> _handbookSections = new();

    [ObservableProperty]
    private bool _isCommandPaletteOpen;

    [ObservableProperty]
    private string _commandPaletteInput = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CommandPaletteEntry> _filteredCommandPaletteEntries = new();

    [ObservableProperty]
    private CommandPaletteEntry? _selectedCommand;

    private int _selectedCommandIndex = -1;

    public bool HasFilteredCommands => FilteredCommandPaletteEntries.Count > 0;

    [ObservableProperty]
    private bool _isImageOverlayOpen;

    [ObservableProperty]
    private bool _isDeckLinkPromptOpen;

    private string _deckLinkTarget = string.Empty;
    private string _deckLinkDisplay = string.Empty;

    private bool _wasLearningBeforeFocusTimer;

    private bool IsArchivedDeckLink => _deckLinkTarget.EndsWith('~');

    public string DeckLinkPromptTitle =>
        IsArchivedDeckLink
            ? $"This deck is archived. Unarchive and open \"{_deckLinkDisplay}\"?"
            : $"Open \"{_deckLinkDisplay}\"?";

    public string DeckLinkPromptActionText =>
        IsArchivedDeckLink ? "Unarchive & Open" : "Open Deck";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentImageFileName))]
    private string _currentImagePath = string.Empty;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private double _textScale = 1.0;

    [ObservableProperty]
    private Avalonia.Media.Imaging.Bitmap? _currentImageBitmap;

    private Avalonia.Media.Imaging.Bitmap? _normalBitmap;

    [ObservableProperty]
    private bool _isInverted;

    public Func<string?, Task<string?>>? PickFolderHandler { get; set; }

    public IKeyBindingService KeyBindingService => _keyBindingService;

    public string CurrentImageFileName => Path.GetFileName(CurrentImagePath);

    partial void OnCurrentImagePathChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || !File.Exists(value))
        {
            CurrentImageBitmap = null;
            return;
        }

        var capturedPath = value;
        Task.Run(() =>
        {
#pragma warning disable CA1031
            try
            {
                var bytes = File.ReadAllBytes(capturedPath);
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        if (CurrentImagePath != capturedPath) return;
                        var ms = new MemoryStream(bytes);
                        _normalBitmap = new Avalonia.Media.Imaging.Bitmap(ms);
                        CurrentImageBitmap = IsInverted
                            ? (CreateInvertedBitmap(_normalBitmap) ?? _normalBitmap)
                            : _normalBitmap;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to decode bitmap from {Path}", capturedPath);
                        if (CurrentImagePath == capturedPath)
                            CurrentImageBitmap = null;
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to read image file {Path}", capturedPath);
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (CurrentImagePath == capturedPath)
                        CurrentImageBitmap = null;
                });
            }
#pragma warning restore CA1031
        });
    }

    private static Avalonia.Media.Imaging.Bitmap? CreateInvertedBitmap(Avalonia.Media.Imaging.Bitmap source)
    {
        try
        {
            using var ms = new MemoryStream();
            source.Save(ms);
            ms.Position = 0;
            using var skData = SKData.Create(ms);
            using var skBitmap = SKBitmap.Decode(skData);
            if (skBitmap == null)
            {
                return null;
            }

            for (var y = 0; y < skBitmap.Height; y++)
            {
                for (var x = 0; x < skBitmap.Width; x++)
                {
                    var pixel = skBitmap.GetPixel(x, y);
                    skBitmap.SetPixel(x, y, new SKColor(
                        (byte)(255 - pixel.Red),
                        (byte)(255 - pixel.Green),
                        (byte)(255 - pixel.Blue),
                        pixel.Alpha));
                }
            }

            using var pngData = skBitmap.Encode(SKEncodedImageFormat.Png, 100);
            using var pngStream = pngData.AsStream();
            return new Avalonia.Media.Imaging.Bitmap(pngStream);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Log.Error(ex, "Failed to create inverted bitmap");
            return null;
        }
    }

    public HomeViewModel HomeViewModel { get; }

    public LearningViewModel LearningViewModel { get; }

    public TagInferenceViewModel TagInferenceViewModel { get; private set; }

    public FlashcardViewModel FlashcardViewModel { get; private set; }

    public McqPanelViewModel McqPanelViewModel { get; private set; }

    public FocusTimerViewModel FocusTimerViewModel { get; }

    public MainWindowViewModel()
    {
        _context = new AppDbContext();
        _context.Database.EnsureCreated();
        MigrateSchema(_context);

        _userService = new UserService(_context);
        _deckService = new DeckService(_context, _userService);
        _deckFileService = new DeckFileService();
        _settingsService = new SettingsService();
        _keyBindingService = new KeyBindingService();
        _tagInferenceService = new TagInferenceService(new HttpClient());
        _flashcardService = new FlashcardService(new HttpClient());
        _mcqGenerationService = new McqGenerationService(new HttpClient());
        _deckFileWriter = new DeckFileWriter();
        _allCommandPaletteEntries = BuildCommandPaletteEntries();
        var htmlContentBuilder = new HtmlContentService();

        LoadSettings();

        KeyBindingsChanged += () =>
        {
            _allCommandPaletteEntries = BuildCommandPaletteEntries();
            if (IsCommandPaletteOpen)
            {
                OnCommandPaletteInputChanged(CommandPaletteInput);
            }

            if (IsShortcutsHandbookOpen)
            {
                RebuildHandbookSections();
            }
        };

        if (!string.IsNullOrEmpty(_settingsService.KeyBindingsProfile))
        {
            _keyBindingService.SwitchProfile(_settingsService.KeyBindingsProfile);
            KeyBindingsChanged?.Invoke();
        }

        var decksPath = _settingsService.ResolvedDecksPath;
        HomeViewModel = new HomeViewModel(_deckService, _deckFileService, this, decksPath);
        LearningViewModel = new LearningViewModel(_deckService, _userService, htmlContentBuilder, this, decksPath);
        TagInferenceViewModel = new TagInferenceViewModel(_settingsService, _tagInferenceService, _deckFileWriter, decksPath);
        FlashcardViewModel = new FlashcardViewModel(_settingsService, _flashcardService, decksPath);

        var mcqFileService = new McqFileService();
        var mcqGenerationVm = new McqGenerationViewModel(_settingsService, _mcqGenerationService, mcqFileService, decksPath);
        var mcqQuizVm = new McqQuizViewModel(mcqFileService, _settingsService);
        McqPanelViewModel = new McqPanelViewModel(_settingsService, mcqFileService, mcqGenerationVm, mcqQuizVm);

        FocusTimerViewModel = new FocusTimerViewModel();
        FocusTimerViewModel.SetDefaults(_settingsService.Settings.FocusWorkDuration, _settingsService.Settings.FocusBreakDuration);

        CurrentView = HomeViewModel;
    }

#pragma warning disable CA1031
    private static void MigrateSchema(AppDbContext context)
    {
        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Decks ADD COLUMN IsArchived INTEGER NOT NULL DEFAULT 0");
        }
        catch
        {
        }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Decks ADD COLUMN IsPinned INTEGER NOT NULL DEFAULT 0");
        }
        catch
        {
        }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Pages DROP COLUMN DurationSeconds");
        }
        catch
        {
        }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Pages DROP COLUMN MediaPath");
        }
        catch
        {
        }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Decks DROP COLUMN Category");
        }
        catch
        {
        }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Decks DROP COLUMN Difficulty");
        }
        catch
        {
        }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Decks DROP COLUMN DownloadsCount");
        }
        catch
        {
        }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Users DROP COLUMN Email");
        }
        catch
        {
        }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Users DROP COLUMN PasswordHash");
        }
        catch
        {
        }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Users DROP COLUMN TotalDecksShared");
        }
        catch
        {
        }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE UserProgress DROP COLUMN IsDownloaded");
        }
        catch
        {
        }
    }
#pragma warning restore CA1031

    private void LoadSettings()
    {
        Theme = _settingsService.Theme;
        Font = _settingsService.Font;
        DecksPath = _settingsService.DecksPath;
        FlashcardsPath = _settingsService.FlashcardsPath;
        McqsPath = _settingsService.McqsPath;
        KeyBindingsProfile = _settingsService.KeyBindingsProfile;
        IsFalconEyeEnabled = _settingsService.FalconEyeEnabled;
        GeminiApiKey = _settingsService.GeminiApiKey;
        FocusWorkDuration = _settingsService.Settings.FocusWorkDuration;
        FocusBreakDuration = _settingsService.Settings.FocusBreakDuration;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _settingsService.Theme = Theme;
        _settingsService.Font = Font;
        _settingsService.DecksPath = DecksPath;
        _settingsService.FlashcardsPath = FlashcardsPath;
        _settingsService.McqsPath = McqsPath;
        _settingsService.KeyBindingsProfile = KeyBindingsProfile;
        _settingsService.FalconEyeEnabled = IsFalconEyeEnabled;
        _settingsService.Settings.FocusWorkDuration = FocusWorkDuration;
        _settingsService.Settings.FocusBreakDuration = FocusBreakDuration;
        _settingsService.GeminiApiKey = GeminiApiKey;
        _keyBindingService.SwitchProfile(KeyBindingsProfile);
        KeyBindingsChanged?.Invoke();

        var resolved = _settingsService.ResolvedDecksPath;
        Directory.CreateDirectory(resolved);

        if (_settingsService.TrySave(out var error))
        {
            SettingsStatus = "Settings saved";
        }
        else
        {
            SettingsStatus = $"Error: {error}";
        }

        ClearStatusAfterDelay();

        var resolvedFont = string.IsNullOrWhiteSpace(Font) ? "Inter" : Font;
        FontChanged?.Invoke(resolvedFont);
    }

    [RelayCommand]
    private void ResetSettings()
    {
        var defaults = SettingsService.Defaults();
        Theme = defaults.Theme;
        Font = defaults.Font;
        DecksPath = defaults.DecksPath;
        FlashcardsPath = defaults.FlashcardsPath;
        McqsPath = defaults.McqsPath;
        KeyBindingsProfile = defaults.KeyBindingsProfile;
        IsFalconEyeEnabled = defaults.FalconEyeEnabled;
        FocusWorkDuration = defaults.FocusWorkDuration;
        FocusBreakDuration = defaults.FocusBreakDuration;
        GeminiApiKey = defaults.GeminiApiKey;
    }

    [RelayCommand]
    private async Task BrowseDecksPathAsync()
    {
        if (PickFolderHandler != null)
        {
            var result = await PickFolderHandler(DecksPath);
            if (result != null)
            {
                DecksPath = result;
            }
        }
    }

    [RelayCommand]
    private async Task BrowseFlashcardsPathAsync()
    {
        if (PickFolderHandler != null)
        {
            var result = await PickFolderHandler(FlashcardsPath);
            if (result != null)
            {
                FlashcardsPath = result;
            }
        }
    }

    [RelayCommand]
    private async Task BrowseMcqsPathAsync()
    {
        if (PickFolderHandler != null)
        {
            var result = await PickFolderHandler(McqsPath);
            if (result != null)
            {
                McqsPath = result;
            }
        }
    }

#pragma warning disable CA1822 // Member does not access instance data
    [RelayCommand]
    private void OpenGeminiApiUrl()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo("https://aistudio.google.com/apikey")
            {
                UseShellExecute = true,
            };
            process.Start();
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open Gemini API URL");
        }
#pragma warning restore CA1031
    }
#pragma warning restore CA1822

    private static void OpenDirectoryInExplorer(string path)
    {
        Log.Information("OpenDirectoryInExplorer: {Path}", path);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start("explorer", path);
                Log.Information("Opened via explorer");
                return;
            }

            if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", path);
                Log.Information("Opened via open");
                return;
            }

            foreach (var fm in new[]
            {
                "thunar", "nautilus", "dolphin", "nemo", "caja",
                "pcmanfm", "konqueror", "krusader", "doublecmd",
                "spacefm", "xfe", "rox-filer",
                "ranger", "nnn", "lf", "mc", "yazi",
            })
            {
                try
                {
                    Process.Start(fm, path);
                    Log.Information("Opened via {FileManager}", fm);
                    return;
                }
                catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
                {
                    Log.Debug(ex, "{FileManager} not found for {Path}", fm, path);
                }
            }

            try
            {
                Process.Start("xdg-open", path);
                Log.Information("Opened via xdg-open (last resort)");
                return;
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
                Log.Debug(ex, "xdg-open also failed for {Path}", path);
            }

            Log.Error("No file manager found to open {Path}", path);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open path in file manager: {Path}", path);
        }
#pragma warning restore CA1031
    }

    [RelayCommand]
    private void OpenDecksFolder()
    {
        OpenDirectoryInExplorer(_settingsService.ResolvedDecksPath);
    }

    private static void RevealFileInExplorer(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (dir == null)
        {
            return;
        }

        Log.Information("RevealFileInExplorer: {File}", filePath);

        if (!File.Exists(filePath))
        {
            OpenDirectoryInExplorer(dir);
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("explorer")
                {
                    ArgumentList = { $"/select,{filePath}" },
                });
                Log.Information("Revealed via explorer /select");
                return;
            }

            if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", $"-R \"{filePath}\"");
                Log.Information("Revealed via open -R");
                return;
            }

            foreach (var (fm, flag) in new[]
            {
                ("nautilus", "--select"),
                ("dolphin", "--select"),
                ("nemo", "--select"),
                ("caja", "--select"),
                ("thunar", string.Empty),
                ("yazi", string.Empty),
                ("spacefm", string.Empty),
            })
            {
                try
                {
                    var args = string.IsNullOrEmpty(flag)
                        ? $"\"{filePath}\""
                        : $"{flag} \"{filePath}\"";
                    Process.Start(fm, args);
                    Log.Information("Revealed via {FileManager}", fm);
                    return;
                }
                catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
                {
                    Log.Debug(ex, "{FileManager} not supported for {File}", fm, filePath);
                }
            }

            // Fallback: just open the directory
            OpenDirectoryInExplorer(dir);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to reveal file: {File}", filePath);
            OpenDirectoryInExplorer(dir);
        }
#pragma warning restore CA1031
    }

    [RelayCommand]
    private void OpenCurrentDeckFolder()
    {
        if (!IsLearning)
        {
            OpenDirectoryInExplorer(_settingsService.ResolvedDecksPath);
            return;
        }

        var deckFile = LearningViewModel.CurrentDeckFilePath;
        if (deckFile == null)
        {
            OpenDirectoryInExplorer(_settingsService.ResolvedDecksPath);
            return;
        }

        RevealFileInExplorer(deckFile);
    }

    [RelayCommand]
    public async Task NavigateToHomeAsync()
    {
        if (IsImageOverlayOpen)
        {
            CloseImageOverlay();
        }

        if (IsLearning)
        {
            await LearningViewModel.SaveProgressAsync();
        }

        IsLearning = false;
        CurrentView = HomeViewModel;
        HomeViewModel.Refresh();
    }

    [RelayCommand]
    public async Task NavigateToLearningAsync(Guid deckId)
    {
        IsPinnedViewOpen = false;
        IsArchivedViewOpen = false;
        IsTagInferenceOpen = false;
        IsLearning = true;
        var deck = HomeViewModel.Decks.FirstOrDefault(d => d.Id == deckId);
        if (deck != null)
        {
            await LearningViewModel.SetCurrentDeckAsync(deck);
        }
        else
        {
            LearningViewModel.StartLearning(deckId);
        }

        CurrentView = LearningViewModel;
    }

    public async void NavigateToLearningByDeck(Deck deck)
    {
        IsPinnedViewOpen = false;
        IsArchivedViewOpen = false;
        IsTagInferenceOpen = false;
        IsLearning = true;
        await LearningViewModel.SetCurrentDeckAsync(deck);
        CurrentView = LearningViewModel;
    }

    [RelayCommand]
    public void NavigateToMarketplace()
    {
        IsSidebarOpen = false;
        IsPinnedViewOpen = false;
        IsArchivedViewOpen = false;
        IsSettingsOpen = false;
        IsHeatmapOpen = false;
        IsTagInferenceOpen = false;
        IsMarketplaceOpen = true;
    }

    [RelayCommand]
    public void CloseMarketplace()
    {
        IsMarketplaceOpen = false;
    }

    [RelayCommand]
    public async Task ExitLearningAsync()
    {
        await NavigateToHomeAsync();
    }

    [RelayCommand]
#pragma warning disable CA1822
    public void NavigateToDocumentation()
#pragma warning restore CA1822
    {
        Views.MainWindow.OpenInBrowser("https://github.com/megamind1230/nextlearn/blob/master/README.org");
    }

    [RelayCommand]
#pragma warning disable CA1822
    public void NavigateToPlugins()
#pragma warning restore CA1822
    {
        Views.MainWindow.OpenInBrowser("https://github.com/megamind1230");
    }

    [RelayCommand]
    public void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
    }

    [RelayCommand]
    private void ToggleFalconEye()
    {
        IsFalconEyeEnabled = !IsFalconEyeEnabled;
    }

    partial void OnIsFalconEyeEnabledChanged(bool value)
    {
        if (IsLearning)
        {
            LearningViewModel.RebuildWithFalconEye(value);
        }
    }

    partial void OnThemeChanged(string value)
    {
        ThemeHelper.ApplyTheme(value);
    }

    [RelayCommand]
    public void OpenSettings()
    {
        LoadSettings();
        SettingsStatus = string.Empty;
        IsSidebarOpen = false;
        IsPinnedViewOpen = false;
        IsArchivedViewOpen = false;
        IsHeatmapOpen = false;
        IsTagInferenceOpen = false;
        IsSettingsOpen = true;
    }

    [RelayCommand]
    public void CloseSettings()
    {
        SettingsStatus = string.Empty;
        IsSettingsOpen = false;
    }

    public ObservableCollection<Deck> PinnedDecks { get; set; } = new();

    public ObservableCollection<Deck> ArchivedDecks { get; set; } = new();

    [RelayCommand]
    public void ShowPinnedView()
    {
        var decksPath = _settingsService.ResolvedDecksPath;
        PinnedDecks.Clear();
        foreach (var d in DeckFileService.GetPinnedDecks(decksPath).OrderBy(d => d.FileName))
        {
            PinnedDecks.Add(d);
        }

        IsSidebarOpen = false;
        IsArchivedViewOpen = false;
        IsSettingsOpen = false;
        IsHeatmapOpen = false;
        IsTagInferenceOpen = false;
        IsPinnedViewOpen = true;
    }

    [RelayCommand]
    public void ShowArchivedView()
    {
        var decksPath = _settingsService.ResolvedDecksPath;
        ArchivedDecks.Clear();
        foreach (var d in DeckFileService.GetArchivedDecks(decksPath).OrderBy(d => d.FileName))
        {
            ArchivedDecks.Add(d);
        }

        IsSidebarOpen = false;
        IsPinnedViewOpen = false;
        IsSettingsOpen = false;
        IsHeatmapOpen = false;
        IsTagInferenceOpen = false;
        IsArchivedViewOpen = true;
    }

    [RelayCommand]
    public void ClosePinnedView()
    {
        IsPinnedViewOpen = false;
    }

    [RelayCommand]
    public void CloseArchivedView()
    {
        IsArchivedViewOpen = false;
    }

    [RelayCommand]
    public void UnpinFromView(Deck deck)
    {
        ArgumentNullException.ThrowIfNull(deck);
        var decksPath = _settingsService.ResolvedDecksPath;
        _deckFileService.UnpinDeck(deck, decksPath);
        _deckService.SyncDeckMetadata(deck);
        PinnedDecks.Remove(deck);
        HomeViewModel.Refresh();
    }

    [RelayCommand]
    public void Unarchive(Deck deck)
    {
        ArgumentNullException.ThrowIfNull(deck);
        var decksPath = _settingsService.ResolvedDecksPath;
        _deckFileService.UnarchiveDeck(deck, decksPath);
        _deckService.SyncDeckMetadata(deck);
        ArchivedDecks.Remove(deck);
        HomeViewModel.Refresh();
    }

    [RelayCommand]
    public void ShowHeatmap()
    {
        RefreshHeatmap();
        IsSidebarOpen = false;
        IsPinnedViewOpen = false;
        IsArchivedViewOpen = false;
        IsSettingsOpen = false;
        IsMarketplaceOpen = false;
        IsTagInferenceOpen = false;
        IsHeatmapOpen = true;
    }

    [RelayCommand]
    public void CloseHeatmap()
    {
        IsHeatmapOpen = false;
    }

    [RelayCommand]
    public void ShowTagInference()
    {
        IsSidebarOpen = false;
        IsPinnedViewOpen = false;
        IsArchivedViewOpen = false;
        IsSettingsOpen = false;
        IsHeatmapOpen = false;
        IsMarketplaceOpen = false;
        IsFlashcardOpen = false;
        TagInferenceViewModel.LoadDecks();
        IsTagInferenceOpen = true;
    }

    [RelayCommand]
    public void CloseTagInference()
    {
        IsTagInferenceOpen = false;
        TagInferenceViewModel.CancelPreviewCommand.Execute(null);
    }

    [RelayCommand]
    public void ShowFlashcardPanel()
    {
        IsSidebarOpen = false;
        IsPinnedViewOpen = false;
        IsArchivedViewOpen = false;
        IsSettingsOpen = false;
        IsHeatmapOpen = false;
        IsMarketplaceOpen = false;
        IsTagInferenceOpen = false;
        FlashcardViewModel.LoadDecks();
        IsFlashcardOpen = true;
    }

    [RelayCommand]
    public void CloseFlashcardPanel()
    {
        IsFlashcardOpen = false;
        FlashcardViewModel.CancelPreviewCommand.Execute(null);
    }

    [RelayCommand]
    public void ShowMcqPanel()
    {
        IsSidebarOpen = false;
        IsPinnedViewOpen = false;
        IsArchivedViewOpen = false;
        IsSettingsOpen = false;
        IsHeatmapOpen = false;
        IsMarketplaceOpen = false;
        IsTagInferenceOpen = false;
        IsFlashcardOpen = false;
        McqPanelViewModel.Generation.LoadDecks();
        McqPanelViewModel.QuitQuizCommand.Execute(null);
        IsMcqOpen = true;
    }

    [RelayCommand]
    public void CloseMcqPanel()
    {
        if (McqPanelViewModel.IsQuizActive)
        {
            McqPanelViewModel.QuitQuizCommand.Execute(null);
        }
        else
        {
            IsMcqOpen = false;
            McqPanelViewModel.QuitQuizCommand.Execute(null);
            McqPanelViewModel.Generation.CancelPreviewCommand.Execute(null);
        }
    }

    [RelayCommand]
    public void OpenFocusTimer()
    {
        IsSidebarOpen = false;
        IsPinnedViewOpen = false;
        IsArchivedViewOpen = false;
        IsSettingsOpen = false;
        IsHeatmapOpen = false;
        IsMarketplaceOpen = false;
        IsTagInferenceOpen = false;
        IsFlashcardOpen = false;
        IsMcqOpen = false;
        _wasLearningBeforeFocusTimer = IsLearning;
        IsLearning = false;
        FocusTimerViewModel.SetDefaults(_settingsService.Settings.FocusWorkDuration, _settingsService.Settings.FocusBreakDuration);
        IsFocusTimerOpen = true;
    }

    [RelayCommand]
    public void CloseFocusTimer()
    {
        IsFocusTimerOpen = false;
        if (_wasLearningBeforeFocusTimer)
        {
            IsLearning = true;
        }
    }

    [RelayCommand]
    private void ZoomHeatmapIn()
    {
        HeatmapCellScale = Math.Min(3.0, HeatmapCellScale + 0.25);
    }

    [RelayCommand]
    private void ZoomHeatmapOut()
    {
        HeatmapCellScale = Math.Max(0.5, HeatmapCellScale - 0.25);
    }

    [RelayCommand]
    private void ZoomHeatmapReset()
    {
        HeatmapCellScale = 1.0;
    }

    private void RefreshHeatmap()
    {
        var (minutes, pages, decks, streak) = _userService.GetTodayStats();
        TodayMinutes = minutes;
        TodayPages = pages;
        TodayDecks = decks;
        TodayStreak = streak;

        var activity = _userService.GetActivityHistory(365);
        var activityByDate = activity.ToDictionary(a => a.Date, a => a.MinutesLearned);

        var today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        HeatmapCells.Clear();

        var year = today.Year;
        var start = new DateTime(year, 1, 1);
        var end = today;

        var current = start;
        while (current <= end)
        {
            var totalDays = (int)(current - start).TotalDays;
            var oldRow = totalDays / 7;
            var oldCol = (oldRow % 2 == 0) ? totalDays % 7 : 6 - (totalDays % 7);

            // Rotate 90° CCW so Jan 1 is at bottom-left, days snake upward
            const int maxOldCol = 6;
            var row = maxOldCol - oldCol;
            var col = oldRow;

            activityByDate.TryGetValue(current, out var minutesLearned);

            HeatmapCells.Add(new HeatmapCell
            {
                Date = current,
                Count = minutesLearned,
                Row = row,
                Col = col,
            });

            current = current.AddDays(1);
        }
    }

    partial void OnIsShortcutsHandbookOpenChanged(bool value)
    {
        if (value)
        {
            RebuildHandbookSections();
        }
    }

    [RelayCommand]
    public void CloseShortcutsHandbook()
    {
        IsShortcutsHandbookOpen = false;
    }

    public void RebuildHandbookSections()
    {
        var sections = new Dictionary<string, List<ShortcutEntry>>();
        var seen = new Dictionary<string, HashSet<string>>();

        foreach (var b in _keyBindingService.CurrentBindings)
        {
            var section = b.Context ?? "Global";
            if (section == "ImageOverlay")
            {
                section = "Image Overlay";
            }

            if (!sections.ContainsKey(section))
            {
                sections[section] = new List<ShortcutEntry>();
                seen[section] = new HashSet<string>();
            }

            var keyText = b.Chords is { Count: > 0 }
                ? FormatChordDisplay(b.Chords)
                : FormatKeyForDisplay(b.Key, b.Modifiers);
            var desc = b.Comment ?? b.Action.ToString();

            // Skip duplicate entries (same keyText and description within a section)
            var dedupKey = $"{keyText}|{desc}";
            if (!seen[section].Add(dedupKey))
            {
                continue;
            }

            sections[section].Add(new ShortcutEntry
            {
                Section = section,
                KeyText = keyText,
                Description = desc,
            });
        }

        // Add static Esc entry (not in binding table)
        sections["Global"].Add(new ShortcutEntry
        {
            Section = "Global",
            KeyText = "Esc",
            Description = "Close current overlay",
        });

        // Add static C-g entry for Emacs mode (not in binding table — hard-coded in KeyboardHandler)
        sections["Global"].Add(new ShortcutEntry
        {
            Section = "Global",
            KeyText = "C-g",
            Description = "Cancel / close current overlay (Emacs)",
        });

        HandbookSections = sections
            .OrderBy(s => s.Key is "Global" ? 0 : s.Key is "Home" ? 1 : s.Key is "Learning" ? 2 : s.Key is "Image Overlay" ? 3 : 4)
            .Select(s => new ShortcutSection { Name = s.Key, Entries = s.Value })
            .ToList();
    }

    internal static string FormatKeyForDisplay(string key, string modifiers, bool compact = false)
    {
        var parts = new List<string>();
        if (modifiers.Contains("Control"))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.Contains("Shift"))
        {
            parts.Add("Shift");
        }

        if (modifiers.Contains("Alt"))
        {
            parts.Add("Alt");
        }

        parts.Add(key switch
        {
            "OemPlus" => "+",
            "OemMinus" => "-",
            "OemComma" => ",",
            "OemPeriod" => ".",
            "Oem2" => modifiers.Contains("Shift") ? "?" : "/",
            "D0" => "0",
            "D1" => "1",
            "D2" => "2",
            "D3" => "3",
            "D4" => "4",
            "D5" => "5",
            "D6" => "6",
            "D7" => "7",
            "D8" => "8",
            "D9" => "9",
            "NumPad0" => "0",
            "Left" => "←",
            "Right" => "→",
            "Up" => "↑",
            "Down" => "↓",
            "Space" => "Space",
            "Escape" => "Esc",
            _ => key.ToLowerInvariant(),
        });

        return compact ? string.Join("+", parts) : string.Join(" + ", parts);
    }

    private static string FormatChordDisplay(List<Models.KeyChord> chords)
    {
        return string.Join(" ", chords.Select(c => FormatKeyForDisplay(c.Key, c.Modifiers, compact: true)));
    }

    public void OpenImageOverlay(string imagePath)
    {
        try
        {
            var paths = LearningViewModel.CurrentPageImagePaths;
            var idx = paths.IndexOf(imagePath);
            CurrentImageIndex = idx >= 0 ? idx : 0;
            CurrentImagePath = imagePath;
            ZoomLevel = 1.0;
            IsImageOverlayOpen = true;
            Log.Information(
                "Image overlay opened: {Path} (index {Index} of {Count})",
                imagePath,
                CurrentImageIndex,
                paths.Count);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open image overlay for {Path}", imagePath);
        }
#pragma warning restore CA1031
    }

    [ObservableProperty]
    private int _currentImageIndex;

    [RelayCommand]
    public void CloseImageOverlay()
    {
        IsImageOverlayOpen = false;
        ZoomLevel = 1.0;
        Log.Information("Image overlay closed");
    }

    public void TryOpenDeckLink(string relativePath)
    {
        _deckLinkTarget = relativePath;
        _deckLinkDisplay = relativePath;
        OnPropertyChanged(nameof(DeckLinkPromptTitle));
        OnPropertyChanged(nameof(DeckLinkPromptActionText));
        IsDeckLinkPromptOpen = true;
        Log.Information("Deck link prompt: {Path}", relativePath);
    }

    [RelayCommand]
    private void DismissDeckLinkPrompt()
    {
        IsDeckLinkPromptOpen = false;
        _deckLinkTarget = string.Empty;
        _deckLinkDisplay = string.Empty;
    }

    [RelayCommand]
    private void NavigateToDeckLink()
    {
        IsDeckLinkPromptOpen = false;
        var target = _deckLinkTarget;
        _deckLinkTarget = string.Empty;
        _deckLinkDisplay = string.Empty;

        if (string.IsNullOrEmpty(target))
        {
            return;
        }

        var decksPath = _settingsService.ResolvedDecksPath;
        var fullPath = Path.GetFullPath(Path.Combine(decksPath, target));

        if (!fullPath.StartsWith(Path.GetFullPath(decksPath), StringComparison.Ordinal) || !File.Exists(fullPath))
        {
            Log.Warning("Deck link target not found or invalid: {Path}", target);
            return;
        }

        // If archived, load deck first (file still has content), unarchive, sync DB, then navigate
        if (target.EndsWith('~'))
        {
            var deck = DeckFileParser.LoadDeckFromFile(fullPath, decksPath);
            if (deck == null)
            {
                Log.Warning("Failed to load archived deck from {Path}", fullPath);
                return;
            }

            _deckFileService.UnarchiveDeck(deck, decksPath);
            _deckService.SyncDeckMetadata(deck);
            NavigateToLearningByDeck(deck);
            return;
        }

        var normalDeck = DeckFileParser.LoadDeckFromFile(fullPath, decksPath);
        if (normalDeck != null)
        {
            NavigateToLearningByDeck(normalDeck);
        }
    }

    [RelayCommand]
    private void ZoomIn()
    {
        ZoomLevel = Math.Min(3.0, ZoomLevel + 0.25);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel = Math.Max(0.5, ZoomLevel - 0.25);
    }

    [RelayCommand]
    private void ResetZoom()
    {
        ZoomLevel = 1.0;
    }

    public event Action? KeyBindingsChanged;

    public event Action<double, double>? TextScaleChanged;

    public event Action<string>? FontChanged;

    [RelayCommand]
    private void ZoomTextIn()
    {
        var old = TextScale;
        TextScale = Math.Min(2.0, TextScale + 0.2);
        TextScaleChanged?.Invoke(old, TextScale);
    }

    [RelayCommand]
    private void ZoomTextOut()
    {
        var old = TextScale;
        TextScale = Math.Max(0.6, TextScale - 0.2);
        TextScaleChanged?.Invoke(old, TextScale);
    }

    [RelayCommand]
    private void ResetTextZoom()
    {
        var old = TextScale;
        TextScale = 1.0;
        TextScaleChanged?.Invoke(old, TextScale);
    }

    [RelayCommand]
    private void NextImage()
    {
        var paths = LearningViewModel.CurrentPageImagePaths;
        if (paths.Count == 0)
        {
            return;
        }

        CurrentImageIndex = (CurrentImageIndex + 1) % paths.Count;
        CurrentImagePath = paths[CurrentImageIndex];
        Log.Information("Image overlay next: {Path}", CurrentImagePath);
    }

    [RelayCommand]
    private void PreviousImage()
    {
        var paths = LearningViewModel.CurrentPageImagePaths;
        if (paths.Count == 0)
        {
            return;
        }

        CurrentImageIndex = (CurrentImageIndex - 1 + paths.Count) % paths.Count;
        CurrentImagePath = paths[CurrentImageIndex];
        Log.Information("Image overlay previous: {Path}", CurrentImagePath);
    }

    [RelayCommand]
    private void ToggleInvert()
    {
        IsInverted = !IsInverted;
        if (_normalBitmap == null)
        {
            return;
        }

        CurrentImageBitmap = IsInverted
            ? (CreateInvertedBitmap(_normalBitmap) ?? _normalBitmap)
            : _normalBitmap;
    }

    [RelayCommand]
    private void OpenImageInViewer()
    {
        if (string.IsNullOrEmpty(CurrentImagePath) || !File.Exists(CurrentImagePath))
        {
            return;
        }

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(CurrentImagePath)
            {
                UseShellExecute = true,
            };
            process.Start();
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open image in external viewer: {Path}", CurrentImagePath);
        }
#pragma warning restore CA1031
    }

    private async void ClearStatusAfterDelay()
    {
        await Task.Delay(3000);
        SettingsStatus = string.Empty;
    }

    // ── Command Palette ─────────────────────────────────────────────
    partial void OnCommandPaletteInputChanged(string value)
    {
        var ctx = IsImageOverlayOpen ? "ImageOverlay" : IsLearning ? "Learning" : "Home";
        var query = value.Trim().ToLowerInvariant();

        var filtered = _allCommandPaletteEntries
            .Where(e => e.Contexts.Any(c => c == "*" || c == ctx))
            .Where(e => string.IsNullOrEmpty(query)
                || e.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase)
                || e.Aliases.Any(a => a.StartsWith(query, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        FilteredCommandPaletteEntries = new ObservableCollection<CommandPaletteEntry>(filtered);
        _selectedCommandIndex = filtered.Count > 0 ? 0 : -1;
        SelectedCommand = filtered.Count > 0 ? filtered[0] : null;
        OnPropertyChanged(nameof(HasFilteredCommands));
    }

    public void OpenCommandPalette()
    {
        IsCommandPaletteOpen = true;
        CommandPaletteInput = string.Empty;
    }

    public void CloseCommandPalette()
    {
        IsCommandPaletteOpen = false;
        CommandPaletteInput = string.Empty;
    }

    public void SelectNextCommand()
    {
        if (FilteredCommandPaletteEntries.Count == 0)
        {
            return;
        }

        _selectedCommandIndex = (_selectedCommandIndex + 1) % FilteredCommandPaletteEntries.Count;
        SelectedCommand = FilteredCommandPaletteEntries[_selectedCommandIndex];
    }

    public void SelectPreviousCommand()
    {
        if (FilteredCommandPaletteEntries.Count == 0)
        {
            return;
        }

        _selectedCommandIndex--;
        if (_selectedCommandIndex < 0)
        {
            _selectedCommandIndex = FilteredCommandPaletteEntries.Count - 1;
        }

        SelectedCommand = FilteredCommandPaletteEntries[_selectedCommandIndex];
    }

    public void SelectCommand(CommandPaletteEntry entry)
    {
        var idx = FilteredCommandPaletteEntries.IndexOf(entry);
        if (idx >= 0)
        {
            _selectedCommandIndex = idx;
            SelectedCommand = entry;
        }
    }

    public void ExecuteSelectedCommand()
    {
        if (_selectedCommandIndex < 0 || _selectedCommandIndex >= FilteredCommandPaletteEntries.Count)
        {
            return;
        }

        var entry = FilteredCommandPaletteEntries[_selectedCommandIndex];
        CloseCommandPalette();
        entry.Execute();
    }

    private string GetShortcutText(KeyboardActionKind action)
    {
        var binding = _keyBindingService.CurrentBindings.FirstOrDefault(b => b.Action == action);
        if (binding == null)
        {
            return string.Empty;
        }

        if (binding.Chords is { Count: > 0 })
        {
            return FormatChordDisplay(binding.Chords);
        }

        return FormatKeyForDisplay(binding.Key, binding.Modifiers);
    }

    private record PaletteMeta(string Name, string[] Aliases, string Description, string[] Contexts);

    private static readonly Dictionary<KeyboardActionKind, PaletteMeta> _paletteMeta = new()
    {
        // ── Global ─────────────────────────────────────────────
        [KeyboardActionKind.ToggleShortcutsHandbook] = new("help", ["shortcuts", "handbook", "?"], "Toggle shortcuts handbook", ["*"]),
        [KeyboardActionKind.OpenSettings] = new("settings", ["config", "prefs"], "Open settings", ["*"]),
        [KeyboardActionKind.ToggleSidebar] = new("sidebar", ["menu"], "Toggle sidebar", ["*"]),
        [KeyboardActionKind.ShowPinnedView] = new("pinned", ["pin"], "Show pinned decks", ["*"]),
        [KeyboardActionKind.ShowArchivedView] = new("archived", ["archive"], "Show archived decks", ["*"]),
        [KeyboardActionKind.ShowHeatmap] = new("heatmap", ["streak", "activity"], "Show study streak heatmap", ["*"]),
        [KeyboardActionKind.OpenDocumentation] = new("docs", ["documentation"], "Open documentation", ["*"]),
        [KeyboardActionKind.OpenTagInference] = new("tag-inference", ["ai-tags", "infer", "tag"], "Open AI tag inference page", ["*"]),
        [KeyboardActionKind.ZoomTextIn] = new("zoom-in", ["z+", "zi"], "Zoom text in", ["*"]),
        [KeyboardActionKind.ZoomTextOut] = new("zoom-out", ["z-", "zo"], "Zoom text out", ["*"]),
        [KeyboardActionKind.ResetTextZoom] = new("zoom-reset", ["z0"], "Reset text zoom", ["*"]),
        [KeyboardActionKind.OpenDecksFolder] = new("folders", ["decks", "open"], "Open decks folder in file manager", ["*"]),
        [KeyboardActionKind.NavigateToMarketplace] = new("marketplace", ["plugins", "extensions"], "Open marketplace", ["*"]),
        [KeyboardActionKind.OpenCurrentDeckFolder] = new("reveal-deck", ["reveal", "current-deck"], "Reveal current deck in file manager", ["*"]),
        [KeyboardActionKind.OpenMcqQuiz] = new("mcq", ["quiz", "exam"], "Open MCQ quiz panel", ["*"]),
        [KeyboardActionKind.OpenFlashcard] = new("flashcard", ["anki", "cards"], "Open flashcard panel", ["*"]),

        // ── Home ───────────────────────────────────────────────
        [KeyboardActionKind.FocusSearchBar] = new("search", ["find", "/"], "Focus search bar", ["Home"]),
        [KeyboardActionKind.FocusSearchWithClear] = new("clear", ["clear-search"], "Clear and focus search bar", ["Home"]),

        // ── Learning ────────────────────────────────────────────
        [KeyboardActionKind.NextPage] = new("next", ["n", ">"], "Next page", ["Learning"]),
        [KeyboardActionKind.PreviousPage] = new("prev", ["p", "<"], "Previous page", ["Learning"]),
        [KeyboardActionKind.NavigateHome] = new("home", ["exit", "quit", "q"], "Exit to home", ["Learning"]),
        [KeyboardActionKind.OpenGoToPage] = new("goto", ["go"], "Open go-to-page dialog", ["Learning"]),

        // ── Image Overlay ───────────────────────────────────────
        [KeyboardActionKind.ZoomIn] = new("img-zoom-in", ["z+"], "Zoom in on image", ["ImageOverlay"]),
        [KeyboardActionKind.ZoomOut] = new("img-zoom-out", ["z-"], "Zoom out on image", ["ImageOverlay"]),
        [KeyboardActionKind.ResetZoom] = new("img-zoom-reset", ["z0"], "Reset image zoom", ["ImageOverlay"]),
        [KeyboardActionKind.NextImage] = new("next-img", ["next"], "Next image", ["ImageOverlay"]),
        [KeyboardActionKind.PreviousImage] = new("prev-img", ["previous"], "Previous image", ["ImageOverlay"]),
    };

    private void ExecutePaletteAction(KeyboardActionKind action)
    {
        switch (action)
        {
            case KeyboardActionKind.ZoomTextIn: ZoomTextInCommand.Execute(null); break;
            case KeyboardActionKind.ZoomTextOut: ZoomTextOutCommand.Execute(null); break;
            case KeyboardActionKind.ResetTextZoom: ResetTextZoomCommand.Execute(null); break;
            case KeyboardActionKind.ZoomIn: ZoomInCommand.Execute(null); break;
            case KeyboardActionKind.ZoomOut: ZoomOutCommand.Execute(null); break;
            case KeyboardActionKind.ResetZoom: ResetZoomCommand.Execute(null); break;
            case KeyboardActionKind.NextImage: NextImageCommand.Execute(null); break;
            case KeyboardActionKind.PreviousImage: PreviousImageCommand.Execute(null); break;
            case KeyboardActionKind.OpenSettings: OpenSettingsCommand.Execute(null); break;
            case KeyboardActionKind.ToggleShortcutsHandbook: IsShortcutsHandbookOpen = !IsShortcutsHandbookOpen; break;
            case KeyboardActionKind.OpenDocumentation: NavigateToDocumentationCommand.Execute(null); break;
            case KeyboardActionKind.ToggleSidebar: ToggleSidebarCommand.Execute(null); break;
            case KeyboardActionKind.OpenDecksFolder: OpenDecksFolderCommand.Execute(null); break;
            case KeyboardActionKind.OpenCurrentDeckFolder: OpenCurrentDeckFolderCommand.Execute(null); break;
            case KeyboardActionKind.OpenTagInference: ShowTagInferenceCommand.Execute(null); break;
            case KeyboardActionKind.OpenMcqQuiz: ShowMcqPanelCommand.Execute(null); break;
            case KeyboardActionKind.OpenFlashcard: ShowFlashcardPanelCommand.Execute(null); break;
            case KeyboardActionKind.NavigateToMarketplace: NavigateToMarketplaceCommand.Execute(null); break;
            case KeyboardActionKind.ShowHeatmap: ShowHeatmapCommand.Execute(null); break;
            case KeyboardActionKind.ShowArchivedView: ShowArchivedViewCommand.Execute(null); break;
            case KeyboardActionKind.ShowPinnedView: ShowPinnedViewCommand.Execute(null); break;
            case KeyboardActionKind.FocusSearchBar: HomeViewModel.FocusSearch(); break;
            case KeyboardActionKind.FocusSearchWithClear:
                HomeViewModel.SearchText = string.Empty;
                HomeViewModel.FocusSearch();
                break;
            case KeyboardActionKind.NextPage: LearningViewModel.NextPageCommand.Execute(null); break;
            case KeyboardActionKind.PreviousPage: LearningViewModel.PreviousPageCommand.Execute(null); break;
            case KeyboardActionKind.NavigateHome: NavigateToHomeCommand.Execute(null); break;
            case KeyboardActionKind.OpenGoToPage: LearningViewModel.IsGoToPageOpen = true; break;
        }
    }

    private List<CommandPaletteEntry> BuildCommandPaletteEntries()
    {
        var seen = new HashSet<KeyboardActionKind>();
        var entries = new List<CommandPaletteEntry>();

        foreach (var binding in _keyBindingService.CurrentBindings)
        {
            if (binding.Action == KeyboardActionKind.None)
            {
                continue;
            }

            if (!seen.Add(binding.Action))
            {
                continue;
            }

            if (!_paletteMeta.TryGetValue(binding.Action, out var meta))
            {
                continue;
            }

            entries.Add(new CommandPaletteEntry
            {
                Name = meta.Name,
                Aliases = meta.Aliases,
                Description = meta.Description,
                Contexts = meta.Contexts,
                Execute = () => ExecutePaletteAction(binding.Action),
                ShortcutText = GetShortcutText(binding.Action),
            });
        }

        // Special entries without keyboard bindings
        entries.Add(new CommandPaletteEntry
        {
            Name = "focus-timer",
            Aliases = new[] { "timer", "pomodoro" },
            Description = "Open focus timer panel",
            Contexts = new[] { "*" },
            Execute = () => OpenFocusTimerCommand.Execute(null),
        });
        entries.Add(new CommandPaletteEntry
        {
            Name = "invert",
            Aliases = new[] { "inv" },
            Description = "Toggle image color inversion",
            Contexts = new[] { "ImageOverlay" },
            Execute = () => ToggleInvertCommand.Execute(null),
        });
        entries.Add(new CommandPaletteEntry
        {
            Name = "open-img",
            Aliases = new[] { "open", "view" },
            Description = "Open image in external viewer",
            Contexts = new[] { "ImageOverlay" },
            Execute = () => OpenImageInViewerCommand.Execute(null),
        });

        return entries;
    }
}

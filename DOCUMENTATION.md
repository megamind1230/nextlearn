# Cover Page

![](./screenshots/nextlearn-logo.png)

**NextLearn** — Distraction-Free Digital Study Environment

**Projekt Documentation**

**Zagazig University** — Faculty of Information Technology

**Author:** Hassan Ahmad Darwish (NSB)

**Date:** July 2026

**Version:** 1.0.0

# Table of Contents {#toc}

# Introduction

## Problem Statement

Modern learners face a fragmented landscape of study tools. The typical
student juggles multiple applications --- a note-taking app for course
content, a flashcard app for memorization, a quiz platform for
self-assessment, and a calendar or timer for study sessions. Each tool
has its own data format, its own synchronization mechanism, and
critically --- its own set of distractions.

The core problem is twofold:

1.  **Context Switching Cost** --- Moving between tools breaks the mental
    state of deep study. Every application boundary is an opportunity
    for distraction: notifications, advertisements, social features, and
    unrelated content compete for the learner's attention.

2.  **Vendor Lock-in and Data Portability** --- Most learning platforms
    store content in proprietary databases. Exporting to a different
    tool is difficult or impossible. The learner's intellectual work is
    trapped inside a single ecosystem.

3.  **Customization Constraints** --- Mainstream study apps offer limited
    keyboard customization, few theming options, and no scripting
    capabilities. Users who prefer keyboard-centric workflows (Vim,
    Emacs) or require accessibility accommodations are underserved.

4.  **AI Integration Fragmentation** --- When AI-powered features exist,
    they are typically tied to subscription tiers, have opaque
    algorithms, and cannot be used with locally-stored content.

## Solution & Significance of Work {#solution-significance}

NextLearn addresses these problems by providing a single, distraction-free
desktop application that integrates:

-   **File-based content management** --- Decks are plain-text `.md`{.verbatim}, `.org`{.verbatim},
    or `.txt`{.verbatim} files stored in a local vault. No proprietary database for
    content. Your study material is portable, version-controllable (git),
    and editable in any text editor.

-   **Keyboard-centric navigation** --- Three built-in keybinding profiles
    (Vim, Emacs, VS Code) with multi-key chords, a command palette, and
    a customizable keybinding system. Every action is accessible without
    touching the mouse.

-   **Offline-first architecture** --- All core features (deck reading,
    search, study, heatmap tracking, focus timer) work without an
    internet connection. Only optional AI features (tag inference,
    flashcard generation, MCQ generation) require the network.

-   **Integrated AI with local-first workflow** --- Google Gemini API is
    used for optional AI assistance: tag suggestion, flashcard creation,
    and MCQ quiz generation. The AI writes to local files that the user
    can review, edit, and control.

-   **Built-in assessment tools** --- MCQ quizzes with interactive WebView
    rendering, Anki-compatible flashcard export, and a Pomodoro focus
    timer --- all within the same application window.

The significance of this work lies in demonstrating that a single-d
developer can build a professional-grade, cross-platform study
environment using free and open-source technologies (.NET, Avalonia UI,
SQLite), integrate AI services without compromising local control, and
create a keyboard-first user experience that rivals mainstream
applications.

## Existing Solutions --- Limitations vs NextLearn Advantages {#existing-solutions}

  Aspect                  Anki                          Obsidian                     Roam Research   SuperMemo           NextLearn
  ----------------------- ----------------------------- ---------------------------- --------------- ------------------- --------------------------------
  Content Format          Proprietary .apkg             Markdown files               Proprietary     Proprietary .sm     .md / .org / .txt files
  Keyboard Navigation     Basic shortcuts               Limited                      Moderate        Minimal             Vim/Emacs/VS Code profiles
  AI Integration          No native AI                  Community plugins only       Limited AI      No                  Built-in Gemini (tags, MCQs)
  Assessment              SRS flashcards only           No native quizzes            No              SRS + some tests    Flashcards + MCQ + Focus Timer
  Offline Capability      Full                          Full                         Limited         Full                Full (AI optional)
  Customizability         Add-ons only                  Plugin system                Limited         Minimal             YAML config + keybinding YAML
  Cross-Platform          Windows/macOS/Linux/Android   Windows/macOS/Linux/mobile   Web-based       Windows (limited)   Linux/macOS (Windows planned)
  Study Streak Tracking   Basic                         Community plugin             No              Basic               Built-in heatmap + daily log
  File Portability        Requires export               Native (plain markdown)      Export only     Export only         Native (plain text)
  Cost                    Free                          Free (sync paid)             Subscription    Paid                Free (open source)
  Focus / Timer           No                            Community plugin             No              No                  Built-in Pomodoro + todos

Key advantages of NextLearn over existing solutions:

-   Single-application integration of reading, flashcards, quizzes, timer,
    and streak tracking --- eliminating context switching.
-   Plain-text file format as the source of truth --- not a database dump.
-   Keyboard profiles that match the user's existing muscle memory.
-   Deterministic deck identity via SHA256 file path hashing --- progress
    survives file renames and OS reinstallation.
-   AI features that write to the user's local file system, not a locked
    cloud service.

## Background

### Linux Philosophy and Customizability {#linux-philosophy}

NextLearn was developed on and primarily targets Linux. The design
philosophy draws heavily from the Unix tradition:

-   **Do one thing well** --- The app is a study tool, not a notes app,
    not a project manager, not a social network.
-   **Plain text as universal interface** --- Deck files are plain text.
    Users can edit them with any tool, version them with git, and
    transform them with shell scripts.
-   **Composability** --- The app is designed to fit into a Linux workflow.
    File manager integration, CLI-paired deck creation, and
    git-backed version history are first-class concerns.
-   **Transparency** --- All data lives in standard locations
    (`~/.config/nextlearn/`{.verbatim}, `~/nextlearn/decks/`{.verbatim}). No hidden databases
    or opaque binary formats.

### Vim and Emacs Influence {#vim-emacs-influence}

The keyboard shortcut system is directly inspired by the two great
traditions of modal editing:

-   **Vim profile** --- Single-key navigation (j/k for scroll, n/p for
    pages), modal context awareness, and the `g`{.verbatim} leader key for chords.
    The command palette (activated by `:`{.verbatim}) mirrors Vim's command-line
    mode.

-   **Emacs profile** --- Chords with modifier keys (`C-n`{.verbatim}, `C-p`{.verbatim}), prefix
    key sequences (`C-x`{.verbatim}, `C-c`{.verbatim}), and the `M-x`{.verbatim} command palette. The
    chord display indicator shows pending key sequences with a 500ms
    timeout, reminiscent of Emacs's `C-g`{.verbatim} cancel mechanism.

-   **VS Code profile** --- Familiar shortcuts for users coming from the
    most popular modern editor (`Ctrl+Shift+P`{.verbatim} for command palette,
    `Ctrl+B`{.verbatim} for sidebar, `Ctrl+W`{.verbatim} for close).

Supporting these three profiles means users can be productive within
seconds of first launch --- no need to learn a new keybinding system from
scratch.

### .NET Ecosystem and Avalonia UI {#dotnet-avalonia}

The technical foundation is .NET 10.0 with Avalonia UI, chosen for:

-   Cross-platform desktop rendering (Linux, macOS, Windows) from a single
    codebase.
-   XAML-based declarative UI with MVVM architecture via
    CommunityToolkit.Mvvm (source-generated observable properties and
    relay commands).
-   Direct hardware acceleration via Skia rendering backend.
-   NativeWebView control for HTML content rendering, enabling rich
    text display with KaTeX math and highlight.js syntax highlighting.

## Methodology

### Chosen Methodology --- Kanban (Lean/Agile) {#chosen-methodology-kanban}

NextLearn was developed using a Kanban-style approach, documented in
`plan-todos.org`{.verbatim} (1351 lines of feature planning). The choice was driven
by:

-   Single-developer project --- no need for sprint ceremonies or
    team coordination.
-   Evolving scope --- features were added based on user (self) feedback and
    practical need, not a fixed requirements document.
-   Continuous delivery --- every feature went through the same pipeline:
    plan → implement → test → document → commit.

The Kanban board is informal but effective:

``` text
TODO (plan-todos.org) → In Progress (active work) → Done (committed)

Each feature entry in `plan-todos.org`{.verbatim} includes:

-   Detailed specification with rationale
-   File-by-file change list
-   Edge case table
-   Test impact assessment
-   Progress UI step table (where applicable)

### Other Methodologies Considered {#other-methodologies}

  Methodology    Considered   Rejected Because
  -------------- ------------ ----------------------------------------------
  Waterfall      Yes          Scope evolved during development
  Scrum          Yes          Overhead for single developer
  Spiral         Yes          Risk analysis overhead not justified
  RAD            Yes          Prototyping not needed --- domain understood
  Extreme (XP)   Yes          Pair programming not applicable

## Project Objectives

1.  **Study Environment** --- Provide a distraction-free, keyboard-centric
    application for reading markdown and org-mode decks as paginated
    slides.

2.  **File-First Architecture** --- Use plain text files as the source of
    truth for all study content. No proprietary content format.

3.  **Cross-Platform Desktop** --- Target Linux (primary), macOS, and
    Windows using a single .NET/Avalonia codebase.

4.  **Integrated Learning Tools** --- Include flashcard generation, MCQ
    quizzes, and a Pomodoro timer within the same application.

5.  **Optional AI Assistance** --- Integrate AI services (Google Gemini)
    for tag suggestion, flashcard creation, and quiz generation without
    requiring internet connectivity for core features.

6.  **Progress Tracking** --- Track study streaks, daily minutes, and
    page-level progress with a persistent heatmap visualization.

7.  **Customizability** --- Provide configurable keybinding profiles,
    dark/light theme switching, and font/text-size adjustment without
    requiring plugin installation.

8.  **Keyboard-First Design** --- Support three major keybinding profiles
    (Vim, Emacs, VS Code) with multi-key chords and a command palette
    for full mouse-free operation.

# Planning & Requirements

## Project State & Scope {#project-state}

NextLearn is currently in active development with the following scope
constraints:

-   **Platform Focus**: Linux (primary). WebView rendering uses
    `libwpewebkit-2.0`{.verbatim} and related WPE dependencies available on Linux.
    macOS builds pass CI but are less tested. Windows is not currently
    supported (requires WebView2; the author lacks a Windows machine for
    consistent testing).

-   **Single-User Mode**: The app creates a local \"Guest\" user on first
    launch. No multi-user or cloud sync features.

-   **Optional AI**: All Google Gemini API features (tag inference,
    flashcard generation, MCQ generation) are optional. The app functions
    fully without them.

-   **Offline-First**: Core features require zero network connectivity.
    Only AI features depend on internet access.

## Development Tools & Environment {#dev-tools}

The project was developed using a deliberately minimal toolchain.
Experience with the tools shaped both the development process and the
design philosophy:

  Tool                  Purpose                                         Notes
  --------------------- ----------------------------------------------- -----------------------------------------------------------------------
  VS Code               Primary IDE for C# and XAML development         With C# Dev Kit, Avalonia, and Markdown plugins
  Vim                   Fast edits, configuration file changes, shell   Used for quick file modifications
  Emacs (org-mode)      Documentation and planning                      All .org files (README, changelog, bugs, plan-todos) created in Emacs
  Git (CLI)             Version control                                 Command-line git, no GUI wrapper
  JetBrains Rider       Tried as alternative IDE                        Abandoned: too heavy for the low-end development machine
  Avalonia Hot Reload   XAML live preview                               Discovered late in development cycle; never used in practice

The \"Avalonia Hot Reload\" discovery is notable: the author was unaware
that Avalonia supported XAML live reloading until approximately 80% of
the core UI was complete. By that point, the edit-compile-run cycle was
already embedded in the workflow, and adopting hot reload mid-project
offered marginal benefit. This is documented as a lesson for future
projects.

## Technology Stack {#tech-stack}

  Technology                     Version          Purpose
  ------------------------------ ---------------- -------------------------------------------------------------------------
  .NET                           10.0             Runtime framework
  Avalonia UI                    11.3.12          Cross-platform desktop UI framework
  CommunityToolkit.Mvvm          8.2.1            Source-generated MVVM pattern
  Entity Framework Core          8.0.0            ORM for SQLite
  SQLite                         (EF Core)        Embedded local database
  Serilog                        4.3.1            Structured logging (file + console)
  NativeWebView                  0.1.0-alpha.3    System WebView HTML rendering
  NativeWebView.Platform.Linux   0.1.0-alpha.3    Linux WebView backend (WPE WebKit)
  YamlDotNet                     18.0.0           YAML serialization (settings, keybindings, focus timer data)
  highlight.js                   11.11.1          Code syntax highlighting (52 languages, bundled as custom-highlight.js)
  KaTeX                          (bundled)        LaTeX math rendering (katex.min.js, katex.min.css, auto-render)
  StyleCop.Analyzers             1.2.0-beta.556   C# code style enforcement
  Google Gemini API              REST             AI tag inference, flashcard generation, MCQ generation
  xUnit                          (latest)         Unit testing framework
  FluentAssertions               (latest)         Readable test assertions
  NSubstitute                    (latest)         Mocking framework

## System Dependencies (Linux runtime) {#system-dependencies}

-   libwpewebkit-2.0 (WebView engine)
-   libwpe-1.0
-   libWPEBackend-fdo-1.0
-   Wayland auto-detection: `XDG_SESSION_TYPE=wayland`{.verbatim} → X11 fallback
    via `UseSkia().UseX11()`{.verbatim}

## Functional Requirements

### Core Deck Management

-   FR-01: Read `.md`{.verbatim}, `.org`{.verbatim}, and `.txt`{.verbatim} files from a configurable vault
    directory (default `~/nextlearn/decks/`{.verbatim})
-   FR-02: Parse YAML frontmatter for title, description, and tags in
    `.md`{.verbatim} and `.org`{.verbatim} files
-   FR-03: Split decks into paginated slides based on heading hierarchy
    (`#`{.verbatim} / `##`{.verbatim} for sections and pages)
-   FR-04: Auto-refresh deck list when files change via FileSystemWatcher
-   FR-05: Search decks by title, description, filename, and tags (with
    prefix operators `file:`{.verbatim}, `title:`{.verbatim}, `desc:`{.verbatim}, `tags:`{.verbatim})
-   FR-06: Regex toggle for search
-   FR-07: Pin/archive/unpin/unarchive decks via file rename convention
    (`+.md`{.verbatim} for pinned, `.md~`{.verbatim} for archived)
-   FR-08: Deterministic deck identity via SHA256 file path hash
-   FR-09: Recursive vault scanning with subdirectory support
-   FR-10: Open decks folder in system file manager

### Study & Navigation

-   FR-26: Three built-in keybinding profiles (Vim, Emacs, VS Code)
-   FR-27: Customizable keybinding via YAML config file
-   FR-28: Multi-key chord support with 500ms timeout and visual
    indicator
-   FR-29: Command palette with fuzzy search (activated by `:`{.verbatim} or `M-x`{.verbatim})
-   FR-30: Shortcuts handbook (auto-generated from current profile)
-   FR-31: 67 keyboard action types across multiple contexts (Learning,
    Home, ImageOverlay, McqQuiz)
-   FR-32: Esc-based overlay closing priority chain

### AI Features (Google Gemini) {#ai-features}

-   FR-33: AI tag inference --- suggest 2--15 tags per deck, diff preview,
    apply to frontmatter (supports 7 tag formats)
-   FR-34: Anki flashcard generation --- Basic (TSV) and Cloze ({{c1::}})
    modes, separate prompts, save to `.basic.txt`{.verbatim} / `.cloze.txt`{.verbatim} files
-   FR-35: MCQ quiz generation --- AI-generated questions, interactive
    quiz in dedicated WebView (2x2 option grid, timer, scoring)
-   FR-36: Model auto-discovery and fallback chain (gemini-2.5-flash →
    gemini-2.0-flash → etc.)
-   FR-37: Exponential retry with backoff (1s, 5s, 10s, 20s, 40s, 80s)
-   FR-38: Token estimation with 700K truncation limit

### Progress & Analytics

-   FR-39: Per-user progress tracking (current page, completed decks)
-   FR-40: Daily activity tracking (pages viewed, minutes learned)
-   FR-41: Study streak heatmap (snake layout, 6-level orange, green for
    today)
-   FR-42: Stable progress across restarts (SQLite persistence)

### Focus Timer

-   FR-43: Pomodoro-style work/break countdown timer
-   FR-44: Configurable work/break durations in settings
-   FR-45: Persistent todo list (CRUD) with YAML persistence
-   FR-46: Session history log
-   FR-47: Sound notification at session end

### Settings & Configuration

-   FR-48: Theme switching (Dark/Light) with live preview
-   FR-49: Configurable decks, MCQs, and flashcards paths
-   FR-50: Gemini API key configuration with status indicator
-   FR-51: Configurable font selection
-   FR-52: Settings persistence in `settings.yaml`{.verbatim}
-   FR-53: Save/Reset settings

## Non-Functional Requirements

  ID       Requirement                  Description
  -------- ---------------------------- ---------------------------------------------------------------
  NFR-1    Offline Operation            Core features must work without internet
  NFR-2    Startup Time                 App must launch within 3 seconds
  NFR-3    File Change Responsiveness   Deck list must refresh within 500ms of file change
  NFR-4    Memory Footprint             Must run on 4GB RAM systems (low-end machine target)
  NFR-5    Single-User Architecture     No multi-user sync or authentication required
  NFR-6    Data Durability              No data loss on crash --- SQLite WAL mode, YAML atomic writes
  NFR-7    Keyboard-Only Operation      All features accessible without mouse
  NFR-8    Open Data Format             All user data in plain text (.md, .org, .yaml, .txt)
  NFR-9    Cross-Platform Compatible    Builds on Linux (x64), macOS (arm64), Windows (x64)
  NFR-10   No Vendor Lock-In            User can stop using the app and keep all data intact
  NFR-11   CI/CD Integration            Automated build + test on every push via GitHub Actions
  NFR-12   Single-File Distribution     Published as self-contained executable

# System Design

## System Architecture

NextLearn follows a layered MVVM (Model-View-ViewModel) architecture
with a single-window UI pattern.

![](diagrams/architecture.png)

### Single-Window UI Pattern {#single-window-ui}

Unlike most desktop applications that use multiple windows or a
navigation framework, NextLearn uses a single `MainWindow.axaml`{.verbatim} with
approximately 15 boolean visibility flags to show/hide panels:

``` text
IsLearning        → learning view (WebView content + pagination)
IsSettingsOpen    → settings overlay
IsSidebarOpen     → sidebar panel
IsTagInferenceOpen → tag inference panel
IsFlashcardOpen   → flashcard generation panel
IsMcqOpen         → MCQ quiz panel (3 tabs)
IsTimerOpen       → focus timer panel
IsPinnedViewOpen   → pinned decks overlay
IsArchivedViewOpen → archived decks overlay
IsHeatmapOpen     → streak heatmap overlay
IsCommandPaletteOpen → command palette
IsShortcutsHandbookOpen → shortcuts handbook
IsGoToPageOpen    → go-to-page dialog
IsImageOverlayOpen → image overlay
IsDeckLinkPromptOpen → deck link navigation prompt

This approach eliminates navigation stacks and keeps the memory
footprint minimal. Only one panel is visible at a time (except sidebar,
which overlays).

## UML Diagrams

### Use Case Diagram

![](diagrams/usecase.png)

### Class Diagram --- Core Models

![](diagrams/class-models.png)

### Class Diagram --- Core Services

![](diagrams/class-services.png)

### Activity Diagram --- Study Flow

![](diagrams/activity-study.png)

### Activity Diagram --- MCQ Quiz Flow {#activity-diagram-mcq-flow}

![](diagrams/activity-mcq.png)

### Sequence Diagram --- AI Tag Inference {#sequence-diagram-tag-inference}

![](diagrams/sequence-tag-inference.png)

### ER Diagram --- Database Schema {#er-diagram}

![](diagrams/er-database.png)

## Database Design

The SQLite database is stored at `~/.config/nextlearn/nextlearn.db`{.verbatim} and
contains six tables managed by Entity Framework Core 8.0.0. The database
stores only metadata and progress --- deck content lives in the file system.

### Table: Users

``` text
Column              Type        Constraints         Notes
─────────────────────────────────────────────────────────────────
Id                  GUID        PK                  Auto-generated
DisplayName         string      NOT NULL            Default "Guest"
CreatedAt           DateTime    NOT NULL
TotalDecksCompleted int         NOT NULL            Counter
CurrentStreak       int         NOT NULL            Computed from daily activity
LastActiveDate      DateTime    NOT NULL
IsGuest             bool        NOT NULL            Always true (single-user)

### Table: Decks

``` text
Column              Type        Constraints         Notes
─────────────────────────────────────────────────────────────────
Id                  GUID        PK                  Deterministic (SHA256 of path)
FileName            string(500) NOT NULL            Relative vault path
Title               string(200) NOT NULL            Indexed
HasExplicitTitle    bool        NOT NULL
Description         string(1000) NOT NULL
Tags                string(500) NOT NULL            Comma-separated
AuthorId            GUID        FK → Users.Id
IsPublished         bool        NOT NULL
IsReviewed          bool        NOT NULL
IsArchived          bool        NOT NULL            '~' suffix convention
IsPinned            bool        NOT NULL            '+' prefix convention
CreatedAt           DateTime    NOT NULL
PageCount           int         NOT NULL

### Table: Pages

``` text
Column              Type        Constraints                   Notes
─────────────────────────────────────────────────────────────────────────
Id                  GUID        PK
DeckId              GUID        FK → Decks.Id
PageNumber          int         NOT NULL                      1-based
SectionTitle        string(200) NULLABLE                      From # heading
Title               string(200) NOT NULL                      From ## or first line
ContentType         enum        NOT NULL                      Text (only value)
TextContent         string?     NULLABLE                      Raw markdown/org

UNIQUE INDEX: (DeckId, PageNumber)

### Table: UserProgress

``` text
Column              Type        Constraints                   Notes
─────────────────────────────────────────────────────────────────────────
Id                  GUID        PK
UserId              GUID        FK → Users.Id
DeckId              GUID        FK → Decks.Id
CurrentPage         int         NOT NULL                      Default 1
IsCompleted         bool        NOT NULL
LastAccessedAt      DateTime    NOT NULL

UNIQUE INDEX: (UserId, DeckId)

### Table: ActiveLearning

``` text
Column              Type        Constraints                   Notes
─────────────────────────────────────────────────────────────────────────
Id                  GUID        PK
UserId              GUID        FK → Users.Id
DeckId              GUID        FK → Decks.Id
Slot                int         NOT NULL                      Up to 2 concurrent

UNIQUE INDEX: (UserId, Slot)

### Table: DailyActivities

``` text
Column              Type        Constraints                   Notes
─────────────────────────────────────────────────────────────────────────
Id                  GUID        PK
UserId              GUID        FK → Users.Id
Date                DateTime    NOT NULL                      Wall-clock date
PagesViewed         int         NOT NULL
MinutesLearned      int         NOT NULL

UNIQUE INDEX: (UserId, Date)

## User Flow Diagrams

### End-to-End Study Flow {#e2e-study-flow}

![](diagrams/flow-study.png)

### AI Feature Usage Flow {#ai-feature-flow}

![](diagrams/flow-ai.png)

## Core Services

NextLearn is built around 21 key services, each with a specific
responsibility. Below is the complete inventory with roles and file
locations:

  Service                   File                                          Role
  ------------------------- --------------------------------------------- ----------------------------------------------------------------------------
  ThemeHelper               Services/ThemeHelper.cs                       Runtime dark/light color dictionaries, live ApplyTheme()
  DeckFileParser            Services/DeckFileParser.cs                    Static parser: frontmatter + heading → Deck+Pages
  DeckFileIdentity          Services/DeckFileIdentity.cs                  SHA256 of path → deterministic GUID
  DeckService               Services/DeckService.cs                       DB layer: CRUD for decks, pages, progress
  DeckFileService           Services/DeckFileService.cs                   File ops: archive/pin/rename (no DB dependency)
  UserService               Services/UserService.cs                       Guest user creation, streak computation, daily activity recording
  SettingsService           Services/SettingsService.cs                   YAML persistence for theme/font/paths/API key/keybindings
  HtmlContentBuilder        Services/HtmlContentBuilder.cs (1049 lines)   Static HTML builder from Page model (markdown/org → HTML)
  HtmlContentService        Services/HtmlContentService.cs                HTML enrichment with KaTeX, highlight.js, copy buttons
  FalconEyeBuilder          Services/FalconEyeBuilder.cs                  Table-of-contents HTML generation from Deck headings
  MarkdownInlineRenderer    Services/MarkdownInlineRenderer.cs            Strategy for .md inline rendering (bold, italic, code, links)
  OrgInlineRenderer         Services/OrgInlineRenderer.cs                 Strategy for .org inline rendering (same constructs + org syntax)
  TagInferenceService       Services/TagInferenceService.cs               Gemini API client: model discovery, retry, tag parsing (435 lines)
  TagInferenceResult        Services/TagInferenceResult.cs                AI tag suggestion result DTO
  DeckFileWriter            Services/DeckFileWriter.cs                    Tag write-back (7 formats) + frontmatter health check
  FlashcardService          Services/FlashcardService.cs                  Gemini API client: basic+cloze generation, mode-aware parsing
  FlashcardGenerationMode   Services/FlashcardGenerationMode.cs           Enum: Basic / Cloze
  FlashcardResult           Services/FlashcardResult.cs                   AI flashcard generation result DTO
  McqFileParser             Services/McqFileParser.cs                     Parse/serialize .mcq files (YAML frontmatter + question blocks)
  McqFileService            Services/McqFileService.cs                    List/read/delete .mcq files
  McqQuizHtmlBuilder        Services/McqQuizHtmlBuilder.cs                Interactive quiz HTML (2×2 grid, timer, scoring, review)
  McqGenerationService      Services/McqGenerationService.cs              Gemini API client for MCQ generation
  McqGenerationResult       Services/McqGenerationResult.cs               MCQ generation result DTO
  McqResultLogger           Services/McqResultLogger.cs                   Logs quiz results to .mcq.result files
  McqFileInfo               Services/McqFileInfo.cs                       MCQ file metadata DTO
  KeyBindingService         Services/KeyBindingService.cs (732 lines)     Keyboard profiles: Vim/Emacs/VS Code/Custom; binding definitions
  KeyboardHandler           Services/KeyboardHandler.cs                   Routes key events to actions by context + profile
  KeyboardActionKind        Services/KeyboardActionKind.cs                Enum: 67 possible keyboard actions
  WebViewBridge             Services/WebViewBridge.cs                     WebView↔native communication (key bridge, URI interception, image overlay)
  DeckFilter                Services/DeckFilter.cs                        Search tokenizer with <file:/title:/desc:/tags>: prefixes
  ViewLocator               ViewLocator.cs                                ViewModel→View resolution via naming convention

# Testing Strategy

## Unit Test Framework

NextLearn uses xUnit as the test framework with FluentAssertions for
readable assertions and NSubstitute for mocking.

Test project location: `tests/NextLearn.Desktop.Tests/`{.verbatim}

## Test Files

  Test File                    Lines   Scope
  ---------------------------- ------- ----------------------------------------------------
  DeckFileParserTests.cs       \~200   .md/.org/.txt parsing, frontmatter, headings
  DeckFileServiceTests.cs      \~150   Pin/archive/rename file ops, subdirectory
  DeckServiceTests.cs          472     DB CRUD, progress tracking, metadata sync
  EdgeCaseTests.cs             223     Empty files, special chars, cross-format edge
  HtmlContentBuilderTests.cs   \~200   HTML output, inline rendering, wiki-link detection
  SettingsServiceTests.cs      139     YAML load/save, path resolution, defaults
  UserServiceTests.cs          \~200   User creation, streak computation, daily activity

## Test Approach

-   **Repository pattern for DB**: Entity Framework Core InMemory provider
    for database tests --- no real SQLite dependency.
-   **File system isolation**: DeckFileServiceTests use temporary
    directories created and cleaned per test.
-   **Mocked HTTP**: TagInferenceService, FlashcardService, and
    McqGenerationService tests use mocked HttpClient handlers to avoid
    real API calls.
-   **Round-trip testing**: Parser tests follow parse→serialize→re-parse
    cycles with assertion of equivalence.
-   **Edge case tables**: plan-todos.org contains exhaustive edge case
    tables for every AI feature, with each case tested.

## CI/CD Pipeline

GitHub Actions (`.github/workflows/ci.yml`{.verbatim}) runs on every push:

``` text
Trigger: push to any branch
Matrix: ubuntu-latest, windows-latest, macos-latest

Steps:
  1. Checkout repository
  2. Setup .NET 10.0 SDK
  3. dotnet restore
  4. dotnet build (TreatWarningsAsErrors enforced)
  5. dotnet test (all platforms)
  6. dotnet publish (single-file self-contained)
     - linux-x64
     - osx-arm64
     - win-x64
  7. Upload artifacts

# UI/UX Design

## Logo & Branding

![](./screenshots/nextlearn-logo.png)

The NextLearn logo features a stylized brain/network motif in purple
gradient (#7C0AED primary). The design communicates:

-   Neural connections (learning)
-   Structured thinking (network graph)
-   Modern, minimalist aesthetic

The application icon is rendered in the title bar and used as the
application launcher icon (installed via `install-icon.sh`{.verbatim} on Linux).

## Screenshot Showcase

### Home & Navigation {#screenshots-home-navigation}

![](./screenshots/home-search-page.png)

**Figure 1: Home page with search bar, showing filtered deck list.**

![](./screenshots/sidebar-panel.png)

**Figure 2: Sidebar panel with navigation links and settings.**

![](./screenshots/settings-panel.png)

**Figure 3: Settings page --- theme, font, paths, keybinding profile, Gemini API key.**

![](./screenshots/pinned-panel.png)

**Figure 4: Pinned decks overlay for quick access to favorite decks.**

![](./screenshots/archived-panel.png)

**Figure 5: Archived decks overlay.**

![](./screenshots/open-decks-directory-feature.png)

**Figure 6: Open decks folder in system file manager.**

![](./screenshots/reveal-deck-feature.png)

**Figure 7: Reveal current deck file in file manager.**

### Study Page {#screenshots-study-page}

![](./screenshots/study-page.png)

**Figure 8: Main study view with deck loaded --- page content, page counter,
section breadcrumbs.**

![](./screenshots/zoom-in-text.png)

**Figure 9: Text zoomed in via Ctrl+Shift++.**

![](./screenshots/zoom-out-text.png)

**Figure 10: Text zoomed out via Ctrl+Shift+-.**

### Command Palette & Handbook {#screenshots-command-palette}

![](./screenshots/cmd-palette-feature.png)

**Figure 11: Command palette for quick action lookup.**

![](./screenshots/shortcuts-handbook-feature.png)

**Figure 12: Shortcuts handbook, auto-generated from current keybinding profile.**

### Falcon Eye (TOC) {#screenshots-falcon-eye}

![](./screenshots/falcon-eye-feature.png)

**Figure 13: Falcon Eye table of contents --- clickable entries navigate to
exact page.**

### Image Overlay {#screenshots-image-overlay}

![](./screenshots/image-overlay-feature.png)

**Figure 14: Image overlay with navigation, zoom slider, and toolbar.**

![](./screenshots/image-overlay-invert-colors.png)

**Figure 15: Image overlay with inverted colors for dark mode readability.**

### Heatmap & Streaks {#screenshots-heatmap}

![](./screenshots/heatmap-streak-panel.png)

**Figure 16: Study streak heatmap --- snake layout, 6-level orange scale,
green for today.**

### Tag Inference {#screenshots-tag-inference}

![](./screenshots/tags-inferencing-panel.png)

**Figure 17: AI tag inference panel using Google Gemini --- select deck,
diff preview, apply with one click.**

### Flashcard Generation {#screenshots-flashcard-generation}

![](./screenshots/anki-flashcards-generation-panel.png)

**Figure 18: Anki flashcard generation panel --- Basic and Cloze modes.**

### MCQ Quiz {#screenshots-mcq-quiz}

![](./screenshots/mcq-generate-tab.png)

**Figure 19: MCQ quiz generate tab --- configure and generate via Gemini.**

![](./screenshots/mcq-take-quiz-tab.png)

**Figure 20: MCQ quiz take tab --- select a quiz and start interactive session.**

![](./screenshots/mcq-interactive-quiz-ui.png)

**Figure 21: Interactive MCQ quiz WebView --- 2×2 option grid, scoring, timer.**

![](./screenshots/mcq-quiz-logs-tab.png)

**Figure 22: MCQ quiz logs tab --- review past quiz results and performance.**

### Theme System {#screenshots-theme}

![](./screenshots/default-dark-theme.png)

**Figure 23: Default dark theme.**

![](./screenshots/default-light-theme.png)

**Figure 24: Default light theme.**

![](./screenshots/default-light-theme-sidebar.png)

**Figure 25: Sidebar in light theme.**

![](./screenshots/default-light-theme-heatmap.png)

**Figure 26: Heatmap in light theme.**

### Focus Timer {#screenshots-focus-timer}

![](./screenshots/focus-timer-panel.png)

**Figure 27: Focus timer panel --- Pomodoro timer, tasks CRUD, and session history.**

## Theme System {#theme-system}

NextLearn features a live-switchable dark/light theme system with 45+
named resource keys defined in `ThemeHelper.cs`{.verbatim}.

### Architecture {#theme-architecture}

1.  All colors centralized as named resources (`PageBgBrush`{.verbatim},
    `TextPrimaryBrush`{.verbatim}, etc.) in `App.axaml → Application.Resources`{.verbatim}
2.  All 13 `.axaml`{.verbatim} files refactored from inline hex to
    `{StaticResource ResourceKey`{.verbatim}...}=
3.  `ThemeHelper.cs`{.verbatim} contains two dictionaries (`DarkColors`{.verbatim} and
    `LightColors`{.verbatim}) with `ApplyTheme(string? theme)`{.verbatim} method
4.  Theme setting persisted in `settings.yaml`{.verbatim}

### Color Palettes

  Resource Key                                          Dark Value   Light Value
  ----------------------------------------------------- ------------ -------------
  `PageBgBrush`{.verbatim}                              #0F172A      #F8FAFC
  `PanelBgBrush`{.verbatim}                             #1E293B      #FFFFFF
  `SurfaceBgBrush`{.verbatim}                           #334155      #F1F5F9
  `TextPrimaryBrush`{.verbatim}                         #E2E8F0      #1E293B
  `TextSecondaryBrush`{.verbatim}                       #94A3B8      #64748B
  `TextMutedBrush`{.verbatim}                           #64748B      #94A3B8
  `BorderDefaultBrush`{.verbatim}                       #334155      #E2E8F0
  `ButtonSecondaryBgBrush=| #334155   | #E2E8F0     |   #7C0AED      #7C0AED
  | =AccentBrush`{.verbatim}                                         

Complementary colors (green for today, warning amber, etc.) maintain
readability across both themes.

# Proposed Workflow

This section describes the intended end-to-end workflow for a typical
user session with NextLearn.

## Typical Evening Study Session {#typical-evening-session}

1.  **Launch**: User starts NextLearn. The app creates a guest user
    (first run), scans the `~/nextlearn/decks/`{.verbatim} directory recursively,
    syncs file metadata with the SQLite database, and displays the deck
    list on the Home screen.

2.  **Search and Select**: User types `#programming`{.verbatim} in the search bar
    to filter decks tagged with \"programming\". The deck list narrows
    in real-time.

3.  **Start Study**: User presses `Enter`{.verbatim} on the selected deck. The
    app loads the deck file, parses it into pages, builds HTML with
    KaTeX/highlight.js enrichment, and displays the first page in the
    WebView.

4.  **Navigate**: User reads through pages using `N`{.verbatim} (next) and `P`{.verbatim}
    (previous). The section breadcrumb shows current position:
    `"Data Structures → Binary Trees "`{.verbatim}. Progress is saved to the
    database after each page navigation.

5.  **Deep Work**: User activates the Focus Timer (sidebar → Timer,
    or command palette). Starts a 25-minute Pomodoro session. The
    study box content is still visible. Timer counts down with a
    sound notification at session end.

6.  **Generate Flashcards**: After studying, user opens the Flashcard
    panel, clicks `Cloze`{.verbatim} on the current deck. Gemini generates
    fill-in-the-blank cards. User reviews and accepts → saved as
    `binary-trees.cloze.txt`{.verbatim} for Anki import.

7.  **Self-Assessment**: User opens the MCQ Quiz panel, generates 10
    questions from the deck via Gemini. Takes the interactive quiz
    in the dedicated WebView --- 2×2 option grid, timer running.
    Review the score summary at the end.

8.  **Review Progress**: User opens the Heatmap overlay (Ctrl+H).
    The snake-layout heatmap shows the past 12 months of study
    activity. Today's cell is green. Current streak is displayed.

9.  **Exit**: User closes the app. All progress is persisted. The
    focus timer session was logged, the heatmap was updated, and
    the quiz results were saved as `.mcq.result`{.verbatim} files.

## Alternative Workflows

-   **New Deck Creation**: User creates a `.md`{.verbatim} file outside the app
    (Vim, Emacs, VS Code), saves it to `~/nextlearn/decks/`{.verbatim}. The app
    auto-refreshes (FileSystemWatcher, 300ms debounce) and the new
    deck appears in the list.

-   **Deck Editing**: User modifies a deck file externally. Next
    time the deck is opened, the app reads the latest file content.
    Progress is linked by SHA256 identity --- renaming the file does
    not lose progress.

-   **Tag Maintenance**: User opens the Tag Inference panel, selects
    a deck that has no tags, clicks \"Infer Tags\". Gemini suggests
    5 tags. User reviews, removes one, applies the remaining 4 ---
    the tags are written to the YAML frontmatter preserving the
    original format.

-   **Cross-Deck Navigation**: While studying a deck, user clicks a
    wiki-link `[[related topic]]`{.verbatim}. The app checks for the target
    deck (.md → .org fallback), shows a prompt, and navigates to
    the linked deck's first page.

# Limitations & Risk Analysis {#limitations-risk}

## Linux WebView Constraint

-   **Issue**: NativeWebView on Linux depends on `libwpewebkit-2.0`{.verbatim} and
    related WPE WebKit libraries. These are available on major
    distributions but may not be pre-installed.
-   **Windows**: Not currently supported --- requires WebView2 runtime
    while the app uses the system WebView API available on Linux.
-   **Mitigation**: Wayland detection with X11 fallback in Program.cs.
    clear error messages for missing dependencies.
-   **Risk**: Low (Linux is the primary target). Windows support would
    require a separate WebView backend (e.g., WebView2).

## Google Gemini API Dependency {#gemini-dependency}

-   **Issue**: AI features (tag inference, flashcards, MCQ generation)
    require a Google Gemini API key and internet connectivity.
-   **Rate Limits**: Free tier has quota limits. The app implements
    exponential retry (1s, 5s, 10s, 20s, 40s, 80s) and model fallback
    chain, but extended outages break AI features.
-   **Mitigation**: All AI features are optional. The core study
    functionality works fully offline. API responses are saved as local
    files --- the user is never locked into any AI provider.
-   **Risk**: Low (AI is additive, not required).

## Single-User Offline Model {#single-user-offline}

-   **Issue**: No multi-user support, no cloud sync, no authentication.
-   **Mitigation**: By design --- the application targets individual
    learners who value privacy and data ownership over collaboration.
    File-based format makes manual sync trivial (git, rsync, Dropbox).
-   **Risk**: Acceptable for the project scope.

## No Automated UI Tests {#no-ui-tests}

-   **Issue**: The test suite covers service and parser logic but has
    no UI automation tests (Avalonia UI testing framework not integrated).
-   **Mitigation**: ViewModels contain minimal logic --- most complexity
    is in Services layer which is well-tested.
-   **Risk**: Low-medium. UI bugs are caught manually during development.

## Performance on Very Large Decks {#performance-large-decks}

-   **Issue**: Single HTML string built for entire deck content could
    be slow for decks with 1000+ pages.
-   **Mitigation**: Current usage targets decks of 10--100 pages.
    Token estimation in AI services warns at \>700K tokens.
-   **Risk**: Low --- file-based architecture makes it easy to split
    large decks into multiple files.

# Conclusion & Future Work {#conclusion-future}

## Conclusion

NextLearn demonstrates that a single developer can build a
professional-grade, distraction-free study environment using free
and open-source tools. The project's file-first architecture ensures
that users own their data in plain text. The keyboard-centric design,
inspired by Vim and Emacs, provides a mouse-free experience unmatched
by mainstream study applications. The optional AI integration via
Google Gemini adds modern convenience without compromising local
control.

The project is currently in active development with all core features
implemented: deck management, paginated study, image rendering, LaTeX
math, code syntax highlighting, AI tag inference, flashcard generation,
interactive MCQ quizzes, a focus timer, and streak tracking. The
application builds and runs on Linux (primary) and macOS, with GitHub
Actions CI ensuring cross-platform compatibility.

At approximately 20,000+ lines of code across 41 service files,
13 ViewModels, 14 user controls, and 6 database tables, NextLearn is
a substantial software engineering project that applies modern .NET
practices (MVVM, EF Core, dependency injection preparation, structured
logging, YAML configuration) to a focused domain problem.

## Future Potential Work & Features {#future-work}

### Short-Term (Next 3 Months) {#short-term-future}

-   **Marketplace** --- Browse and download community decks from within
    the app (placeholder already exists).
-   **Theme presets / custom themes** --- User-defined color schemes.
-   **Tag cloud / tag filter** --- Visual tag browsing and filtering.
-   **Spaced repetition** --- Basic 24h review → full SRS algorithm
    (SM-2 or FSRS).

### Medium-Term (3--6 Months) {#medium-term-future}

-   **Windows support** --- WebView2 backend for full cross-platform coverage.
-   **Mobile companion** --- Phone-optimized reading (PWA or MAUI).
-   **Multi-user profiles** --- Switch between users within the app.
-   **Anki direct sync** --- Two-way sync with AnkiWeb via AnkiConnect.
-   **Export formats** --- PDF, EPUB, HTML single-page export.

### Long-Term (6--12 Months) {#long-term-future}

-   **Plugin system** --- Scriptable extensions (JavaScript/Python plugins).
-   **Real-time collaboration** --- Shared study sessions with sync.
-   **Audio support** --- Text-to-speech for auditory learning.
-   **Gamification** --- Achievements, levels, leaderboards (optional).
-   **AI model flexibility** --- Support for local LLMs (Ollama, llama.cpp)
    and alternative providers (OpenAI, Anthropic).
-   **Mobile app** --- Native iOS/Android via Avalonia MAUI target.

# References & Inspiration

## Technology References {#tech-references}

-   Avalonia UI: <https://avaloniaui.net/> --- Cross-platform desktop UI
    framework for .NET
-   CommunityToolkit.Mvvm: <https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/>
    --- Source-generated MVVM pattern
-   Entity Framework Core: <https://learn.microsoft.com/en-us/ef/core/>
    --- ORM for .NET
-   Serilog: <https://serilog.net/> --- Structured logging
-   YamlDotNet: <https://github.com/aaubry/YamlDotNet> --- YAML serialization
-   highlight.js: <https://highlightjs.org/> --- Syntax highlighting (52 languages)
-   KaTeX: <https://katex.org/> --- Fast LaTeX math rendering
-   Google Gemini API: <https://ai.google.dev/> --- AI model API
-   NativeWebView: <https://github.com/arthurrump/NativeWebView> ---
    Avalonia WebView control
-   xUnit: <https://xunit.net/> --- Unit testing framework
-   FluentAssertions: <https://fluentassertions.com/> --- Readable test assertions
-   NSubstitute: <https://nsubstitute.github.io/> --- Mocking framework

## Design Inspiration

-   **Anki** (<https://apps.ankiweb.net/>) --- Spaced repetition flashcard
    system
-   **Obsidian** (<https://obsidian.md/>) --- Plain-text knowledge base with
    wiki-links and vault concept
-   **Roam Research** (<https://roamresearch.com/>) --- Note-taking with
    bidirectional links
-   **SuperMemo** (<https://supermemo.guru/>) --- Early SRS pioneer
-   **Vim** (<https://www.vim.org/>) --- Modal text editing with keyboard
    shortcuts
-   **GNU Emacs** (<https://www.gnu.org/software/emacs/>) --- Extensible
    editor with chord-based keybindings
-   **Visual Studio Code** (<https://code.visualstudio.com/>) --- Modern
    editor keybinding conventions

## Academic References

-   Wozniak, P. A. (1995). **Economics of learning**. SuperMemo World.
-   Cepeda, N. J., et al. (2006). **Distributed practice in verbal recall
    tasks: A review and quantitative synthesis**. Psychological Bulletin.
-   Ebbinghaus, H. (1885). **Memory: A Contribution to Experimental
    Psychology**. (Classic forgetting curve research)
-   Cirillo, F. (2006). **The Pomodoro Technique**. (Time management
    methodology)

# License & Source Code {#license-source}

## License

This project is currently distributed without an explicit license file.
All rights reserved by the author. Users may view and build the source
code for personal use. A permissive open-source license (MIT) is planned
for future release.

## Source Code

Repository: <https://github.com/megamind1230/testing-nextlearn>

Branch: main (default branch)

The repository contains:

-   `NextLearn.Desktop/`{.verbatim} --- Avalonia UI desktop application
-   `tests/NextLearn.Desktop.Tests/`{.verbatim} --- xUnit unit tests
-   `screenshots/`{.verbatim} --- Application screenshots gallery
-   `.github/workflows/ci.yml`{.verbatim} --- GitHub Actions CI configuration
-   Documentation files: `README.org`{.verbatim}, `DOCUMENTATION.org`{.verbatim},
    `changelog.org`{.verbatim}, `plan-todos.org`{.verbatim}, `bugs.org`{.verbatim}, `screenshots.org`{.verbatim}

Build instructions:

``` {.bash org-language="sh"}
git clone https://github.com/megamind1230/testing-nextlearn
cd testing-nextlearn
dotnet build NextLearn.Desktop/NextLearn.Desktop.csproj -c Release
dotnet run --project NextLearn.Desktop

To produce a portable single-file binary:

``` {.bash org-language="sh"}
dotnet publish NextLearn.Desktop/NextLearn.Desktop.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o publish

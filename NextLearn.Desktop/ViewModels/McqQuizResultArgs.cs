using System;

namespace NextLearn.Desktop.ViewModels;

public class McqQuizResultArgs : EventArgs
{
    public string Score { get; init; } = string.Empty;

    public int CorrectCount { get; init; }

    public int TotalCount { get; init; }

    public string Title { get; init; } = string.Empty;

    public string SourceDeck { get; init; } = string.Empty;

    public string TimeTaken { get; init; } = string.Empty;
}

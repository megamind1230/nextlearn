using System.Collections.Generic;

namespace NextLearn.Desktop.Models;

public class McqQuestion
{
    public string Question { get; set; } = string.Empty;

    public List<string> Options { get; set; } = [];

    public int CorrectIndex { get; set; }

    public string? Explanation { get; set; }

    public int? SelectedIndex { get; set; }

    public bool IsCorrect => SelectedIndex.HasValue && SelectedIndex.Value == CorrectIndex;

    public bool IsAnswered => SelectedIndex.HasValue;
}

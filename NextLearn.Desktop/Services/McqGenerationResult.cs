using System.Collections.Generic;
using NextLearn.Desktop.Models;

namespace NextLearn.Desktop.Services;

public class McqGenerationResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public List<McqQuestion> Questions { get; set; } = [];

    public int Count => Questions.Count;
}

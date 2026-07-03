using System;
using System.Collections.Generic;

namespace NextLearn.Desktop.Models;

public class McqDocument
{
    public string? Title { get; set; }

    public string? SourceDeck { get; set; }

    public DateTime GeneratedDate { get; set; } = DateTime.Now;

    public List<McqQuestion> Questions { get; set; } = [];
}

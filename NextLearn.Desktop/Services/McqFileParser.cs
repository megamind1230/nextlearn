using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NextLearn.Desktop.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NextLearn.Desktop.Services;

public static partial class McqFileParser
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static McqDocument Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var doc = new McqDocument();
        var blocks = SplitOnSeparator(content);

        if (blocks.Count == 0)
        {
            return doc;
        }

        var firstBlock = blocks[0].Trim();
        if (!string.IsNullOrEmpty(firstBlock))
        {
            try
            {
                var frontmatter = YamlDeserializer.Deserialize<McqFrontmatter>(firstBlock);
                if (frontmatter != null
                    && (!string.IsNullOrEmpty(frontmatter.Title)
                        || !string.IsNullOrEmpty(frontmatter.Source)
                        || frontmatter.Generated.HasValue))
                {
                    doc.Title = frontmatter.Title;
                    doc.SourceDeck = frontmatter.Source;
                    if (frontmatter.Generated.HasValue)
                    {
                        doc.GeneratedDate = frontmatter.Generated.Value;
                    }

                    blocks.RemoveAt(0);
                }
            }
            catch (YamlException)
            {
            }
        }

        foreach (var block in blocks)
        {
            var question = ParseQuestionBlock(block.Trim());
            if (question != null)
            {
                doc.Questions.Add(question);
            }
        }

        return doc;
    }

    public static string Serialize(McqDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("---");
        if (!string.IsNullOrEmpty(doc.Title))
        {
            sb.AppendLine($"title: {doc.Title}");
        }

        if (!string.IsNullOrEmpty(doc.SourceDeck))
        {
            sb.AppendLine($"source: {doc.SourceDeck}");
        }

        sb.AppendLine($"generated: {doc.GeneratedDate:yyyy-MM-dd HH:mm}");
        sb.AppendLine("---");
        sb.AppendLine();

        for (var i = 0; i < doc.Questions.Count; i++)
        {
            var q = doc.Questions[i];
            if (i > 0)
            {
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }

            sb.AppendLine($"## Question {i + 1}");
            sb.AppendLine(q.Question);
            sb.AppendLine();

            var letters = new[] { 'A', 'B', 'C', 'D' };
            for (var j = 0; j < q.Options.Count && j < 4; j++)
            {
                sb.AppendLine($"{letters[j]}. {q.Options[j]}");
            }

            sb.AppendLine();
            sb.AppendLine($"**Answer:** {letters[q.CorrectIndex]}");
            if (!string.IsNullOrEmpty(q.Explanation))
            {
                sb.AppendLine($"**Explanation:** {q.Explanation}");
            }
        }

        return sb.ToString();
    }

    private static McqQuestion? ParseQuestionBlock(string block)
    {
        var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 3)
        {
            return null;
        }

        var question = new McqQuestion();
        var questionLines = new List<string>();
        var options = new List<(string letter, string text)>();
        var currentOptionLines = new List<string>();
        var currentOptionLetter = string.Empty;
        var inOption = false;
        var answerLetter = string.Empty;
        var explanationLines = new List<string>();
        var inExplanation = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("**Answer:**", StringComparison.OrdinalIgnoreCase))
            {
                FinalizeOption(options, currentOptionLines, ref currentOptionLetter, ref inOption);
                answerLetter = line["**Answer:**".Length..].Trim().ToUpperInvariant();
                inExplanation = false;
                continue;
            }

            if (line.StartsWith("**Explanation:**", StringComparison.OrdinalIgnoreCase))
            {
                FinalizeOption(options, currentOptionLines, ref currentOptionLetter, ref inOption);
                var explanationText = line["**Explanation:**".Length..].Trim();
                if (!string.IsNullOrEmpty(explanationText))
                {
                    explanationLines.Add(explanationText);
                }

                inExplanation = true;
                continue;
            }

            if (inExplanation)
            {
                explanationLines.Add(line);
                continue;
            }

            var optionMatch = OptionRegex().Match(line);
            if (optionMatch.Success)
            {
                FinalizeOption(options, currentOptionLines, ref currentOptionLetter, ref inOption);
                currentOptionLetter = optionMatch.Groups[1].Value.ToUpperInvariant();
                currentOptionLines.Add(optionMatch.Groups[2].Value.Trim());
                inOption = true;
                continue;
            }

            if (inOption)
            {
                currentOptionLines.Add(line);
            }
            else
            {
                questionLines.Add(line);
            }
        }

        FinalizeOption(options, currentOptionLines, ref currentOptionLetter, ref inOption);

        if (options.Count < 2 || string.IsNullOrEmpty(answerLetter))
        {
            return null;
        }

        var letterToIndex = new Dictionary<string, int>
        {
            { "A", 0 }, { "B", 1 }, { "C", 2 }, { "D", 3 },
        };

        if (!letterToIndex.TryGetValue(answerLetter, out var correctIndex) || correctIndex >= options.Count)
        {
            return null;
        }

        question.Question = string.Join("\n", questionLines).Trim();
        question.Options = options.Select(o => o.text).ToList();
        question.CorrectIndex = correctIndex;
        question.Explanation = explanationLines.Count > 0 ? string.Join("\n", explanationLines).Trim() : null;

        return question;
    }

    private static void FinalizeOption(
        List<(string letter, string text)> options,
        List<string> currentOptionLines,
        ref string currentOptionLetter,
        ref bool inOption)
    {
        if (inOption && currentOptionLines.Count > 0)
        {
            options.Add((currentOptionLetter, string.Join("\n", currentOptionLines)));
            currentOptionLines.Clear();
            currentOptionLetter = string.Empty;
            inOption = false;
        }
    }

    private static List<string> SplitOnSeparator(string content)
    {
        var blocks = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var line in content.Split('\n'))
        {
            if (SeparatorRegex().IsMatch(line))
            {
                if (current.Length > 0)
                {
                    blocks.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.AppendLine(line);
            }
        }

        if (current.Length > 0)
        {
            blocks.Add(current.ToString());
        }

        return blocks;
    }

    [GeneratedRegex(@"^([A-D])\.\s+(.+)$")]
    private static partial Regex OptionRegex();

    [GeneratedRegex(@"^---$")]
    public static partial Regex SeparatorRegex();

    private class McqFrontmatter
    {
        public string? Title { get; set; }

        public string? Source { get; set; }

        public DateTime? Generated { get; set; }
    }
}

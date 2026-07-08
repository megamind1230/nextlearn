using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NextLearn.Desktop.Models;

namespace NextLearn.Desktop.Services;

public static class DeckFilter
{
    public static List<string> Tokenize(string query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tokens = new List<string>();
        var inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (var i = 0; i < query.Length; i++)
        {
            var c = query[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    public static bool TokenMatch(Deck deck, string token, bool useRegex)
    {
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(token);

        if (token.StartsWith('#'))
        {
            var tag = token[1..].ToLowerInvariant();
            var deckTags = GetTagSet(deck.Tags);

            if (useRegex)
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

        if (token.StartsWith("tags:", StringComparison.OrdinalIgnoreCase))
        {
            return MatchTags(deck.Tags, token[5..], useRegex);
        }

        if (token.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return MatchField(deck.FileName, token[5..], useRegex);
        }

        if (token.StartsWith("title:", StringComparison.OrdinalIgnoreCase))
        {
            return MatchField(deck.Title, token[6..], useRegex);
        }

        if (token.StartsWith("description:", StringComparison.OrdinalIgnoreCase))
        {
            return MatchField(deck.Description, token[12..], useRegex);
        }

        if (token.StartsWith("desc:", StringComparison.OrdinalIgnoreCase))
        {
            return MatchField(deck.Description, token[5..], useRegex);
        }

        return MatchField(deck.Title, token, useRegex)
            || MatchField(deck.Description, token, useRegex)
            || MatchField(deck.FileName, token, useRegex);
    }

    private static bool MatchField(string field, string term, bool useRegex)
    {
        if (string.IsNullOrEmpty(term))
        {
            return false;
        }

        if (useRegex)
        {
            try
            {
                return Regex.IsMatch(field, term, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        return field.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> GetTagSet(string? tags)
    {
        return (tags ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .ToHashSet();
    }

    private static bool MatchTags(string? deckTagsStr, string raw, bool useRegex)
    {
        var wanted = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (wanted.Length == 0)
        {
            return false;
        }

        var deckTags = GetTagSet(deckTagsStr);

        foreach (var w in wanted)
        {
            var tag = w.ToLowerInvariant();
            bool matched;

            if (useRegex)
            {
                try
                {
                    matched = deckTags.Any(dt => Regex.IsMatch(dt, tag, RegexOptions.IgnoreCase));
                }
                catch (ArgumentException)
                {
                    matched = false;
                }
            }
            else
            {
                matched = deckTags.Any(dt => dt.StartsWith(tag));
            }

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }
}

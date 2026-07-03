using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NextLearn.Desktop.Models;

namespace NextLearn.Desktop.Services;

public partial class McqGenerationService : IMcqGenerationService
{
    private readonly HttpClient _http;
    private string? _cachedModelName;

    private static readonly string[] KnownModels =
    [
        "gemini-2.5-flash",
        "gemini-2.0-flash",
        "gemini-flash-latest",
        "gemini-1.5-flash",
    ];

    private static readonly int[] RetryDelays = [1, 5, 10, 20, 40, 80];

    public McqGenerationService(HttpClient http)
    {
        _http = http;
    }

    public async Task<McqGenerationResult> GenerateMcqAsync(string deckContent, string apiKey, int questionCount = 5)
    {
        ArgumentNullException.ThrowIfNull(deckContent);

        const long maxSafeTokens = 700_000;
        var estimatedTokens = deckContent.Length / 4L;

        if (estimatedTokens > 900_000)
        {
            return new McqGenerationResult
            {
                Success = false,
                Error = $"Deck too large (estimated ~{estimatedTokens} tokens, max 1M). Split into smaller files.",
            };
        }

        string content;
        if (estimatedTokens > maxSafeTokens)
        {
            var truncChars = (int)(maxSafeTokens * 4);
            content = deckContent[..truncChars];
        }
        else
        {
            content = deckContent;
        }

        var prompt = BuildPrompt(content, questionCount);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt },
                    },
                },
            },
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var modelQueue = new List<string>();
        if (_cachedModelName != null)
        {
            modelQueue.Add(_cachedModelName);
        }

        modelQueue.AddRange(KnownModels);

        string? lastErrBody = null;
        bool allWere404 = true;

        foreach (var model in modelQueue)
        {
            var result = await TryModelWithRetriesAsync(model, json, apiKey).ConfigureAwait(false);
            lastErrBody = result.LastErrBody;

            if (result.FinalResult != null)
            {
                return result.FinalResult;
            }

            if (!result.Was404)
            {
                allWere404 = false;
            }
        }

        if (allWere404 && _cachedModelName == null)
        {
            var discovered = await DiscoverModelAsync(apiKey).ConfigureAwait(false);
            if (discovered != null)
            {
                _cachedModelName = discovered;
                var result = await TryModelWithRetriesAsync(discovered, json, apiKey).ConfigureAwait(false);
                lastErrBody = result.LastErrBody;

                if (result.FinalResult != null)
                {
                    return result.FinalResult;
                }
            }
        }

        return new McqGenerationResult
        {
            Success = false,
            Error = lastErrBody != null
                ? $"Gemini API error: {lastErrBody}"
                : "All available models failed. Try again later.",
        };
    }

    internal static string BuildPrompt(string content, int questionCount)
    {
        return $@"You are an educator creating multiple-choice questions from study material.
Given the deck content below, create {questionCount} questions.

Format each question EXACTLY like this:

## Question 1
{{question text}}

A. {{option A}}
B. {{option B}}
C. {{option C}}
D. {{option D}}

**Answer:** A
**Explanation:** {{why this is correct}}

---

## Question 2
...

Rules:
- Each question must have exactly 4 options (A, B, C, D)
- Make sure exactly one answer is correct
- Distractors should be plausible but clearly wrong
- Use --- on its own line as a separator between questions
- Do NOT use any markdown code fences
- Cover EVERY section of the content — do not skip any part
- Generate exactly {questionCount} questions

Deck content:
---
{content}
---";
    }

    internal static McqGenerationResult ParseResponse(string text)
    {
        text = text.Trim();

        var codeBlockMatch = CodeBlockRegex().Match(text);
        if (codeBlockMatch.Success)
        {
            text = codeBlockMatch.Groups[1].Value.Trim();
        }

        var blocks = text.Split("\n---\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (blocks.Length == 0)
        {
            blocks = text.Split("\n---\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        var questions = new List<McqQuestion>();

        foreach (var block in blocks)
        {
            var question = ParseSingleQuestion(block.Trim());
            if (question != null)
            {
                questions.Add(question);
            }
        }

        if (questions.Count == 0)
        {
            return new McqGenerationResult
            {
                Success = false,
                Error = $"Gemini did not return valid MCQ questions. Raw response:\n{text}",
            };
        }

        return new McqGenerationResult
        {
            Success = true,
            Questions = questions,
        };
    }

    private static McqQuestion? ParseSingleQuestion(string block)
    {
        var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 3)
        {
            return null;
        }

        var question = new McqQuestion();
        var questionLines = new List<string>();
        var options = new Dictionary<string, string>();
        var answerLetter = string.Empty;
        var explanationParts = new List<string>();
        var inExplanation = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("## "))
            {
                continue;
            }

            if (line.StartsWith("**Answer:**", StringComparison.OrdinalIgnoreCase))
            {
                answerLetter = line["**Answer:**".Length..].Trim().ToUpperInvariant();
                inExplanation = false;
                continue;
            }

            if (line.StartsWith("**Explanation:**", StringComparison.OrdinalIgnoreCase))
            {
                var explanationText = line["**Explanation:**".Length..].Trim();
                if (!string.IsNullOrEmpty(explanationText))
                {
                    explanationParts.Add(explanationText);
                }

                inExplanation = true;
                continue;
            }

            if (inExplanation)
            {
                explanationParts.Add(line);
                continue;
            }

            var optionMatch = OptionRegex().Match(line);
            if (optionMatch.Success)
            {
                var letter = optionMatch.Groups[1].Value.ToUpperInvariant();
                var text = optionMatch.Groups[2].Value.Trim();
                options[letter] = text;
                continue;
            }

            questionLines.Add(line);
        }

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
        question.Options = options.OrderBy(kvp => letterToIndex.GetValueOrDefault(kvp.Key, 99))
            .Select(kvp => kvp.Value)
            .ToList();
        question.CorrectIndex = correctIndex;
        question.Explanation = explanationParts.Count > 0 ? string.Join("\n", explanationParts).Trim() : null;

        return question;
    }

    [GeneratedRegex(@"^([A-D])\.\s+(.+)$")]
    private static partial Regex OptionRegex();

    [GeneratedRegex(@"```(?:markdown)?\s*\n?(.+?)\n?```", RegexOptions.Singleline)]
    private static partial Regex CodeBlockRegex();

    private async Task<ModelResult> TryModelWithRetriesAsync(string model, string json, string apiKey)
    {
        string? lastErrBody = null;

        for (int attempt = 0; attempt < RetryDelays.Length; attempt++)
        {
            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}",
                    httpContent).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var parsed = ParseGeminiResponse(responseBody);
                    return new ModelResult(parsed, lastErrBody, Was404: false);
                }

                var errBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                lastErrBody = errBody;

                if (response.StatusCode == (System.Net.HttpStatusCode)429)
                {
                    if (attempt < RetryDelays.Length - 1)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(RetryDelays[attempt])).ConfigureAwait(false);
                        continue;
                    }

                    return new ModelResult(null, lastErrBody, Was404: false);
                }

                if (response.StatusCode == (System.Net.HttpStatusCode)404)
                {
                    return new ModelResult(null, lastErrBody, Was404: true);
                }

                return new ModelResult(
                    new McqGenerationResult
                    {
                        Success = false,
                        Error = response.StatusCode switch
                        {
                            System.Net.HttpStatusCode.Unauthorized => $"Invalid API key. Response: {errBody}",
                            _ => $"Gemini API error ({(int)response.StatusCode}): {errBody}",
                        },
                    },
                    lastErrBody,
                    Was404: false);
            }
            catch (HttpRequestException)
            {
                return new ModelResult(
                    new McqGenerationResult
                    {
                        Success = false,
                        Error = "No internet connection. Check and try again.",
                    },
                    null,
                    Was404: false);
            }
            catch (TaskCanceledException)
            {
                return new ModelResult(
                    new McqGenerationResult
                    {
                        Success = false,
                        Error = "Request timed out. Check your internet connection and try again.",
                    },
                    null,
                    Was404: false);
            }
            catch (JsonException)
            {
                return new ModelResult(
                    new McqGenerationResult
                    {
                        Success = false,
                        Error = "Failed to parse Gemini response. Try again.",
                    },
                    null,
                    Was404: false);
            }
        }

        return new ModelResult(null, lastErrBody, Was404: false);
    }

    private static McqGenerationResult? ParseGeminiResponse(string responseBody)
    {
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var text = geminiResponse?.Candidates?.FirstOrDefault()
            ?.Content?.Parts?.FirstOrDefault()
            ?.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            return new McqGenerationResult
            {
                Success = false,
                Error = "Gemini returned an empty response. Try again.",
            };
        }

        return ParseResponse(text);
    }

    private async Task<string?> DiscoverModelAsync(string apiKey)
    {
        try
        {
            var response = await _http.GetAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}")
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var list = JsonSerializer.Deserialize<ModelsListResponse>(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

            var model = list?.Models?
                .Where(m => m.SupportedGenerationMethods?.Contains("generateContent") == true)
                .Where(m => m.Name?.Contains("flash", StringComparison.OrdinalIgnoreCase) == true)
                .OrderByDescending(m => m.Version ?? string.Empty)
                .FirstOrDefault();

            model ??= list?.Models?
                .FirstOrDefault(m => m.SupportedGenerationMethods?.Contains("generateContent") == true);

            if (model?.Name == null)
            {
                return null;
            }

            return model.Name.StartsWith("models/") ? model.Name[7..] : model.Name;
        }
#pragma warning disable CA1031
        catch (Exception)
        {
            return null;
        }
#pragma warning restore CA1031
    }

    private record ModelResult(McqGenerationResult? FinalResult, string? LastErrBody, bool Was404);

    private class ModelsListResponse
    {
        [JsonPropertyName("models")]
        public List<ModelInfo>? Models { get; set; }
    }

    private class ModelInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("supportedGenerationMethods")]
        public List<string>? SupportedGenerationMethods { get; set; }
    }

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}

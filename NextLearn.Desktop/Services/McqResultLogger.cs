using System;
using System.IO;
using System.Text;

namespace NextLearn.Desktop.Services;

public static class McqResultLogger
{
    public static void LogResult(
        string logPath,
        string timestamp,
        string score,
        string title,
        string sourceDeck,
        int questionCount,
        string timeTaken)
    {
        ArgumentNullException.ThrowIfNull(logPath);
        ArgumentNullException.ThrowIfNull(timestamp);
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(sourceDeck);
        ArgumentNullException.ThrowIfNull(timeTaken);

        var dir = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var header = "#+TITLE: MCQ Quiz Results\n\n"
            + "| Timestamp | Score | Title | Source Deck | Questions | Time Taken |\n"
            + "|-----------+-------+-------+-------------+-----------+------------|\n";

        var row = $"| {timestamp} | {score} | {EscapeOrgPipe(title)} | {EscapeOrgPipe(sourceDeck)} | {questionCount} | {timeTaken} |\n";

        if (!File.Exists(logPath))
        {
            File.WriteAllText(logPath, header + row, Encoding.UTF8);
            return;
        }

        var content = File.ReadAllText(logPath, Encoding.UTF8);

        if (!content.Contains("#+TITLE: MCQ Quiz Results", StringComparison.Ordinal))
        {
            content = header + content;
        }

        if (!content.EndsWith("\n", StringComparison.Ordinal))
        {
            content += "\n";
        }

        content += row;
        File.WriteAllText(logPath, content, Encoding.UTF8);
    }

    public static string ReadLog(string logPath)
    {
        if (!File.Exists(logPath))
        {
            return string.Empty;
        }

        return File.ReadAllText(logPath, Encoding.UTF8);
    }

    private static string EscapeOrgPipe(string value)
    {
        return value.Replace("|", "\\|");
    }
}

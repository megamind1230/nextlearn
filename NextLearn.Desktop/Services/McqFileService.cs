using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NextLearn.Desktop.Models;

#pragma warning disable CA1031
#pragma warning disable CA1822

namespace NextLearn.Desktop.Services;

public class McqFileService
{
    public List<McqFileInfo> ListMcqFiles(string mcqsPath)
    {
        var files = new List<McqFileInfo>();

        if (!Directory.Exists(mcqsPath))
        {
            return files;
        }

        foreach (var filePath in Directory.GetFiles(mcqsPath, "*.mcq", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var doc = McqFileParser.Parse(content);
                files.Add(new McqFileInfo
                {
                    FilePath = filePath,
                    Title = doc.Title ?? McqFileInfoDisplayName(filePath),
                    QuestionCount = doc.Questions.Count,
                    GeneratedDate = doc.GeneratedDate,
                });
            }
            catch (Exception)
            {
                files.Add(new McqFileInfo
                {
                    FilePath = filePath,
                    Title = McqFileInfoDisplayName(filePath),
                    QuestionCount = 0,
                    GeneratedDate = File.GetLastWriteTime(filePath),
                });
            }
        }

        return files.OrderByDescending(f => f.GeneratedDate).ToList();
    }

    public McqDocument? LoadMcq(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var content = File.ReadAllText(filePath);
            return McqFileParser.Parse(content);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void SaveMcq(string filePath, McqDocument doc)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var content = McqFileParser.Serialize(doc);
        File.WriteAllText(filePath, content);
    }

    public void DeleteMcq(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static string McqFileInfoDisplayName(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        if (name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^3];
        }

        return name;
    }
}

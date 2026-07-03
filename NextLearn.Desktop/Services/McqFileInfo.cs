using System;
using System.IO;

namespace NextLearn.Desktop.Services;

public class McqFileInfo
{
    public string FilePath { get; set; } = string.Empty;

    public string FileName => Path.GetFileName(FilePath);

    public string DisplayName
    {
        get
        {
            var name = Path.GetFileNameWithoutExtension(FilePath);
            if (name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                name = name[..^3];
            }

            return name;
        }
    }

    public string Title { get; set; } = string.Empty;

    public int QuestionCount { get; set; }

    public DateTime GeneratedDate { get; set; }

    public string GeneratedDateDisplay => GeneratedDate.ToString("yyyy-MM-dd HH:mm");
}

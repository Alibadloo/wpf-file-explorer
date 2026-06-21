using System.IO;
using System.Windows.Media;

namespace FileExplorer.Models;

public class FileSystemItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public DateTime DateModified { get; set; }
    public DateTime DateCreated { get; set; }
    public bool IsDirectory { get; set; }
    public bool IsHidden { get; set; }
    public bool IsReadOnly { get; set; }
    public ImageSource? Icon { get; set; }

    public static FileSystemItem FromFileInfo(FileInfo fileInfo)
    {
        return new FileSystemItem
        {
            Name = fileInfo.Name,
            FullPath = fileInfo.FullName,
            Extension = fileInfo.Extension.ToLowerInvariant(),
            Type = GetFileType(fileInfo.Extension),
            Size = fileInfo.Length,
            SizeFormatted = FormatSize(fileInfo.Length),
            DateModified = fileInfo.LastWriteTime,
            DateCreated = fileInfo.CreationTime,
            IsDirectory = false,
            IsHidden = fileInfo.Attributes.HasFlag(FileAttributes.Hidden),
            IsReadOnly = fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly),
        };
    }

    public static FileSystemItem FromDirectoryInfo(DirectoryInfo dirInfo)
    {
        return new FileSystemItem
        {
            Name = dirInfo.Name,
            FullPath = dirInfo.FullName,
            Extension = string.Empty,
            Type = "Folder",
            Size = 0,
            SizeFormatted = string.Empty,
            DateModified = dirInfo.LastWriteTime,
            DateCreated = dirInfo.CreationTime,
            IsDirectory = true,
            IsHidden = dirInfo.Attributes.HasFlag(FileAttributes.Hidden),
            IsReadOnly = dirInfo.Attributes.HasFlag(FileAttributes.ReadOnly),
        };
    }

    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }

    private static string GetFileType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".txt" => "Text Document",
            ".pdf" => "PDF Document",
            ".doc" or ".docx" => "Word Document",
            ".xls" or ".xlsx" => "Excel Spreadsheet",
            ".ppt" or ".pptx" => "PowerPoint Presentation",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => "Image",
            ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" => "Audio File",
            ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" => "Video File",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "Compressed Archive",
            ".exe" => "Application",
            ".dll" => "Dynamic Link Library",
            ".cs" => "C# Source File",
            ".cpp" or ".cc" => "C++ Source File",
            ".py" => "Python Script",
            ".js" => "JavaScript File",
            ".ts" => "TypeScript File",
            ".html" or ".htm" => "HTML File",
            ".css" => "CSS Stylesheet",
            ".json" => "JSON File",
            ".xml" => "XML File",
            ".yaml" or ".yml" => "YAML File",
            ".md" => "Markdown File",
            ".sln" => "Visual Studio Solution",
            ".csproj" => "C# Project File",
            _ => string.IsNullOrEmpty(extension) ? "File" : $"{extension.TrimStart('.').ToUpper()} File"
        };
    }
}

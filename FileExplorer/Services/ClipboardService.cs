namespace FileExplorer.Services;

public enum ClipboardOperation { Copy, Cut }

public class ClipboardService
{
    public string? SourcePath { get; private set; }
    public bool IsDirectory { get; private set; }
    public ClipboardOperation Operation { get; private set; }
    public bool HasItem => SourcePath is not null;

    public void Copy(string path, bool isDirectory)
    {
        SourcePath = path;
        IsDirectory = isDirectory;
        Operation = ClipboardOperation.Copy;
    }

    public void Cut(string path, bool isDirectory)
    {
        SourcePath = path;
        IsDirectory = isDirectory;
        Operation = ClipboardOperation.Cut;
    }

    public void Clear()
    {
        SourcePath = null;
    }
}

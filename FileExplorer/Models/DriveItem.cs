namespace FileExplorer.Models;

public class DriveItem
{
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string DriveType { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public long FreeSpace { get; set; }
    public string DisplayName => string.IsNullOrEmpty(Label)
        ? $"Local Disk ({Name})"
        : $"{Label} ({Name})";
    public double UsedPercent => TotalSize > 0 ? (double)(TotalSize - FreeSpace) / TotalSize * 100 : 0;
}

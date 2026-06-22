using System;
using System.IO;

namespace IDESK.Core.Logging;
public class FileLogger : ILogger
{
    private readonly string _path;

    public FileLogger()
    {
        _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IDESK", "Logs", "log.txt"
        );
        if (!File.Exists(_path)) Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? throw new InvalidOperationException());
    }

    public void Log(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        File.AppendAllText(_path, line + Environment.NewLine);
    }
}
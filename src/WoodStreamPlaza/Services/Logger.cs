using System;
using System.IO;

namespace WoodStreamPlaza.Services;

public static class Logger
{
    private static readonly string LogPath;

    static Logger()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WoodStreamPlaza");
        Directory.CreateDirectory(dir);
        LogPath = Path.Combine(dir, "app.log");
    }

    public static void Info(string message)
    {
        try
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            File.AppendAllText(LogPath, line + Environment.NewLine);
            Console.WriteLine(line);
        }
        catch { }
    }
}

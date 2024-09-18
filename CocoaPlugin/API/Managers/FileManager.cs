using System.IO;
using Exiled.API.Features;

namespace CocoaPlugin.API.Managers;

public static class FileManager
{
    public static string FolderPath => Path.Combine(Paths.Configs, "CocoaPlugin");

    public static void CreateFolder()
    {
        if (!Directory.Exists(FolderPath))
            Directory.CreateDirectory(FolderPath);
    }

    public static void AppendFile(string fileName, string content)
    {
        File.AppendAllText(Path.Combine(FolderPath, fileName), content);
    }

    public static void WriteFile(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(FolderPath, fileName), content);
    }

    public static string ReadFile(string fileName)
    {
        if (!File.Exists(Path.Combine(FolderPath, fileName)))
        {
            File.WriteAllText(Path.Combine(FolderPath, fileName), "");
        }

        return File.ReadAllText(Path.Combine(FolderPath, fileName));
    }

    public static string GetPath(string path)
    {
        return Path.Combine(FolderPath, path);
    }
}

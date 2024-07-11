using System.IO;
using Exiled.API.Features;

namespace CocoaPlugin.API;

public static class FileManager
{
    public static string FolderPath => Path.Combine(Paths.Configs, "CocoaPlugin");

    public static void CreateFolder()
    {
        if (!Directory.Exists(FolderPath))
            Directory.CreateDirectory(FolderPath);
    }

    public static void WriteFile(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(FolderPath, fileName), content);
    }

    public static string ReadFile(string fileName)
    {
        return !File.Exists(Path.Combine(FolderPath, fileName)) ? null : File.ReadAllText(Path.Combine(FolderPath, fileName));
    }
}
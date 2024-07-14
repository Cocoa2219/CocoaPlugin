using System.IO;
using Exiled.API.Features;
using Utf8Json;

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
        if (!File.Exists(Path.Combine(FolderPath, fileName)))
        {
            File.WriteAllText(Path.Combine(FolderPath, fileName), "");
        }

        return File.ReadAllText(Path.Combine(FolderPath, fileName));
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Exiled.API.Features;

public class VideoEncoder
{
    public VideoEncoder(Player player)
    {
        var imagePath = CocoaPlugin.API.Managers.FileManager.GetPath("Images");

        if (!Directory.Exists(imagePath))
            Directory.CreateDirectory(imagePath);

        try
        {
            ImageDirectory = Path.Combine(imagePath, player.UserId);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return;
        }

        if (!Directory.Exists(ImageDirectory))
            Directory.CreateDirectory(ImageDirectory);

        if (!Directory.Exists(CocoaPlugin.API.Managers.FileManager.GetPath("Videos")))
            Directory.CreateDirectory(CocoaPlugin.API.Managers.FileManager.GetPath("Videos"));

        VideoOutputPath = Path.Combine(CocoaPlugin.API.Managers.FileManager.GetPath("Videos"), $"{player.UserId}.mp4");
    }

    public string ImageDirectory { get; set; }
    public string VideoOutputPath { get; set; }
    public string FFmpegPath => Path.Combine(Paths.Configs, "ffmpeg.exe");

    public const string ImageNamePattern = "image_%d.png";

    public string FFmpegArguments { get; set; }

    public Task Encode(Action onComplete, float framerate)
    {
        FFmpegArguments = $"-y -framerate {framerate} -i {Path.Combine(ImageDirectory, ImageNamePattern)} -c:v libx264 -preset veryfast -threads 4 -r {framerate} -pix_fmt yuv420p {VideoOutputPath}";

        var processStartInfo = new ProcessStartInfo
        {
            FileName = FFmpegPath,
            Arguments = FFmpegArguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = processStartInfo;
        // process.ErrorDataReceived += (sender, e) => { Console.WriteLine(e.Data); };
        // process.OutputDataReceived += (sender, e) => { Console.

        process.Start();
        Log.Info($"Encoding video for {VideoOutputPath} with arguments {FFmpegArguments}");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();

        onComplete?.Invoke();

        foreach (var file in Directory.GetFiles(ImageDirectory))
        {
            File.Delete(file);
        }

        return Task.CompletedTask;
    }
}

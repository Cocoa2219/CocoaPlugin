using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AdminToys;
using CommandSystem;
using Exiled.API.Features;
using Exiled.API.Features.Toys;
using MEC;
using UnityEngine;

namespace CocoaPlugin.API.Beta;

public static class VideoLoader
{
    public static string DefaultPath { get; } = System.IO.Path.Combine(Paths.Configs, "CocoaPlugin", "VideoLoader");
    public static string FFMpegPath { get; } = System.IO.Path.Combine(DefaultPath, "ffmpeg.exe");
    public static readonly Regex ResolutionRegex = new Regex(@"(\d+)x(\d+)");
    public static readonly Regex FrameRateRegex = new Regex(@"(\d+\.?\d*) fps");

    private static bool _working;

    public static IEnumerator<float> LoadVideo(string path, float scale, int width, int height, Player player, float delay = 0.01f)
    {
        if (_working)
        {
            player.ShowHint("Another video is being loaded. Please wait.", 5f);
            yield break;
        }

        var position = player.Position;

        _working = true;

        Video video = null;

        var width1 = width;
        var height1 = height;
        Task.Run(async () =>
        {
            video = await Load(path, player, width1, height1);
        });

        Timing.RunCoroutine(PlayVideo(video, position, scale, width, height, delay));
    }

    private static List<Primitive> Primitives { get; } = new List<Primitive>();

    private static IEnumerator<float> PlayVideo(Video video, Vector3 position, float scale, int width, int height, float delay)
    {
        foreach (var primitive in Primitives)
            primitive.Destroy();

        Primitives.Clear();

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (var x = 0; x < texture.width; x++)
        {
            for (var y = 0; y < texture.height; y++)
            {
                var primitive = Primitive.Create(PrimitiveType.Quad, position + new Vector3(x * scale, y * scale, 0),
                    Vector3.zero, Vector3.one * scale, true, Color.white);

                primitive.Flags &= ~PrimitiveFlags.Collidable;

                Primitives.Add(primitive);

                yield return Timing.WaitForSeconds(delay);
            }
        }

        foreach (var frame in video.Frames)
        {
            texture.LoadRawTextureData(frame);

            for (var x = 0; x < texture.width; x++)
            {
                for (var y = 0; y < texture.height; y++)
                {
                    var color = texture.GetPixel(x, y);

                    Primitives[y * texture.width + x].Color = color;
                }
            }

            yield return Timing.WaitForSeconds(1f / video.FrameRate);
        }

        _working = false;
    }

    private static async Task<Video> Load(string path, Player player, int targetWidth, int targetHeight)
    {
        if (!File.Exists(path))
        {
            path = System.IO.Path.Combine(DefaultPath, path);
            if (!File.Exists(path))
            {
                Log.Error($"VideoLoader: File not found: {path}");
                player.ShowHint("Failed to fetch video metadata.", 5f);
                return null;
            }
        }

        player.ShowHint("Fetching video \"" + path + "\"'s metadata...", 120f);
        Log.Info($"Started loading video metadata: {path}");

        using var metadataProcess = new Process();
        metadataProcess.StartInfo = new ProcessStartInfo
        {
            FileName = FFMpegPath,
            Arguments = $"-i \"{path}\" -f null -",
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        metadataProcess.Start();

        var metadata = await metadataProcess.StandardError.ReadToEndAsync();

        Log.Info($"Video metadata: {metadata}");

        await WaitForExitAsync(metadataProcess);

        if (metadataProcess.ExitCode != 0 || metadata.Contains("No such file or directory"))
        {
            Log.Error($"VideoLoader: File not found: {path}");
            player.ShowHint("Failed to fetch video metadata.", 5f);
            return null;
        }

        var resolutionMatch = ResolutionRegex.Matches(metadata)[2];

        if (!resolutionMatch.Success)
        {
            Log.Error($"VideoLoader: Failed to fetch video resolution: {path}");
            player.ShowHint("Failed to fetch video metadata.", 5f);
            return null;
        }

        var frameRateMatch = FrameRateRegex.Match(metadata);

        if (!frameRateMatch.Success)
        {
            Log.Error($"VideoLoader: Failed to fetch video frame rate: {path}");
            player.ShowHint("Failed to fetch video metadata.", 5f);
            return null;
        }

        var durationMatch = Regex.Match(metadata, @"Duration: (\d+):(\d+):(\d+\.\d+)");
        if (!durationMatch.Success)
        {
            Log.Error($"VideoLoader: Failed to fetch video duration: {path}");
            player.ShowHint("Failed to fetch video metadata.", 5f);
            return null;
        }

        var duration = TimeSpan.Parse($"{durationMatch.Groups[1].Value}:{durationMatch.Groups[2].Value}:{durationMatch.Groups[3].Value}");

        var width = int.Parse(resolutionMatch.Groups[1].Value);
        var height = int.Parse(resolutionMatch.Groups[2].Value);
        var frameRate = float.Parse(frameRateMatch.Groups[1].Value);

        var totalFrames = (int)(duration.TotalSeconds * frameRate);

        var targetRatio = (float)width / height;

        if (targetWidth / targetRatio > targetHeight)
            targetWidth = (int)(targetHeight * targetRatio);
        else
            targetHeight = (int)(targetWidth / targetRatio);

        player.ShowHint($"Video resolution: {width}x{height}, frame rate: {frameRate} fps\nLoading video...", 120f);
        Log.Info($"VideoLoader: Video resolution: {width}x{height}, frame rate: {frameRate} fps");

        using var videoProcess = new Process();
        videoProcess.StartInfo = new ProcessStartInfo
        {
            FileName = FFMpegPath,
            Arguments = $"-i \"{path}\" -vf scale={targetWidth}:{targetHeight} -f image2pipe -pix_fmt rgb24 -vcodec rawvideo -",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        videoProcess.Start();

        videoProcess.ErrorDataReceived += (sender, e) => Log.Error(e.Data);
        videoProcess.BeginErrorReadLine();

        videoProcess.Exited += (sender, e) => Log.Info("Video process exited - " + e);

        await using var videoStream = videoProcess.StandardOutput.BaseStream;
        using var binaryReader = new BinaryReader(videoStream);

        var video = new Video
        {
            Frames = new List<byte[]>(),
            FrameRate = frameRate,
            Width = width,
            Height = height
        };

        Log.Info($"VideoLoader: Loading video frames...");

        while (true)
        {
            var buffer = new byte[width * height * 3];
            var bytesRead = await binaryReader.BaseStream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead == 0)
            {
                break;
            }

            var frame = new byte[width * height * 4];

            for (var i = 0; i < width * height; i++)
            {
                frame[i * 4] = buffer[i * 3];
                frame[i * 4 + 1] = buffer[i * 3 + 1];
                frame[i * 4 + 2] = buffer[i * 3 + 2];
                frame[i * 4 + 3] = 255;
            }

            Log.Info($"VideoLoader: Loaded frame {video.Frames.Count + 1} of {totalFrames}");

            video.Frames.Add(frame);
        }

        await WaitForExitAsync(videoProcess);

        player.ShowHint("Video loaded. Total frames: " + video.Frames.Count, 5f);
        Log.Info($"VideoLoader: Video loaded. Total frames: {video.Frames.Count}");

        return video;
    }

    private class Video
    {
        public List<byte[]> Frames { get; set; }
        public float FrameRate { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
    {
        if (process.HasExited) return Task.CompletedTask;

        var tcs = new TaskCompletionSource<object>();
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => tcs.TrySetResult(null);
        if(cancellationToken != default)
            cancellationToken.Register(() => tcs.SetCanceled());

        return process.HasExited ? Task.CompletedTask : tcs.Task;
    }
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class SpawnVideo : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender as CommandSender);

        if (player == null)
        {
            response = "You must be a player to use this command.";
            return false;
        }

        if (arguments.Count < 1)
        {
            response = "Usage: spawnimage <path> [x] [y] [z] [scale] [width] [height] [preserveAspect]";
            return false;
        }

        var path = arguments.At(0);

        var scale = arguments.Count > 1 ? float.Parse(arguments.At(1)) : 1f;
        var width = arguments.Count > 2 ? int.Parse(arguments.At(2)) : 128;
        var height = arguments.Count > 3 ? int.Parse(arguments.At(3)) : 128;

        var delay = arguments.Count > 4 ? float.Parse(arguments.At(4)) : 0.01f;

        Timing.RunCoroutine(VideoLoader.LoadVideo(path, scale, width, height, player, delay));

        response = "The video is being loaded. Please wait.";
        return true;
    }

    public string Command { get; } = "spawnvideo";
    public string[] Aliases { get; } = { "sv" };
    public string Description { get; } = "Spawns an video in the world.";
}
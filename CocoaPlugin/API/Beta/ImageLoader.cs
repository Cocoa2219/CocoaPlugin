using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using AdminToys;
using CommandSystem;
using Exiled.API.Features;
using Exiled.API.Features.Toys;
using MEC;
using UnityEngine;

namespace CocoaPlugin.API.Beta;

public static class ImageLoader
{
    public static string DefaultPath { get; } = System.IO.Path.Combine(Paths.Configs, "CocoaPlugin", "ImageLoader");

    private static Texture2D Load(string path)
    {
        if (!File.Exists(path))
        {
            path = System.IO.Path.Combine(DefaultPath, path);
            if (!File.Exists(path))
            {
                Log.Error($"ImageLoader: File not found: {path}");
                return null;
            }
        }

        var image = File.ReadAllBytes(path);
        var texture = new Texture2D(2, 2);

        if (!texture.LoadImage(image))
        {
            Log.Error($"ImageLoader: Failed to load image: {path}");
            return null;
        }

        return texture;
    }

    public static bool LoadImage(string path, Vector3 position, float scale, int width, int height,
        bool preserveAspect = true)
    {
        var texture = Load(path);
        if (texture == null)
            return false;

        var ratio = (float)texture.width / texture.height;

        if (preserveAspect)
        {
            if (width / ratio > height)
                width = (int)(height * ratio);
            else
                height = (int)(width / ratio);
        }

        var newTexture = new Texture2D(width, height);

        SpawnCubes(texture, newTexture, position, scale);

        return true;
    }

    private static List<Primitive> Primitives { get; } = new List<Primitive>();

    private static void SpawnCubes(Texture2D texture, Texture2D newTexture, Vector3 position, float scale)
    {
        foreach (var primitive in Primitives)
            primitive.Destroy();

        Primitives.Clear();

        for (var x = 0; x < newTexture.width; x++)
        {
            for (var y = 0; y < newTexture.height; y++)
            {
                var color = texture.GetPixelBilinear((float)x / newTexture.width, (float)y / newTexture.height);

                var primitive = Primitive.Create(PrimitiveType.Quad, position + new Vector3(x * scale, y * scale, 0),
                    Vector3.zero, Vector3.one * scale, true, color);

                primitive.Flags &= ~PrimitiveFlags.Collidable;

                Primitives.Add(primitive);
            }
        }
    }
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class SpawnImage : ICommand
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

        var preserveAspect = arguments.Count > 4 && bool.Parse(arguments.At(4));
        _ = arguments.Count > 5 ? float.Parse(arguments.At(5)) : 0.01f;

        if (!ImageLoader.LoadImage(path, player.Position, scale, width, height, preserveAspect))
        {
            response = "Failed to load image.";
            return false;
        }

        response = "Image loaded successfully.";
        return true;
    }

    public string Command { get; } = "spawnimage";
    public string[] Aliases { get; } = { "si" };
    public string Description { get; } = "Spawns an image in the world.";
}
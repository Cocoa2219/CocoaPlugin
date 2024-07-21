using System.Collections.Generic;
using CocoaPlugin.API;

namespace CocoaPlugin.Configs;

public class Network
{
    public string PostBaseUrl { get; set; } = "http://localhost:8080/";

    public Dictionary<PostType, string> SubUrl { get; set; } = new()
    {
        {PostType.None, ""},
        {PostType.Log, "log"},
        {PostType.LinkDm, "linkdm"}
    };

    public string ListenUrl { get; set; } = "http://localhost:8081/";
}
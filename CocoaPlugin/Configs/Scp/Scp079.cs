using System.Collections.Generic;

namespace CocoaPlugin.Configs.Scp;

public class Scp079
{
    public List<float> AuxRegeneration { get; set; } = new() { 1.2f, 2.5f, 4.1f, 5.1f, 6.9f };
    public int BlackoutRoomCost { get; set; } = 40;
    public int SurfaceBlackoutCost { get; set; } = 80;
    public float BlackoutRoomDuration { get; set; } = 10f;
    public float BlackoutRoomCooldown { get; set; } = 17f;
    public List<int> BlackoutRoomCapacity { get; set; } = new() { 0, 1, 2, 3 };
    public float BlackoutZoneDuration { get; set; } = 60f;
    public float BlackoutZoneCooldown { get; set; } = 120f;
    public int BlackoutZoneCost { get; set; } = 200;
}
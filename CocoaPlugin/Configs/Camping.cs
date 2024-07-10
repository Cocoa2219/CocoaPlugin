namespace CocoaPlugin.Configs;

public class Camping
{
    public float CampingTime { get; set; } = 120f;

    public float CampingTimeToleranceMultiplier { get; set; } = 10f;

    public float CampingCheckInterval { get; set; } = 1f;

    public API.Broadcast CampingMessage { get; set; } = new("<cspace=0.05em><size=30><color=#d44b42>🐢 장시간 같은 구역에 있음</color>이 감지되었습니다.\n<size=20><color=#a5ed95>게임에 활발한 참여</color>를 부탁드리며, 지속될 경우 <color=#d44b42>제재 수 있음을 알려 드립니다.</color></size></size></cspace>", 5, 10);

    public float CampingMessageFrequency { get; set; } = 10f;
}
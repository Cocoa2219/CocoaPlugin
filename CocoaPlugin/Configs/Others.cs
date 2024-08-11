namespace CocoaPlugin.Configs;

public class Others
{
    public float DoorTrollingSphereRadius { get; set; } = 7f;
    public API.Broadcast DoorTrollingMessage { get; set; } = new("<cspace=0.05em><size=30px><color=#d44b42>🚪 <b>문트롤이 감지되었습니다.</color></b>\n<size=20px>지속적으로 문트롤이 감지될 경우, 제재가 가해질 수 있습니다.</size></size></cspace>", 10, 10);
}
namespace CocoaPlugin.Configs.Scp;

public class Scp049
{
    public bool OneHitKill { get; set; } = false;
    public float CardiacArrestDamage { get; set; } = 8f;
    public float CardiacArrestDuration { get; set; } = 20f;
    public float FirstAttackCooldown { get; set; } = 1.5f;
    public float SecondAttackCooldown { get; set; } = 1.5f;
    public float SenseKilledCooldown { get; set; } = 40f;
    public float SenseLostCooldown { get; set; } = 20f;
    public float SenseNoTargetCooldown { get; set; } = 2.5f;
    public float SenseDuration { get; set; } = 20f;
    public float CallCooldown { get; set; } = 60f;
    public float CallDuration { get; set; } = 20f;
}
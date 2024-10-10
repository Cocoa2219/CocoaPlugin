using System.ComponentModel;

namespace CocoaPlugin.Configs.Scp
{
    [Description("SCP-049의 설정을 정의하는 클래스입니다.")]
    public class Scp049
    {
        [Description("한 방에 죽이는 기능을 활성화할지 여부를 나타냅니다.")]
        public bool OneHitKill { get; set; } = false;
        [Description("심장마비 공격의 피해량을 나타냅니다.")]
        public float CardiacArrestDamage { get; set; } = 8f;
        [Description("심장마비 공격의 지속 시간을 나타냅니다.")]
        public float CardiacArrestDuration { get; set; } = 20f;
        [Description("첫 번째 공격의 쿨타임 시간을 나타냅니다.")]
        public float FirstAttackCooldown { get; set; } = 1.5f;
        [Description("두 번째 공격의 쿨타임 시간을 나타냅니다.")]
        public float SecondAttackCooldown { get; set; } = 1.5f;
        [Description("죽인 후 F의 쿨타임 시간을 나타냅니다.")]
        public float SenseKilledCooldown { get; set; } = 40f;
        [Description("대상을 잃은 후 F의 쿨타임 시간을 나타냅니다.")]
        public float SenseLostCooldown { get; set; } = 20f;
        [Description("대상 없는 F의 쿨타임 시간을 나타냅니다.")]
        public float SenseNoTargetCooldown { get; set; } = 2.5f;
        [Description("F 기능의 지속 시간을 나타냅니다.")]
        public float SenseDuration { get; set; } = 20f;
        [Description("밥 기능의 쿨타임 시간을 나타냅니다.")]
        public float CallCooldown { get; set; } = 60f;
        [Description("밥 기능의 지속 시간을 나타냅니다.")]
        public float CallDuration { get; set; } = 20f;
        [Description("좀비의 쉴드 재생 속도를 나타냅니다.")]
        public float ZombieHumeShieldRegeneration { get; set; } = 0f;
        [Description("좀비의 최대 쉴드 양을 나타냅니다.")]
        public float ZombieHumeShieldMax { get; set; } = 100f;
        [Description("좀비의 공격 피해량을 나타냅니다.")]
        public float ZombieDamage { get; set; } = 40f;
        [Description("좀비의 공격 쿨타임 시간을 나타냅니다.")]
        public float ZombieAttackCooldown { get; set; } = 1.3f;
        [Description("좀비가 E키를 눌러하여 치유할 수 있는 양을 나타냅니다.")]
        public float ZombieConsumeHealAmount { get; set; } = 100f;
        [Description("좀비의 쉴드 재생 최대 거리를 나타냅니다.")]
        public float ZombieHsRegenerationMaxDistanceSqr { get; set; } = 100f;
    }
}
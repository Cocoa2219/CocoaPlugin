using CocoaPlugin.API.Managers;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Server;
using MEC;
using Respawning;

namespace CocoaPlugin.API.Achievements.Survival;

public class InsurgencyCoin : Achievement
{
    public override AchievementType Type { get; set; } = AchievementType.InsurgencyCoin;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Survival];
    public override string Name { get; set; } = "반란 코인";
    public override string Description { get; set; } = "상대 진영의 지원 후 10초 내로 반대 진영으로 탈출하세요.";

    private bool _canAchieve;
    private SpawnableTeamType _lastTeam;

    public override void RegisterEvents()
    {
        Exiled.Events.Handlers.Server.RespawningTeam += OnRespawningTeam;
        Exiled.Events.Handlers.Player.Escaping += OnEscaping;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Server.RespawningTeam -= OnRespawningTeam;
        Exiled.Events.Handlers.Player.Escaping -= OnEscaping;
    }

    private void OnRespawningTeam(RespawningTeamEventArgs ev)
    {
        _lastTeam = ev.NextKnownTeam;
        _canAchieve = true;

        Timing.CallDelayed(10f, () =>
        {
            _canAchieve = false;
        });
    }

    private void OnEscaping(Exiled.Events.EventArgs.Player.EscapingEventArgs ev)
    {
        if (!_canAchieve) return;

        if (ev.EscapeScenario == EscapeScenario.ClassD && _lastTeam == SpawnableTeamType.NineTailedFox)
            Achieve(ev.Player.UserId);
        else if (ev.EscapeScenario == EscapeScenario.Scientist && _lastTeam == SpawnableTeamType.ChaosInsurgency)
            Achieve(ev.Player.UserId);
        else if (ev.EscapeScenario == EscapeScenario.CuffedClassD && _lastTeam == SpawnableTeamType.ChaosInsurgency)
            Achieve(ev.Player.UserId);
        else if (ev.EscapeScenario == EscapeScenario.CuffedScientist && _lastTeam == SpawnableTeamType.NineTailedFox)
            Achieve(ev.Player.UserId);
    }
}

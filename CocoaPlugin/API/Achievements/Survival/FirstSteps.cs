using CocoaPlugin.API.Managers;
using Exiled.API.Enums;

namespace CocoaPlugin.API.Achievements.Survival;

public class FirstSteps : ProgressiveAchievement
{
    public override AchievementType Type { get; set; } = AchievementType.FirstSteps;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Survival];
    public override string Name { get; set; } = "탈출의 첫걸음";
    public override string Description { get; set; } = "총 10번 탈출하십시오.";
    public override int NeededProgress { get; set; } = 10;

    public override void RegisterEvents()
    {
        Exiled.Events.Handlers.Player.Escaping += OnEscaping;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Player.Escaping -= OnEscaping;
    }

    private void OnEscaping(Exiled.Events.EventArgs.Player.EscapingEventArgs ev)
    {
        if (ev.EscapeScenario == EscapeScenario.None || ev.EscapeScenario == EscapeScenario.CustomEscape) return;
        if (!ev.IsAllowed) return;

        AddProgress(ev.Player.UserId);
    }
}
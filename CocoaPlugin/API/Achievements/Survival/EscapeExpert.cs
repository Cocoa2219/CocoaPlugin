﻿using CocoaPlugin.API.Managers;

namespace CocoaPlugin.API.Achievements.Survival;

public class EscapeExpert : ProgressiveAchievement
{
    public override AchievementType Type { get; set; } = AchievementType.EscapeExpert;
    public override Category Category { get; set; } = Category.Categories[AchievementCategory.Survival];
    public override string Name { get; set; } = "탈출 달인";
    public override string Description { get; set; } = "총 100번 탈출하십시오.";
    public override int NeededProgress { get; set; } = 100;

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
        if (ev.EscapeScenario == Exiled.API.Enums.EscapeScenario.None || ev.EscapeScenario == Exiled.API.Enums.EscapeScenario.CustomEscape) return;
        if (!ev.IsAllowed) return;

        AddProgress(ev.Player.UserId);
    }
}
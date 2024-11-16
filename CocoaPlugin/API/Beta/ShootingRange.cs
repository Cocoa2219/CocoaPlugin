using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AdminToys;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Toys;
using Exiled.Events.EventArgs.Item;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.BasicMessages;
using MapEditorReborn.API.Features;
using MapEditorReborn.API.Features.Objects;
using MEC;
using MultiBroadcast.API;
using PlayerRoles;
using UnityEngine;
using Firearm = Exiled.API.Features.Items.Firearm;
using Random = UnityEngine.Random;

namespace CocoaPlugin.API.Beta;

public class ShootingRange
{
    private Vector3 _spawnPosition = new(120, -960, 45);

    public List<ShootingRangeInstance> Instances { get; } = new();

    private Stopwatch _timeSinceLastHint;

    public ShootingRange()
    {
        Exiled.Events.Handlers.Player.Left += OnLeft;
        Exiled.Events.Handlers.Player.ChangingItem += OnChangingItem;
        Exiled.Events.Handlers.Player.TogglingWeaponFlashlight += OnTogglingFlashlight;
        Exiled.Events.Handlers.Item.ChangingAttachments += OnChangingAttachments;
        Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
        Exiled.Events.Handlers.Player.Shooting += OnShooting;
        Exiled.Events.Handlers.Player.AimingDownSight += OnAimingDownSight;
        Exiled.Events.Handlers.Player.Shot += OnShot;
    }

    public void Destroy()
    {
        Exiled.Events.Handlers.Item.ChangingAttachments -= OnChangingAttachments;
        Exiled.Events.Handlers.Player.TogglingWeaponFlashlight -= OnTogglingFlashlight;
        Exiled.Events.Handlers.Player.ChangingItem -= OnChangingItem;
        Exiled.Events.Handlers.Player.Left -= OnLeft;
        Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
        Exiled.Events.Handlers.Player.Shooting -= OnShooting;
        Exiled.Events.Handlers.Player.AimingDownSight -= OnAimingDownSight;
        Exiled.Events.Handlers.Player.Shot -= OnShot;

        foreach (var player in Instances.Select(x => x.Player))
            RemovePlayer(player);
    }

    public SchematicObject SpawnLobby()
    {
        var pos = _spawnPosition;

        _spawnPosition += new Vector3(0, 0, 12);

        return ObjectSpawner.SpawnSchematic("ShootingRangeLobby", pos, Quaternion.identity, Vector3.one, isStatic: true);
    }

    public bool AddPlayer(Player player)
    {
        if (Instances.Any(x => x.Player == player))
            return false;

        if (!player.IsHuman) player.Role.Set(RoleTypeId.ClassD);

        player.ClearInventory();
        player.EnableEffect(EffectType.SoundtrackMute);

        var pos = _spawnPosition;

        var schematic = SpawnLobby();

        var lobbyGun = Item.Create(ItemType.GunCOM15);

        var instance = new ShootingRangeInstance
        {
            Player = player,
            Schematic = schematic,
            State = ShootingRangeState.Lobby,
            Mode = Mode.Reflex,
            OriginalPosition = player.Position,
            SchematicPosition = pos,
            LobbyGun = lobbyGun.Serial,
            Difficulty = Difficulty.Easy,
            MenuState = MenuState.Mode,
            GunType = FirearmType.Com15
        };

        Instances.Add(instance);

        player.Position = pos;

        player.Role.As<FpcRole>().IsInvisible = true;

        lobbyGun.As<Firearm>().AddAttachment(AttachmentName.Flashlight);

        player.AddItem(lobbyGun);
        player.AddItem(ItemType.GrenadeHE);

        _timeSinceLastHint = Stopwatch.StartNew();
        RefreshHint(instance);

        player.CurrentItem = lobbyGun;
        return true;
    }

    public bool RemovePlayer(Player player)
    {
        if (Instances.All(x => x.Player != player))
            return false;

        var instance = Instances.First(x => x.Player == player);

        player.Position = instance.OriginalPosition;

        instance.Schematic.Destroy();

        Item.Get(instance.LobbyGun)?.Destroy();

        Instances.Remove(instance);

        player.ShowHint("");
        player.Role.As<FpcRole>().IsInvisible = false;

        player.DisableEffect(EffectType.SoundtrackMute);

        return true;
    }

    private void OnLeft(LeftEventArgs ev)
    {
        if (Instances.Any(x => x.Player == ev.Player))
            RemovePlayer(ev.Player);
    }

    private void OnChangingItem(ChangingItemEventArgs ev)
    {
        if (Instances.All(x => x.Player != ev.Player))
            return;

        var instance = Instances.First(x => x.Player == ev.Player);

        if (ev.Item == null)
        {
            ev.IsAllowed = false;
            return;
        }

        if (instance.State == ShootingRangeState.Ready)
        {
            switch (ev.Item.Type)
            {
                case ItemType.GrenadeHE:
                    ev.IsAllowed = false;

                    UnReady(instance);
                    break;
                case ItemType.Medkit:
                    ev.IsAllowed = false;

                    Start(instance);
                    break;
            }
            return;
        }

        if (instance.State != ShootingRangeState.Lobby)
        {
            if (ev.Item.Type == ItemType.Medkit)
            {
                ReturnToLobby(instance);
            }

            return;
        }

        if (ev.Item.Serial != instance.LobbyGun)
        {
            ev.IsAllowed = false;
        }

        if (ev.Item.Type == ItemType.GrenadeHE)
        {
            TakeAction(instance, ActionType.Next);
        }
    }

    private void OnTogglingFlashlight(TogglingWeaponFlashlightEventArgs ev)
    {
        if (Instances.All(x => x.Player != ev.Player))
            return;

        var instance = Instances.First(x => x.Player == ev.Player);

        if (instance.State != ShootingRangeState.Lobby)
            return;

        if (ev.Item.Serial != instance.LobbyGun) return;

        ev.IsAllowed = false;

        TakeAction(instance, ActionType.Previous);
    }

    private readonly Dictionary<Mode, string> _schematicNames = new()
    {
        {Mode.Reflex, "ShootingRangeReflex"},
        {Mode.Long, "ShootingRangeLong"},
        {Mode.ThreeHundredSixty, "ShootingRangeThreeHundredSixty"}
    };

    private void Start(ShootingRangeInstance instance)
    {
        instance.State = instance.Mode.ToShootingRangeState();

        var gun = instance.Player.Items.FirstOrDefault(x => x.IsWeapon)?.Clone() ?? (Firearm)Item.Create(instance.GunType.GetItemType());

        instance.Player.ClearItems();
        instance.Player.ShowHint("");

        instance.Player.AddItem(gun);

        Timing.CallDelayed(0.5f, () =>
        {
            if (instance.Player.CurrentItem != gun)
            {
                instance.Player.CurrentItem = gun;
            }
        });

        instance.Player.AddItem(ItemType.Medkit);

        instance.Schematic.Destroy();

        instance.Schematic = ObjectSpawner.SpawnSchematic(_schematicNames[instance.Mode], instance.SchematicPosition, Quaternion.identity, Vector3.one, isStatic: true);

        var spawnPos = instance.SchematicPosition;

        instance.Player.Position = spawnPos;

        ShootingRangeOption option;
        instance.Targets.Clear();

        float distance;
        switch (instance.Mode)
        {
            case Mode.Reflex:
                option = new ShootingRangeReflexOption();

                instance.Option = option;

                break;
            case Mode.Long:
                distance = instance.Difficulty switch
                {
                    Difficulty.Easy => 5f,
                    Difficulty.Normal => 10f,
                    Difficulty.Hard => 15f,
                    Difficulty.Extreme => 20f,
                    _ => 0f
                };

                var scale = instance.Difficulty switch
                {
                    Difficulty.Easy => 0.6f,
                    Difficulty.Normal => 0.5f,
                    Difficulty.Hard => 0.4f,
                    Difficulty.Extreme => 0.3f,
                    _ => 0f
                };

                option = new ShootingRangeLongOption
                {
                    TargetScale = scale,
                    Distance = distance,
                    Targets = 5
                };

                instance.Option = option;

                for (var i = 0; i < ((ShootingRangeLongOption)option).Targets; i++)
                {
                    var relativePosition = new Vector3(distance, Random.Range(-1.5f, 1.5f), Random.Range(-3f, 3f));

                    var position = instance.SchematicPosition + relativePosition;

                    SpawnLongTarget(instance, position, scale);
                }
                break;
            case Mode.ThreeHundredSixty:
                instance.Player.EnableEffect(EffectType.Ensnared);

                distance = instance.Difficulty switch
                {
                    Difficulty.Easy => 5f,
                    Difficulty.Normal => 10f,
                    Difficulty.Hard => 15f,
                    Difficulty.Extreme => 20f,
                    _ => 0f
                };

                var interval = instance.Difficulty switch
                {
                    Difficulty.Easy => 2f,
                    Difficulty.Normal => 1.5f,
                    Difficulty.Hard => 1f,
                    Difficulty.Extreme => 0.5f,
                    _ => 0f
                };

                var isColored = instance.Difficulty switch
                {
                    Difficulty.Easy => false,
                    Difficulty.Normal => false,
                    Difficulty.Hard => true,
                    Difficulty.Extreme => true,
                    _ => false
                };

                option = new ShootingRangeThreeHundredSixtyOption()
                {
                    Distance = distance,
                    Interval = interval,
                    Duration = 3f,
                    IsColored = isColored
                };
                instance.Option = option;

                instance.Coroutines.Add(Timing.RunCoroutine(ThreeHundredSixtyCoroutine(instance)));
                break;
        }

        instance.Coroutines.Add(Timing.RunCoroutine(GameCoroutine(instance)));
    }

    private Primitive SpawnLongTarget(ShootingRangeInstance instance, Vector3 position, float scale)
    {
        var target = Primitive.Create(PrimitiveType.Sphere, position, Vector3.zero, Vector3.zero, true, Color.white);

        var t = target.Base.gameObject.AddComponent<LongTarget>();

        t.Init(target);

        t.Scale = scale;

        instance.Targets.Add(t);

        return target;
    }

    private Primitive Spawn360Target(ShootingRangeInstance instance, bool isTarget)
    {
        if (instance.Option is not ShootingRangeThreeHundredSixtyOption option)
            return null;

        var position = GetRandom360Position(option.Distance);
        var absPosition = instance.SchematicPosition + position;

        var target = Primitive.Create(PrimitiveType.Sphere, absPosition, Vector3.zero, Vector3.zero, true, Color.white);

        var t = target.Base.gameObject.AddComponent<ThreeHundredSixtyTarget>();

        t.Init(target);

        t.Scale = 0.5f;
        t.IsTarget = isTarget;
        t.SetTargetTime(option.Duration);

        return target;
    }

    private IEnumerator<float> GameCoroutine(ShootingRangeInstance instance)
    {
        if (instance.Option == null)
            yield break;

        var timer = instance.Option.Time;

        while (timer > 0)
        {
            yield return Timing.WaitForSeconds(1f);

            timer--;

            instance.Timer = timer;

            RefreshBroadcast(instance);
        }

        ReturnToLobby(instance);
    }

    private IEnumerator<float> ThreeHundredSixtyCoroutine(ShootingRangeInstance instance)
    {
        if (instance.Option == null)
            yield break;

        var option = (ShootingRangeThreeHundredSixtyOption)instance.Option;

        var interval = option.Interval;
        var isColored = option.IsColored;

        var colorQueue = new List<bool>();

        while (instance.State == ShootingRangeState.ThreeHundredSixty)
        {
            yield return Timing.WaitForSeconds(interval);

            if (isColored)
            {
                if (colorQueue.Count == 0)
                {
                    colorQueue.AddRange(new List<bool> {true, true, false, false});
                    colorQueue.ShuffleListSecure();
                }

                var isColoredTarget = colorQueue[0];

                colorQueue.RemoveAt(0);

                Spawn360Target(instance, isColoredTarget);
            }
            else
            {
                Spawn360Target(instance, false);
            }
        }
    }

    private Vector3 GetRandom360Position(float radius)
    {
        var angle = Random.Range(0f, 360f);

        var x = radius * Mathf.Cos(angle);
        var y = Random.Range(-1.5f, 1.5f);
        var z = radius * Mathf.Sin(angle);

        return new Vector3(x, y, z);
    }

    private void RefreshBroadcast(ShootingRangeInstance instance, ushort duration = 2)
    {
        var min = instance.Timer / 60;
        var sec = instance.Timer % 60;

        var sb = new StringBuilder(GameBroadcast);

        sb.Replace("[mode]", instance.Mode.GetShootingRangeStateText());
        sb.Replace("[difficulty]", $"<color={instance.Difficulty.GetDifficultyColor()}>{instance.Difficulty.GetDifficultyText(instance.Mode)}</color>");
        sb.Replace("[gun]", instance.GunType.ToString());

        sb.Replace("[min]", min.ToString("00"));
        sb.Replace("[sec]", sec.ToString("00"));

        sb.Replace("[score]", instance.Score.ToString());
        sb.Replace("[total]", instance.Total.ToString());
        sb.Replace("[accuracy]", instance.Total == 0 ? "0%" : $"{(int)((float)instance.Score / instance.Total * 100)}%");
        sb.Replace("[combo]", instance.Combo.ToString());

        if (instance.Player.HasBroadcast("ShootingRange"))
            instance.Player.RemoveBroadcast("ShootingRange");

        instance.Player.AddBroadcast(duration, sb.ToString(), 0, "ShootingRange");
    }

    public void ShowResult(ShootingRangeInstance instance)
    {
        var sb = new StringBuilder(ResultBroadcast);

        sb.Replace("[mode]", instance.Mode.GetShootingRangeStateText());
        sb.Replace("[difficulty]", $"<color={instance.Difficulty.GetDifficultyColor()}>{instance.Difficulty.GetDifficultyText(instance.Mode)}</color>");
        sb.Replace("[gun]", instance.GunType.ToString());

        sb.Replace("[score]", instance.Score.ToString());
        sb.Replace("[total]", instance.Total.ToString());
        sb.Replace("[accuracy]", instance.Total == 0 ? "0%" : $"{(int)((float)instance.Score / instance.Total * 100)}%");
        sb.Replace("[combo]", instance.Combo.ToString());

        instance.Player.AddBroadcast(10, sb.ToString(), 0, "ShootingRangeResult");
    }

    private void TakeAction(ShootingRangeInstance instance, ActionType type)
    {
        // Log.Info($"Player {instance.Player.Nickname} took action {type}");

        if (_timeSinceLastHint.ElapsedMilliseconds < 501)
        {
            if (instance.Player.HasBroadcast("HintRateLimit"))
                instance.Player.RemoveBroadcast("HintRateLimit");

            instance.Player.AddBroadcast(3, "<size=20><cspace=0.05em>너무 빨리 설정을 바꾸고 있습니다. 몇 초 후에 다시 시도해주세요.</cspace></size>", 0, "HintRateLimit");
            _timeSinceLastHint.Restart();
            return;
        }

        switch (type)
        {
            case ActionType.Down:
            {
                var newState = instance.MenuState + 1;

                if (!Enum.IsDefined(typeof(MenuState), newState)) newState = MenuState.Mode;

                instance.MenuState = newState;
                break;
            }
            case ActionType.Up:
            {
                var newState = instance.MenuState - 1;

                if (!Enum.IsDefined(typeof(MenuState), newState)) newState = MenuState.Gun;

                instance.MenuState = newState;
                break;
            }
            case ActionType.Select:
                // Log.Info($"Player {instance.Player.Nickname} selected {instance.MenuState}");
                Ready(instance);
                return;
        }

        switch (instance.MenuState)
        {
            case MenuState.Mode:
                var state = instance.Mode;

                switch (type)
                {
                    case ActionType.Next:
                        var newState = state + 1;

                        if (!Enum.IsDefined(typeof(Mode), newState)) newState = Mode.Reflex;

                        state = newState;
                        break;
                    case ActionType.Previous:
                        var newState2 = state - 1;

                        if (!Enum.IsDefined(typeof(Mode), newState2)) newState2 = Mode.Long;

                        state = newState2;
                        break;
                }

                instance.Mode = state;
                break;
            case MenuState.Difficulty:
                var difficulty = instance.Difficulty;

                switch (type)
                {
                    case ActionType.Next:
                        var newDifficulty = difficulty + 1;

                        if (!Enum.IsDefined(typeof(Difficulty), newDifficulty)) newDifficulty = Difficulty.Easy;

                        difficulty = newDifficulty;
                        break;
                    case ActionType.Previous:
                        var newDifficulty2 = difficulty - 1;

                        if (!Enum.IsDefined(typeof(Difficulty), newDifficulty2)) newDifficulty2 = Difficulty.Extreme;

                        difficulty = newDifficulty2;
                        break;
                }

                instance.Difficulty = difficulty;

                break;
            case MenuState.Gun:
                var gun = instance.GunType;

                switch (type)
                {
                    case ActionType.Next:
                        var newType = gun + 1;

                        if (!Enum.IsDefined(typeof(FirearmType), newType)) newType = FirearmType.Com15;

                        if (Enum.IsDefined(typeof(FirearmType), newType) && newType == FirearmType.ParticleDisruptor) newType++;

                        instance.GunType = newType;
                        break;
                    case ActionType.Previous:
                        var newType2 = gun - 1;

                        if (!Enum.IsDefined(typeof(FirearmType), newType2) || newType2 == FirearmType.None) newType2 = FirearmType.A7;

                        if (Enum.IsDefined(typeof(FirearmType), newType2) && newType2 == FirearmType.ParticleDisruptor) newType2--;

                        instance.GunType = newType2;
                        break;
                }

                break;
        }

        RefreshHint(instance);
    }

    private void Ready(ShootingRangeInstance instance)
    {
        instance.State = ShootingRangeState.Ready;

        instance.Player.RemoveBroadcast("HintRateLimit");

        instance.Player.ShowHint(ReadyHint, 300f);

        Item.Get(instance.LobbyGun)?.Destroy();

        instance.Player.ClearItems();

        instance.Player.AddItem(instance.GunType.GetItemType());

        instance.Player.AddItem(ItemType.Medkit);
        instance.Player.AddItem(ItemType.GrenadeHE);
    }

    private void UnReady(ShootingRangeInstance instance)
    {
        instance.State = ShootingRangeState.Lobby;

        instance.Player.ShowHint("", 300f);

        instance.Player.ClearItems();

        var lobbyGun = Item.Create(ItemType.GunCOM15);

        instance.LobbyGun = lobbyGun.Serial;

        lobbyGun.As<Firearm>().AddAttachment(AttachmentName.Flashlight);

        instance.Player.AddItem(lobbyGun);
        instance.Player.AddItem(ItemType.GrenadeHE);

        instance.Player.CurrentItem = lobbyGun;

        _timeSinceLastHint.Restart();

        RefreshHint(instance);
    }

    private const string ResultBroadcast =
        "<cspace=0.05em><size=30><color=#ffda75>모드 / </color>[mode] | <color=#ffda75>난이도 / </color>[difficulty] | <color=#ff8875>[gun]</color></size>\n<size=35><b>[score] / [total] (x[combo]) | [accuracy]</b></size></cspace>";

    private const string GameBroadcast =
        "<cspace=0.05em><size=30><color=#ffda75>모드 / </color>[mode] | <color=#ffda75>난이도 / </color>[difficulty] | <color=#ff8875>[gun]</color></size>\n<size=35><b>[min] : [sec] <color=#ff8875>|</color> [score] / [total] <size=30>(x[combo])</size> <sup>[accuracy]</sup></b></size></cspace>";

    private const string ReadyHint = "\n\n<b><size=30><color=#ffda75>✓</color> <color=#999999>[X]</color> <color=#ff8875>|</color> <color=#ffda75>←</color> <color=#999999>[G]</color></size></b>";

    private const string HintFormat =
        "<size=30><b>[text]</b></size>\n<b><size=20><color=#ffda75>◀</color> <color=#999999>[F]</color> <color=#ff8875>|</color> <color=#999999>[G]</color> <color=#ffda75>▶</color> <color=#ff8875>|</color> <color=#999999>[T] 선택</color> <color=#ff8875>|</color> <color=#999999>[LMB]</color> <color=#ffda75>▲</color> <color=#ff8875>|</color> <color=#999999>[RMB]</color> <color=#ffda75>▼</color></size></b>";

    private readonly Dictionary<MenuState, (string menu, string selected)> _menuTexts = new()
    {
        {MenuState.Mode, ("<color=#999999>모드 | </color>[mode]", "<color=#ffda75>모드 | </color>[mode]")},
        {MenuState.Difficulty, ("<color=#999999>난이도 | </color>[difficulty]", "<color=#ffda75>난이도 | </color>[difficulty]")},
        {MenuState.Gun, ("<size=35>[gun]</size>", "<size=35>[gun]</size>")}
    };

    private void RefreshHint(ShootingRangeInstance instance)
    {
        Log.Debug($"Refreshing hi   nt for {instance.Player.Nickname}");

        var sb = new StringBuilder();

        foreach (var menu in _menuTexts)
        {
            var textSb = new StringBuilder(menu.Key == instance.MenuState ? menu.Value.selected : menu.Value.menu);

            var unselectedColor = menu.Key == instance.MenuState ? "#cccccc" : "#999999";
            var modeColor = menu.Key == MenuState.Mode ? menu.Key == instance.MenuState ? "#ff8875" : "#cccccc" : "#cccccc";
            var difficultyColor =
                menu.Key == MenuState.Difficulty ? menu.Key == instance.MenuState ? instance.Difficulty.GetDifficultyColor() : "#cccccc" : "#cccccc";
            var gunColor = menu.Key == MenuState.Gun ? menu.Key == instance.MenuState ? "#ff8875" : "#cccccc" : "#cccccc";

            var modeText = $"<size=20><color={unselectedColor}>{instance.Mode.GetShootingRangeMode(true)}</color></size> <color={modeColor}>{instance.Mode.GetShootingRangeStateText()}</color> <size=20><color={unselectedColor}>{instance.Mode.GetShootingRangeMode(false)}</color></size>";
            var difficultyText = $"<size=20><color={unselectedColor}>{instance.Difficulty.GetShootingRangeDifficulty(true)}</color></size> <color={difficultyColor}>{instance.Difficulty.GetDifficultyText(instance.Mode)}</color> <size=20><color={unselectedColor}>{instance.Difficulty.GetShootingRangeDifficulty(false)}</color></size>";
            var gunText = $"<size=20><color={unselectedColor}>{instance.GunType.GetShootingRangeGun(true)}</color></size> <color={gunColor}>{instance.GunType}</color> <size=20><color={unselectedColor}>{instance.GunType.GetShootingRangeGun(false)}</color></size>";

            textSb.Replace("[mode]", modeText);
            textSb.Replace("[difficulty]", difficultyText);
            textSb.Replace("[gun]", gunText);

            sb.AppendLine(textSb.ToString());
        }

        _timeSinceLastHint.Restart();

        instance.Player.ShowHint(HintFormat.Replace("[text]", sb.ToString().Trim()), 300f);
    }

    private void ReturnToLobby(ShootingRangeInstance instance)
    {
        Timing.KillCoroutines(instance.Coroutines.ToArray());

        instance.State = ShootingRangeState.Lobby;

        instance.Player.ClearItems();

        instance.Schematic.Destroy();

        instance.Schematic = ObjectSpawner.SpawnSchematic("ShootingRangeLobby", instance.SchematicPosition, Quaternion.identity, Vector3.one, isStatic: true);

        var lobbyGun = Item.Create(ItemType.GunCOM15);

        lobbyGun.As<Firearm>().AddAttachment(AttachmentName.Flashlight);

        instance.Player.AddItem(lobbyGun);

        instance.Player.AddItem(ItemType.GrenadeHE);

        instance.Player.DisableEffect(EffectType.Ensnared);

        instance.LobbyGun = lobbyGun.Serial;

        instance.Player.CurrentItem = lobbyGun;

        instance.Player.RemoveBroadcast("ShootingRange");

        ShowResult(instance);

        instance.Score = 0;
        instance.Total = 0;
        instance.Timer = 0;
        instance.Combo = 0;

        foreach (var target in instance.Targets)
            UnityEngine.Object.Destroy(target.Primitive.Base.gameObject);

        instance.Targets.Clear();

        RefreshHint(instance);
    }

    private void OnDroppingItem(DroppingItemEventArgs ev)
    {
        if (Instances.All(x => x.Player != ev.Player))
            return;

        var instance = Instances.First(x => x.Player == ev.Player);

        ev.IsAllowed = false;

        if (instance.State != ShootingRangeState.Lobby)
            return;

        if (ev.IsThrown)
        {
            TakeAction(instance, ActionType.Select);
        }
    }

    private void OnChangingAttachments(ChangingAttachmentsEventArgs ev)
    {
        if (Instances.All(x => x.Player != ev.Player))
            return;

        var instance = Instances.First(x => x.Player == ev.Player);

        if (instance.State != ShootingRangeState.Lobby)
            return;

        if (ev.Item.Serial != instance.LobbyGun) return;

        ev.IsAllowed = false;
    }

    private void OnShooting(ShootingEventArgs ev)
    {
        if (Instances.All(x => x.Player != ev.Player))
            return;

        var instance = Instances.First(x => x.Player == ev.Player);

        switch (instance.State)
        {
            case ShootingRangeState.Lobby:
            {
                if (ev.Firearm.Serial == instance.LobbyGun)
                {
                    ev.IsAllowed = false;
                    TakeAction(instance, ActionType.Up);
                }

                return;
            }
            case ShootingRangeState.Ready:
                ev.Firearm.Ammo = ev.Firearm.MaxAmmo;
                return;
        }

        instance.Total += 1;
    }

    private void OnShot(ShotEventArgs ev)
    {
        if (Instances.All(x => x.Player != ev.Player))
            return;

        var instance = Instances.First(x => x.Player == ev.Player);

        if (instance.State is ShootingRangeState.Ready or ShootingRangeState.Lobby) return;

        ev.Firearm.Ammo = ev.Firearm.MaxAmmo;

        instance.Player.Connection.Send(new StatusMessage()
        {
            Serial = ev.Firearm.Serial,
            Status = new FirearmStatus(ev.Firearm.MaxAmmo, ev.Firearm.Base.Status.Flags, ev.Firearm.Base.Status.Attachments)
        });

        if (ev.RaycastHit.collider.TryGetComponent(out Target target))
        {
            OnShootTarget(target, instance);
        }
        else
        {
            OnMissTarget(instance);
        }
    }

    private void OnShootTarget(Target target, ShootingRangeInstance instance)
    {
        if (target == null)
        {
            return;
        }

        instance.Player.ShowHitMarker();

        instance.Player.Connection.Send(new AntiScp207.BreakMessage
        {
            SoundPos = target.transform.position
        });

        instance.Targets.Remove(target);

        UnityEngine.Object.Destroy(target.gameObject);

        switch (target)
        {
            case LongTarget:
            {
                instance.Score += 1;
                instance.Combo += 1;

                var position = new Vector3(((ShootingRangeLongOption)instance.Option).Distance, Random.Range(-1.5f, 1.5f), Random.Range(-3f, 3f));
                var relativePosition = instance.SchematicPosition + position;

                SpawnLongTarget(instance, relativePosition, ((ShootingRangeLongOption)instance.Option).TargetScale);
                break;
            }
            case ThreeHundredSixtyTarget threeHundredSixtyTarget:
            {
                var isTarget = threeHundredSixtyTarget.IsTarget;

                if (isTarget)
                {
                    instance.Score += 1;
                    instance.Combo += 1;
                }
                else if (instance.Score > 0)
                {
                    instance.Score -= 1;
                    instance.Combo = 0;
                }

                break;
            }
        }

        RefreshBroadcast(instance);
    }

    private void OnMissTarget(ShootingRangeInstance instance)
    {
        instance.Combo = 0;

        RefreshBroadcast(instance);
    }

    private void OnAimingDownSight(AimingDownSightEventArgs ev)
    {
        if (Instances.All(x => x.Player != ev.Player))
            return;

        var instance = Instances.First(x => x.Player == ev.Player);

        if (!ev.AdsIn) return;

        if (instance.State == ShootingRangeState.Lobby)
        {
            if (ev.Firearm.Serial == instance.LobbyGun)
            {
                TakeAction(instance, ActionType.Down);
            }
        }
    }
}

public static class ShootingRangeExtensions
{
    public static string GetShootingRangeMode(this Mode mode, bool previous)
    {
        if (previous)
        {
            var newMode = mode - 1;

            if (!Enum.IsDefined(typeof(Mode), newMode)) newMode = Mode.Long;

            return newMode.GetShootingRangeStateText();
        }

        var newMode2 = mode + 1;

        if (!Enum.IsDefined(typeof(Mode), newMode2)) newMode2 = Mode.Reflex;

        return newMode2.GetShootingRangeStateText();
    }

    public static string GetShootingRangeDifficulty(this Difficulty difficulty, bool previous)
    {
        if (previous)
        {
            var newDifficulty = difficulty - 1;

            if (!Enum.IsDefined(typeof(Difficulty), newDifficulty)) newDifficulty = Difficulty.Extreme;

            return newDifficulty.GetDifficultyText();
        }

        var newDifficulty2 = difficulty + 1;

        if (!Enum.IsDefined(typeof(Difficulty), newDifficulty2)) newDifficulty2 = Difficulty.Easy;

        return newDifficulty2.GetDifficultyText();
    }

    public static string GetShootingRangeGun(this FirearmType gun, bool previous)
    {
        if (previous)
        {
            var newGun = gun - 1;

            if (!Enum.IsDefined(typeof(FirearmType), newGun) || newGun == FirearmType.None) newGun = FirearmType.A7;

            if (Enum.IsDefined(typeof(FirearmType), newGun) && newGun == FirearmType.ParticleDisruptor) newGun--;

            return newGun.ToString();
        }

        var newGun2 = gun + 1;

        if (!Enum.IsDefined(typeof(FirearmType), newGun2)) newGun2 = FirearmType.Com15;

        if (Enum.IsDefined(typeof(FirearmType), newGun2) && newGun2 == FirearmType.ParticleDisruptor) newGun2++;

        return newGun2.ToString();
    }

    public static string GetDifficultyColor(this Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => "#8BC34A",    // Light Green
            Difficulty.Normal => "#FF9800",  // Orange
            Difficulty.Hard => "#F44336",    // Red
            Difficulty.Extreme => "#9C27B0", // Purple
            _ => "#FFFFFF"                   // White (default case)
        };
    }

    public static string GetDifficultyText(this Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => "쉬움",
            Difficulty.Normal => "보통",
            Difficulty.Hard => "어려움",
            Difficulty.Extreme => "익스트림",
            _ => "알 수 없음"
        };
    }

    public static string GetDifficultyText(this Difficulty difficulty, Mode mode)
    {
        switch (mode)
        {
            case Mode.Long:
                return difficulty switch
                {
                    Difficulty.Easy => "쉬움 <size=20>5m, x0.6</size>",
                    Difficulty.Normal => "보통 <size=20>10m, x0.5</size>",
                    Difficulty.Hard => "어려움 <size=20>15m, x0.4</size>",
                    Difficulty.Extreme => "익스트림 <size=20>20m, x0.3</size>",
                    _ => "알 수 없음"
                };
            case Mode.Reflex:
            case Mode.ThreeHundredSixty:
            default:
                return difficulty switch
                {
                    Difficulty.Easy => "쉬움",
                    Difficulty.Normal => "보통",
                    Difficulty.Hard => "어려움",
                    Difficulty.Extreme => "익스트림",
                    _ => "알 수 없음"
                };
        }
    }

    public static string GetShootingRangeStateText(this Mode state)
    {
        return state switch
        {
            Mode.Reflex => "반응 속도",
            Mode.Long => "장거리",
            Mode.ThreeHundredSixty => "360°",
            _ => "알 수 없음"
        };
    }

    public static ShootingRangeState ToShootingRangeState(this Mode mode)
    {
        return mode switch
        {
            Mode.Reflex => ShootingRangeState.Reflex,
            Mode.Long => ShootingRangeState.Long,
            Mode.ThreeHundredSixty => ShootingRangeState.ThreeHundredSixty,
            _ => ShootingRangeState.Lobby
        };
    }
}

public abstract class ShootingRangeOption
{
    public abstract int Time { get; set; }
    public abstract float TargetScale { get; set; }
}

public class ShootingRangeLongOption : ShootingRangeOption
{
    public override float TargetScale { get; set; } = 0.6f;
    public override int Time { get; set; } = 90;
    public float Distance { get; set; }
    public int Targets { get; set; }
}

public class ShootingRangeReflexOption : ShootingRangeOption
{
    public override float TargetScale { get; set; } = 0.6f;
    public override int Time { get; set; } = 90;
    public float Interval { get; set; }
    public float Duration { get; set; }
}

public class ShootingRangeThreeHundredSixtyOption : ShootingRangeOption
{
    public override float TargetScale { get; set; } = 0.6f;
    public override int Time { get; set; } = 90;
    public bool IsColored { get; set; }
    public float Interval { get; set; }
    public float Distance { get; set; }
    public float Duration { get; set; }
}

public class ShootingRangeInstance
{
    public Player Player { get; set; }
    public SchematicObject Schematic { get; set; }
    public ShootingRangeState State { get; set; }
    public Mode Mode { get; set; }
    public Vector3 SchematicPosition { get; set; }
    public Vector3 OriginalPosition { get; set; }
    public ushort LobbyGun { get; set; }
    public Difficulty Difficulty { get; set; }
    public MenuState MenuState { get; set; }
    public FirearmType GunType { get; set; }
    public int Score { get; set; }
    public int Total { get; set; }
    public int Timer { get; set; }
    public int Combo { get; set; }
    public List<Target> Targets { get; set; } = [];
    public ShootingRangeOption Option { get; set; }
    public List<CoroutineHandle> Coroutines { get; set; } = [];
}

public enum Mode
{
    Reflex,
    ThreeHundredSixty,
    Long
}

public enum ShootingRangeState
{
    Lobby,
    Ready,
    Reflex,
    ThreeHundredSixty,
    Long
}

public enum Difficulty
{
    Easy,
    Normal,
    Hard,
    Extreme
}

public enum MenuState
{
    Mode,
    Difficulty,
    Gun
}

public enum ActionType
{
    Next,
    Previous,
    Up,
    Down,
    Select
}

public class Target : MonoBehaviour
{
    public float Scale
    {
        set => Primitive.Scale = Vector3.one * value;
    }

    internal Primitive Primitive { get; set; }

    public virtual void Init(Primitive primitive)
    {
        Primitive = primitive;
    }
}

public class LongTarget : Target;

public class ThreeHundredSixtyTarget : Target
{
    public float Timer { get; set; }

    private bool _isTarget;

    public bool IsTarget
    {
        get => _isTarget;
        set
        {
            _isTarget = value;
            Primitive.Color = _isTarget ? Color.red : Color.white;
        }
    }

    private float _targetTime;

    public void SetTargetTime(float time)
    {
        _targetTime = time;
    }

    public override void Init(Primitive primitive)
    {
        base.Init(primitive);

        Timer = 0f;
    }

    private void Update()
    {
        Timer += UnityEngine.Time.deltaTime;

        if (Timer >= _targetTime)
        {
            Destroy(gameObject);
        }
    }
}
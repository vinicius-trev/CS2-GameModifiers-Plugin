
using System.Collections.Generic;

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace GameModifiers.Modifiers;

public abstract class GameModifierVelocity : GameModifierBase
{
    public virtual float SpeedMultiplier { get; protected set; } = 1.0f;
    private Timer? _speedTimer = null;

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        }

        Utilities.GetPlayers().ForEach(controller =>
        {
            GameModifiersUtils.SetPlayerSpeedMultiplier(controller, SpeedMultiplier);
        });

        _speedTimer = new Timer(0.2f, OnSpeedTimer, TimerFlags.REPEAT);
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        }

        Utilities.GetPlayers().ForEach(controller =>
        {
            GameModifiersUtils.SetPlayerSpeedMultiplier(controller, 1.0f);
        });

        if (_speedTimer != null)
        {
            _speedTimer.Kill();
            _speedTimer = null;
        }

        base.Disabled();
    }

    private void OnSpeedTimer()
    {
        Utilities.GetPlayers().ForEach(controller =>
        {
            GameModifiersUtils.SetPlayerSpeedMultiplier(controller, SpeedMultiplier);
        });
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false || player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        GameModifiersUtils.SetPlayerSpeedMultiplier(player, SpeedMultiplier);
        return HookResult.Continue;
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false || player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        GameModifiersUtils.SetPlayerSpeedMultiplier(player, SpeedMultiplier);
        return HookResult.Continue;
    }
}

public class GameModifierLightweight : GameModifierVelocity
{
    public override string Name => "Lightweight";
    public override string Description => "Max movement speed is much faster";
    public override bool SupportsRandomRounds => true;
    public override float SpeedMultiplier { get; protected set; } = 2.0f;
    public override HashSet<string> IncompatibleModifiers =>
    [
        "LeadBoots"
    ];
}

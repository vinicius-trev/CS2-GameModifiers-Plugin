
using System.Collections.Generic;

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace GameModifiers.Modifiers;

public class GameModifierSwapPlacesOnKill : GameModifierBase
{
    public override string Name => "SwapOnDeath";
    public override string Description => "Players will swap places on kill.";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierSwapPlacesOnHit>(),
        GameModifiersUtils.GetModifierName<GameModifierTeleportOnReload>(),
        GameModifiersUtils.GetModifierName<GameModifierTeleportOnHit>(),
        GameModifiersUtils.GetModifierName<GameModifierResetOnReload>()
    ];

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        }
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        }

        base.Disabled();
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        CCSPlayerController? attackerPlayer = @event.Attacker;
        CCSPlayerController? victimPlayer = @event.Userid;
        if (attackerPlayer == victimPlayer)
        {
            return HookResult.Continue;
        }

        if (attackerPlayer == null || !attackerPlayer.IsValid || victimPlayer == null || !victimPlayer.IsValid)
        {
            return HookResult.Continue;
        }

        CCSPlayerPawn? attackingPawn = attackerPlayer.PlayerPawn.Value;
        CCSPlayerPawn? victimPawn = victimPlayer.PlayerPawn.Value;
        if (attackingPawn == null || !attackingPawn.IsValid || victimPawn == null || !victimPawn.IsValid)
        {
            return HookResult.Continue;
        }

        if (victimPawn.AbsOrigin == null)
        {
            return HookResult.Continue;
        }

        GameModifiersUtils.TeleportPlayer(attackerPlayer, victimPawn.AbsOrigin);
        return HookResult.Continue;
    }
}

public class GameModifierSwapPlacesOnHit : GameModifierBase
{
    public override string Name => "SwapOnHit";
    public override string Description => "Players will swap places on hit";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierSwapPlacesOnKill>(),
        GameModifiersUtils.GetModifierName<GameModifierTeleportOnReload>(),
        GameModifiersUtils.GetModifierName<GameModifierTeleportOnHit>(),
        GameModifiersUtils.GetModifierName<GameModifierResetOnReload>()
    ];

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurtEvent);
        }
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurtEvent);
        }

        base.Disabled();
    }

    private HookResult OnPlayerHurtEvent(EventPlayerHurt @event, GameEventInfo info)
    {
        GameModifiersUtils.SwapPlayerLocations(@event.Attacker, @event.Userid);
        return HookResult.Continue;
    }
}

public class GameModifierRandomSpawn : GameModifierBase
{
    public override string Name => "RandomSpawns";
    public override string Description => "Players spawn locations are randomized";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierImposters>()
    ];

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        }

        Utilities.GetPlayers().ForEach(ApplyRandomSpawnToPlayer);
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        }

        base.Disabled();
    }

    protected void ApplyRandomSpawnToPlayer(CCSPlayerController? player)
    {
        GameModifiersUtils.TeleportPlayerToRandomSpot(player);
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false || player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        ApplyRandomSpawnToPlayer(player);
        return HookResult.Continue;
    }
}

public class GameModifierTeleportOnReload : GameModifierBase
{
    public override string Name => "TeleportOnReload";
    public override string Description => "Players are teleported to a random spot on reload";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierSwapPlacesOnKill>(),
        GameModifiersUtils.GetModifierName<GameModifierSwapPlacesOnHit>(),
        GameModifiersUtils.GetModifierName<GameModifierTeleportOnHit>(),
        GameModifiersUtils.GetModifierName<GameModifierResetOnReload>()
    ];

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventWeaponReload>(OnPlayerReload);
        }
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventWeaponReload>(OnPlayerReload);
        }

        base.Disabled();
    }

    private HookResult OnPlayerReload(EventWeaponReload @event, GameEventInfo info)
    {
        CCSPlayerController? reloadingPlayer = @event.Userid;
        if (reloadingPlayer == null || !reloadingPlayer.IsValid || !reloadingPlayer.PawnIsAlive)
        {
            return HookResult.Continue;
        }

        GameModifiersUtils.TeleportPlayerToRandomSpot(reloadingPlayer);
        return HookResult.Continue;
    }
}

public class GameModifierTeleportOnHit : GameModifierBase
{
    public override string Name => "TeleportOnHit";
    public override string Description => "Players are teleported to a random spot on hit";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierSwapPlacesOnKill>(),
        GameModifiersUtils.GetModifierName<GameModifierSwapPlacesOnHit>(),
        GameModifiersUtils.GetModifierName<GameModifierTeleportOnReload>(),
        GameModifiersUtils.GetModifierName<GameModifierResetOnReload>()
    ];

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        }
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        }

        base.Disabled();
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        CCSPlayerController? damagedPlayer = @event.Userid;
        if (damagedPlayer == null || !damagedPlayer.IsValid || !damagedPlayer.PawnIsAlive)
        {
            return HookResult.Continue;
        }

        GameModifiersUtils.TeleportPlayerToRandomSpot(damagedPlayer);
        return HookResult.Continue;
    }
}

public class GameModifierResetOnReload : GameModifierBase
{
    public override string Name => "ResetOnReload";
    public override string Description => "Players are teleported back to their spawn on reload";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierSwapPlacesOnKill>(),
        GameModifiersUtils.GetModifierName<GameModifierSwapPlacesOnHit>(),
        GameModifiersUtils.GetModifierName<GameModifierTeleportOnReload>(),
        GameModifiersUtils.GetModifierName<GameModifierTeleportOnHit>()
    ];

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventWeaponReload>(OnPlayerReload);
        }
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventWeaponReload>(OnPlayerReload);
        }

        base.Disabled();
    }

    private HookResult OnPlayerReload(EventWeaponReload @event, GameEventInfo info)
    {
        CCSPlayerController? reloadingPlayer = @event.Userid;
        if (reloadingPlayer == null || !reloadingPlayer.IsValid || !reloadingPlayer.PawnIsAlive)
        {
            return HookResult.Continue;
        }

        GameModifiersUtils.TeleportPlayerToSpawnArea(reloadingPlayer);
        return HookResult.Continue;
    }
}

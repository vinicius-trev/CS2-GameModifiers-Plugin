
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.VisualBasic.CompilerServices;

namespace GameModifiers.Modifiers;

public abstract class GameModifierInvisibleBase : GameModifierBase
{
    protected readonly List<int> CachedInvisiblePlayers = new();

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Pre);
            Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Pre);
            Core.RemoveListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        base.Disabled();
    }

    protected void SetPlayerInvisible(CCSPlayerController? player, bool invisible)
    {
        if (player == null || !player.IsValid)
        {
            return;
        }

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
        {
            return;
        }

        if (invisible)
        {
            if (CachedInvisiblePlayers.Contains(player.Slot))
            {
                return;
            }

            playerPawn.Render = Color.FromArgb(0, 255, 255, 255);
            CachedInvisiblePlayers.Add(player.Slot);
        }
        else
        {
            if (!CachedInvisiblePlayers.Contains(player.Slot))
            {
                return;
            }

            playerPawn.Render = Color.FromArgb(255, 255, 255, 255);
            CachedInvisiblePlayers.Remove(player.Slot);
        }

        Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false || player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        SetPlayerInvisible(player, false);
        return HookResult.Continue;
    }

    private void OnClientDisconnect(int slot)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);
        if (player == null || !player.IsValid)
        {
            if (CachedInvisiblePlayers.Contains(slot))
            {
                CachedInvisiblePlayers.Remove(slot);
            }

            return;
        }

        SetPlayerInvisible(player, false);
    }
}

public class GameModifierCloaked : GameModifierInvisibleBase
{
    public override string Name => "Cloaked";
    public override string Description => "Everyone is invisible";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierRandomCloak>(),
        GameModifiersUtils.GetModifierName<GameModifierSingleCloak>()
    ];

    public override void Enabled()
    {
        base.Enabled();

        Utilities.GetPlayers().ForEach(player =>
        {
            SetPlayerInvisible(player, true);
        });
    }

    public override void Disabled()
    {
        Utilities.GetPlayers().ForEach(player =>
        {
            SetPlayerInvisible(player, false);
        });

        base.Disabled();
    }
}

public class GameModifierRandomCloak : GameModifierInvisibleBase
{
    public override string Name => "RandomCloak";
    public override string Description => "Everyone has a random chance to be invisible";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierCloaked>(),
        GameModifiersUtils.GetModifierName<GameModifierSingleCloak>()
    ];

    public override void Enabled()
    {
        base.Enabled();

        Utilities.GetPlayers().ForEach(player =>
        {
            if (Random.Shared.Next(0, 2) == 0)
            {
                SetPlayerInvisible(player, true);
            }
        });
    }

    public override void Disabled()
    {
        List<int> removeSlots = CachedInvisiblePlayers.ToList();
        foreach (var playerSlot in removeSlots)
        {
            SetPlayerInvisible(Utilities.GetPlayerFromSlot(playerSlot), false);
        }

        base.Disabled();
    }
}

public class GameModifierSingleCloak : GameModifierInvisibleBase
{
    public override string Name => "SingleCloak";
    public override string Description => "Each team has an invisible player";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierCloaked>(),
        GameModifiersUtils.GetModifierName<GameModifierRandomCloak>()
    ];

    public override void Enabled()
    {
        base.Enabled();

        List<CCSPlayerController> terroristPlayers = GameModifiersUtils.GetTerroristPlayers();
        if (terroristPlayers.Count > 0)
        {
            SetPlayerInvisible(terroristPlayers[Random.Shared.Next(terroristPlayers.Count)], true);
        }

        List<CCSPlayerController> counterTerroristPlayers = GameModifiersUtils.GetCounterTerroristPlayers();
        if (counterTerroristPlayers.Count > 0)
        {
            SetPlayerInvisible(counterTerroristPlayers[Random.Shared.Next(counterTerroristPlayers.Count)], true);
        }
    }

    public override void Disabled()
    {
        List<int> removeSlots = CachedInvisiblePlayers.ToList();
        foreach (var playerSlot in removeSlots)
        {
            SetPlayerInvisible(Utilities.GetPlayerFromSlot(playerSlot), false);
        }

        base.Disabled();
    }
}

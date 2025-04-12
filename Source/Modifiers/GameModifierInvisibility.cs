
using System;
using System.Collections.Generic;
using System.Linq;

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace GameModifiers.Modifiers;

public abstract class GameModifierInvisibleBase : GameModifierBase
{
    protected readonly List<int> CachedHiddenPlayers = new();

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
            Core.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Pre);
            Core.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.RegisterEventHandler<EventPlayerSpawned>(OnPlayerSpawned);
            Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        HidePlayers();
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
            Core.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Pre);
            Core.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.DeregisterEventHandler<EventPlayerSpawned>(OnPlayerSpawned);
            Core.RemoveListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }
        
        CachedHiddenPlayers.Clear();
        base.Disabled();
    }
    
    protected virtual void HidePlayers()
    {
        Utilities.GetPlayers().ForEach(player =>
        {
            if (!CachedHiddenPlayers.Contains(player.Slot) && CheckHidePlayer(player))
            {
                CachedHiddenPlayers.Add(player.Slot);
            }
        });
    }
    
    protected virtual bool CheckHidePlayer(CCSPlayerController player)
    {
        // Override in child class...
        return false;
    }
    
    protected virtual void UnHidePlayer(CCSPlayerController player)
    {
        if (CachedHiddenPlayers.Contains(player.Slot))
        {
            CachedHiddenPlayers.Remove(player.Slot);
        }
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false || player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        UnHidePlayer(player);
        return HookResult.Continue;
    }
    
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false || player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        if (!CachedHiddenPlayers.Contains(player.Slot) && CheckHidePlayer(player))
        {
            CachedHiddenPlayers.Add(player.Slot);
        }
        
        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawned(EventPlayerSpawned @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false || player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }
        
        if (!CachedHiddenPlayers.Contains(player.Slot) && CheckHidePlayer(player))
        {
            CachedHiddenPlayers.Add(player.Slot);
        }
        
        return HookResult.Continue;
    }
    
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        List<CCSPlayerController> players = Utilities.GetPlayers();
        if (!players.Any())
        {
            return;
        }

        foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
        {
            if (player == null || !player.IsValid)
            {
                continue;
            }
            
            IEnumerable<CCSPlayerController> hiddenPlayers = players
                .Where(p => p.IsValid && p.Pawn.IsValid && p.Slot != player.Slot && CachedHiddenPlayers.Contains(p.Slot));
            
            foreach (CCSPlayerController hiddenPlayer in hiddenPlayers)
            {
                info.TransmitEntities.Remove((int)hiddenPlayer.Pawn.Index);
            }
        }
    }

    private void OnClientDisconnect(int slot)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);
        if (player == null || !player.IsValid)
        {
            if (CachedHiddenPlayers.Contains(slot))
            {
                CachedHiddenPlayers.Remove(slot);
            }

            return;
        }
        
        UnHidePlayer(player);
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
    
    protected override bool CheckHidePlayer(CCSPlayerController player)
    {
        return true;
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
    
    protected override bool CheckHidePlayer(CCSPlayerController player)
    {
        if (Random.Shared.Next(0, 2) == 0)
        {
            return false;
        }
        
        return true;
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
    
    protected override void HidePlayers()
    {
        CachedHiddenPlayers.Clear();
        
        List<CCSPlayerController> terroristPlayers = GameModifiersUtils.GetTerroristPlayers();
        if (terroristPlayers.Any())
        {
            CachedHiddenPlayers.Add(terroristPlayers[Random.Shared.Next(terroristPlayers.Count)].Slot);
        }

        List<CCSPlayerController> counterTerroristPlayers = GameModifiersUtils.GetCounterTerroristPlayers();
        if (counterTerroristPlayers.Any())
        {
            CachedHiddenPlayers.Add(counterTerroristPlayers[Random.Shared.Next(counterTerroristPlayers.Count)].Slot);
        }
    }
}

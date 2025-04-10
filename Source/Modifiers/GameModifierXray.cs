
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace GameModifiers.Modifiers;

// Borrows a portion of code for the glow effect from aquevadis/bg-koka-cs2-xray-esp.

public abstract class GameModifierXrayBase : GameModifierBase
{
    protected readonly List<int> CachedXrayEnabledPlayers = new();
    private Dictionary<int, Tuple<int, int>> _glowingPlayerInstances = new();

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
            Core.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        SetupXray();
        
        Utilities.GetPlayers().ForEach(ApplyXrayToPlayer);
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
            Core.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            Core.RemoveListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }
        
        CachedXrayEnabledPlayers.Clear();
        Utilities.GetPlayers().ForEach(RemoveXrayFromPlayer);
        
        base.Disabled();
    }
    
    protected virtual void SetupXray()
    {
        Utilities.GetPlayers().ForEach(player =>
        {
            if (!CachedXrayEnabledPlayers.Contains(player.Slot) && CheckEnableXray(player))
            {
                CachedXrayEnabledPlayers.Add(player.Slot);
            }
        });
    }

    protected virtual bool CheckEnableXray(CCSPlayerController player)
    {
        // Override in child class...
        return false;
    }
    
    private void ApplyXrayToPlayer(CCSPlayerController? player)
    {
        RemoveXrayFromPlayer(player);

        Server.NextFrame(() =>
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive)
            {
                return;
            }

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid)
            {
                return;
            }

            GameModifiersUtils.ApplyEntityGlowEffect(playerPawn, out CDynamicProp? modelRelay, out CDynamicProp? modelGlow);
            if (modelRelay == null || modelGlow == null)
            {
                return;
            }

            switch (player.Team)
            {
                case CsTeam.Terrorist:
                    modelGlow.Glow.GlowColorOverride = Color.Orange;
                    break;
                case CsTeam.CounterTerrorist:
                    modelGlow.Glow.GlowColorOverride = Color.SkyBlue;
                    break;
            }
            
            _glowingPlayerInstances.Add(player.Slot, new Tuple<int, int>((int)modelRelay.Index, (int)modelGlow.Index));
        });
    }

    private void RemoveXrayFromPlayer(CCSPlayerController? player)
    {
        if (player == null)
        {
            return;
        }

        if (_glowingPlayerInstances.ContainsKey(player.Slot) == false)
        {
            return;
        }

        GameModifiersUtils.RemoveEntityGlowEffect(_glowingPlayerInstances[player.Slot].Item1, _glowingPlayerInstances[player.Slot].Item2);
        _glowingPlayerInstances.Remove(player.Slot);
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

            if (!CachedXrayEnabledPlayers.Contains(player.Slot))
            {
                foreach (var glowingInstances in _glowingPlayerInstances)
                {
                    info.TransmitEntities.Remove(glowingInstances.Value.Item1);
                    info.TransmitEntities.Remove(glowingInstances.Value.Item2);
                }
            }
        }
    }
    
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false)
        {
            return HookResult.Continue;
        }

        ApplyXrayToPlayer(player);
        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false)
        {
            return HookResult.Continue;
        }

        RemoveXrayFromPlayer(player);
        return HookResult.Continue;
    }

    private void OnClientDisconnect(int slot)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);
        if (player == null || player.IsValid is not true)
        {
            if (_glowingPlayerInstances.ContainsKey(slot))
            {
                _glowingPlayerInstances.Remove(slot);
            }

            return;
        }

        RemoveXrayFromPlayer(player);
    }
}

public class GameModifierXrayAll : GameModifierXrayBase
{
    public override string Name => "Xray";
    public override string Description => "Everyone can see each other through walls";
    public override bool SupportsRandomRounds { get; protected set; } = true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierCloaked>(),
        GameModifiersUtils.GetModifierName<GameModifierRandomCloak>(),
        GameModifiersUtils.GetModifierName<GameModifierSingleCloak>(),
        GameModifiersUtils.GetModifierName<GameModifierXrayRandom>(),
        GameModifiersUtils.GetModifierName<GameModifierXraySingle>()
    ];
    
    protected override bool CheckEnableXray(CCSPlayerController player)
    {
        return true;
    }
}

public class GameModifierXrayRandom : GameModifierXrayBase
{
    public override string Name => "RandomXray";
    public override string Description => "Some people can see each other through walls";
    public override bool SupportsRandomRounds { get; protected set; } = true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierCloaked>(),
        GameModifiersUtils.GetModifierName<GameModifierRandomCloak>(),
        GameModifiersUtils.GetModifierName<GameModifierSingleCloak>(),
        GameModifiersUtils.GetModifierName<GameModifierXrayAll>(),
        GameModifiersUtils.GetModifierName<GameModifierXraySingle>()
    ];
    
    protected override bool CheckEnableXray(CCSPlayerController player)
    {
        if (Random.Shared.Next(0, 2) == 0)
        {
            return false;
        }
        
        return true;
    }
}

public class GameModifierXraySingle : GameModifierXrayBase
{
    public override string Name => "SoloXray";
    public override string Description => "One person on each team has walls";
    public override bool SupportsRandomRounds { get; protected set; } = true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierCloaked>(),
        GameModifiersUtils.GetModifierName<GameModifierRandomCloak>(),
        GameModifiersUtils.GetModifierName<GameModifierSingleCloak>(),
        GameModifiersUtils.GetModifierName<GameModifierXrayAll>(),
        GameModifiersUtils.GetModifierName<GameModifierXrayRandom>()
    ];
    
    protected override void SetupXray()
    {
        CachedXrayEnabledPlayers.Clear();
        
        List<CCSPlayerController> terroristPlayers = GameModifiersUtils.GetTerroristPlayers();
        if (terroristPlayers.Any())
        {
            CachedXrayEnabledPlayers.Add(Random.Shared.Next(terroristPlayers.Count));
        }

        List<CCSPlayerController> counterTerroristPlayers = GameModifiersUtils.GetCounterTerroristPlayers();
        if (counterTerroristPlayers.Any())
        {
            CachedXrayEnabledPlayers.Add(Random.Shared.Next(counterTerroristPlayers.Count));
        }
    }
}

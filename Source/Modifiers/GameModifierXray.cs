
using System;
using System.Collections.Generic;
using System.Drawing;

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace GameModifiers.Modifiers;

// Borrows a large portion of code from aquevadis/bg-koka-cs2-xray-esp.
public class GameModifierXray : GameModifierBase
{
    public override string Name => "Xray";
    public override string Description => "Everyone can see each other through walls.";
    public override bool SupportsRandomRounds { get; protected set; } = true;

    private Dictionary<int, Tuple<int, int>> _glowingPlayerInstances = new();

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        Utilities.GetPlayers().ForEach(ApplyXrayToPlayer);
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            Core.RemoveListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        Utilities.GetPlayers().ForEach(RemoveXrayFromPlayer);

        base.Disabled();
    }

    private void ApplyXrayToPlayer(CCSPlayerController? player)
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

        RemoveXrayFromPlayer(player);

        _glowingPlayerInstances.Add(player.Slot, new Tuple<int, int>((int)modelRelay.Index, (int)modelGlow.Index));
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

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false || player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        ApplyXrayToPlayer(player);
        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false || player.Connected != PlayerConnectedState.PlayerConnected)
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

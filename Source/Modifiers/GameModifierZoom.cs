
using System;
using System.Collections.Generic;

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace GameModifiers.Modifiers;

public abstract class GameModifierZoomBase : GameModifierBase
{
    public virtual uint Fov { get; protected set; } = 90;

    protected readonly Dictionary<int, uint> CachedFov = new();

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterListener<Listeners.OnClientConnected>(OnClientConnected);
            Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        Utilities.GetPlayers().ForEach(ApplyZoom);
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.RemoveListener<Listeners.OnClientConnected>(OnClientConnected);
            Core.RemoveListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        Utilities.GetPlayers().ForEach(RemoveZoom);
        CachedFov.Clear();

        base.Disabled();
    }

    protected void ApplyZoom(CCSPlayerController? player)
    {
        if (player != null)
        {
            if (!CachedFov.ContainsKey(player.Slot))
            {
                CachedFov.Add(player.Slot, player.DesiredFOV);
            }

            player.DesiredFOV = Fov;

            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
            Console.WriteLine($"[GameModifierZoom::Start] Setting {player.PlayerName} FOV to {player.DesiredFOV}!");
        }
    }

    protected void RemoveZoom(CCSPlayerController? player)
    {
        if (player != null)
        {
            if (CachedFov.ContainsKey(player.Slot))
            {
                player.DesiredFOV = CachedFov[player.Slot];
                Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
                Console.WriteLine($"[GameModifierZoom::End] Setting {player.PlayerName} FOV back to {player.DesiredFOV}!");
            }
            else
            {
                Console.WriteLine($"[GameModifierZoom::End] WARNING: Failed to set players FOV back to original value!");
            }
        }
    }

    private void OnClientConnected(int slot)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);
        if (player == null || player.IsValid is not true)
        {
            return;
        }

        ApplyZoom(player);
    }

    private void OnClientDisconnect(int slot)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);
        if (player == null || player.IsValid is not true)
        {
            if (!CachedFov.ContainsKey(slot))
            {
                CachedFov.Remove(slot);
            }

            return;
        }

        RemoveZoom(player);
    }
}

public class GameModifierZoomIn : GameModifierZoomBase
{
    public override string Name => "ZoomIn";
    public override string Description => "Everyone's FOV is set to 30";
    public override bool SupportsRandomRounds => true;
    public override uint Fov { get; protected set; } = 30;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierZoomOut>()
    ];
}

public class GameModifierZoomOut : GameModifierZoomBase
{
    public override string Name => "ZoomOut";
    public override string Description => "Everyone's FOV is set to 150";
    public override bool SupportsRandomRounds => true;
    public override uint Fov { get; protected set; } = 150;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierZoomIn>()
    ];
}

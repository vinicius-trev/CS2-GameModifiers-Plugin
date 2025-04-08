
using System.Collections.Generic;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace GameModifiers.Modifiers;

public abstract class GameModifierScalePlayer : GameModifierBase
{
    public virtual float Scale { get; protected set; } = 1.0f;

    protected readonly Dictionary<int, float> CachedOriginalScale = new();

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        Utilities.GetPlayers().ForEach(ApplyPlayerScale);
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.RemoveListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        Utilities.GetPlayers().ForEach(ResetPlayerScale);

        base.Disabled();
    }

    protected void ApplyPlayerScale(CCSPlayerController? player)
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

        var playerSceneNode = playerPawn.CBodyComponent?.SceneNode;
        if (playerSceneNode == null)
        {
            return;
        }

        if (!CachedOriginalScale.ContainsKey(player.Slot))
        {
            CachedOriginalScale.Add(player.Slot, playerSceneNode.Scale);
        }

        playerSceneNode.Scale = Scale;
        playerPawn.AcceptInput("SetScale", null, null, Scale.ToString());
        Server.NextFrame(() =>
        {
            Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_CBodyComponent");
        });
    }

    protected void ResetPlayerScale(CCSPlayerController? player)
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

        var playerSceneNode = playerPawn.CBodyComponent?.SceneNode;
        if (playerSceneNode == null)
        {
            return;
        }

        if (CachedOriginalScale.ContainsKey(player.Slot))
        {
            float originalScale = CachedOriginalScale[player.Slot];
            playerSceneNode.Scale = originalScale;
            playerPawn.AcceptInput("SetScale", null, null, originalScale.ToString());
            Server.NextFrame(() =>
            {
                Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_CBodyComponent");
            });
        }
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false || player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        ApplyPlayerScale(player);
        return HookResult.Continue;
    }

    private void OnClientDisconnect(int slot)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);
        if (player == null || player.IsValid is not true)
        {
            if (CachedOriginalScale.ContainsKey(slot))
            {
                CachedOriginalScale.Remove(slot);
            }

            return;
        }

        ResetPlayerScale(player);
    }
}

public class GameModifierSmallPlayers : GameModifierScalePlayer
{
    public override string Name => "SmallPlayers";
    public override string Description => "Everyone is 2X smaller";
    public override bool SupportsRandomRounds => true;
    public override float Scale => 0.5f;
}

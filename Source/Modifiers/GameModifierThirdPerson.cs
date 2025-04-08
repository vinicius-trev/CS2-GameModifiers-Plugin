using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using GameModifiers.ThirdParty;

namespace GameModifiers.Modifiers;

public class GameModifierThirdPerson : GameModifierBase
{
    public override string Name => "ThirdPerson";
    public override string Description => "Everyone is in third person view";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierZoomIn>(),
        GameModifiersUtils.GetModifierName<GameModifierZoomOut>()
    ];
    private readonly Dictionary<int, CPhysicsPropMultiplayer> _thirdPersonAttachPointInstances = new();

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
            Core.RegisterListener<Listeners.OnTick>(OnTick);
        }

        Utilities.GetPlayers().ForEach(ApplyThirdPersonToPlayer);
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            Core.RemoveListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
            Core.RemoveListener<Listeners.OnTick>(OnTick);
        }

        Utilities.GetPlayers().ForEach(ApplyFirstPersonToPlayer);

        base.Disabled();
    }

    private void ApplyThirdPersonToPlayer(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
        {
            return;
        }

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
        {
            return;
        }

        CPhysicsPropMultiplayer? thirdPersonAttachPoint = Utilities.CreateEntityByName<CPhysicsPropMultiplayer>("prop_physics_multiplayer");
        if (thirdPersonAttachPoint == null || !thirdPersonAttachPoint.IsValid)
        {
            return;
        }

        thirdPersonAttachPoint.DispatchSpawn();

        thirdPersonAttachPoint.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_NEVER;
        thirdPersonAttachPoint.Collision.SolidFlags = 12;
        thirdPersonAttachPoint.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
        thirdPersonAttachPoint.Render = Color.Transparent;

        playerPawn.CameraServices!.ViewEntity.Raw = thirdPersonAttachPoint.EntityHandle.Raw;
        Utilities.SetStateChanged(playerPawn, "CBasePlayerPawn", "m_pCameraServices");

        thirdPersonAttachPoint.Teleport(player.CalculatePositionInFront(-110, 90),
            playerPawn.V_angle, new Vector());

        _thirdPersonAttachPointInstances.Add(player.Slot, thirdPersonAttachPoint);
    }

    private void ApplyFirstPersonToPlayer(CCSPlayerController? player)
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

        playerPawn.CameraServices!.ViewEntity.Raw = uint.MaxValue;
        Utilities.SetStateChanged(playerPawn, "CBasePlayerPawn", "m_pCameraServices");

        if (_thirdPersonAttachPointInstances.ContainsKey(player.Slot))
        {
            _thirdPersonAttachPointInstances[player.Slot].Remove();
            _thirdPersonAttachPointInstances.Remove(player.Slot);
        }
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false || player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        ApplyFirstPersonToPlayer(player);
        return HookResult.Continue;
    }

    private void OnClientDisconnect(int slot)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);
        if (player == null || !player.IsValid)
        {
            if (_thirdPersonAttachPointInstances.ContainsKey(slot))
            {
                _thirdPersonAttachPointInstances[slot].Remove();
                _thirdPersonAttachPointInstances.Remove(slot);
            }

            return;
        }

        ApplyFirstPersonToPlayer(player);
    }

    private void OnTick()
    {
        foreach (var attachPointPair in _thirdPersonAttachPointInstances)
        {
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(attachPointPair.Key);
            if (player == null || !player.IsValid || !player.PawnIsAlive)
            {
                continue;
            }

            attachPointPair.Value.UpdateCameraSmooth(player);
        }
    }
}


using System;
using System.Collections.Generic;

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace GameModifiers.Modifiers;

public abstract class GameModifierMissedShot : GameModifierBase
{
    protected readonly Dictionary<int, int> CachedHitBullets = new();

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Pre);
            Core.RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Pre);
            Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Pre);
            Core.DeregisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Pre);
            Core.RemoveListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        CachedHitBullets.Clear();

        base.Disabled();
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        CCSPlayerController? attackingPlayer = @event.Attacker;
        CCSPlayerController? damagedPlayer = @event.Userid;
        if (attackingPlayer == null || !attackingPlayer.IsValid || damagedPlayer == null || !damagedPlayer.IsValid || !damagedPlayer.PawnIsAlive)
        {
            return HookResult.Continue;
        }
        
        if (ShouldCountMissedShots(attackingPlayer) == false)
        {
            return HookResult.Continue;
        }

        var attackingPawn = attackingPlayer.PlayerPawn.Value;
        var damagedPawn = damagedPlayer.PlayerPawn.Value;
        if (attackingPawn == null || !attackingPawn.IsValid || damagedPawn == null || !damagedPawn.IsValid)
        {
            return HookResult.Continue;
        }

        if (CachedHitBullets.ContainsKey(attackingPlayer.Slot))
        {
            CachedHitBullets[attackingPlayer.Slot] += 1;
        }
        else
        {
            CachedHitBullets.Add(attackingPlayer.Slot, 1);
        }

        return HookResult.Continue;
    }

    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false || !player.PawnIsAlive)
        {
            return HookResult.Continue;
        }
        
        if (ShouldCountMissedShots(player) == false)
        {
            return HookResult.Continue;
        }
        
        int lastHitBullets = 0;
        if (CachedHitBullets.ContainsKey(player.Slot))
        {
            lastHitBullets = CachedHitBullets[player.Slot];
        }

        Server.NextFrame(() =>
        {
            int hitBullets = 0;
            if (CachedHitBullets.ContainsKey(player.Slot))
            {
                hitBullets = CachedHitBullets[player.Slot];
            }

            if (hitBullets <= lastHitBullets)
            {
                OnMissedShot(player);
            }
        });

        return HookResult.Continue;
    }

    private bool ShouldCountMissedShots(CCSPlayerController? player)
    {
        switch (GameModifiersUtils.GetActiveWeaponType(player))
        {
            case CSWeaponType.WEAPONTYPE_KNIFE:
            case CSWeaponType.WEAPONTYPE_PISTOL:
            case CSWeaponType.WEAPONTYPE_SUBMACHINEGUN:
            case CSWeaponType.WEAPONTYPE_RIFLE:
            case CSWeaponType.WEAPONTYPE_SHOTGUN:
            case CSWeaponType.WEAPONTYPE_SNIPER_RIFLE:
            case CSWeaponType.WEAPONTYPE_MACHINEGUN:
            case CSWeaponType.WEAPONTYPE_TASER:
                return true;
            default: break;
        }

        return false;
    }

    private void OnClientDisconnect(int slot)
    {
        if (CachedHitBullets.ContainsKey(slot))
        {
            CachedHitBullets.Remove(slot);
        }
    }

    protected virtual void OnMissedShot(CCSPlayerController? player)
    {
        // Implement in child class...
    }
}

public class GameModifierDropOnMiss : GameModifierMissedShot
{
    public override string Name => "DropOnMiss";
    public override string Description => "Weapons are dropped on missed shots";
    public override bool SupportsRandomRounds => true;

    protected override void OnMissedShot(CCSPlayerController? player)
    {
        if (player == null || player.IsValid == false || !player.PawnIsAlive)
        {
            return;
        }

        player.DropActiveWeapon();
    }
}

public class GameModifierDontMiss : GameModifierMissedShot
{
    public override string Name => "DontMiss";
    public override string Description => "You take the damage from your missed shots";
    public override bool SupportsRandomRounds => true;

    protected override void OnMissedShot(CCSPlayerController? player)
    {
        if (player == null || player.IsValid == false || !player.PawnIsAlive)
        {
            return;
        }

        int damage = (int)GameModifiersUtils.GetWeaponDamage(GameModifiersUtils.GetActiveWeapon(player));
        if (damage <= 0.0f)
        {
            return;
        }

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
        {
            return;
        }

        int newHealth = playerPawn.Health - damage < 0 ? 0 : playerPawn.Health - damage;
        GameModifiersUtils.SetPlayerHealth(playerPawn, newHealth);
        if (newHealth <= 0)
        {
            playerPawn.CommitSuicide(false, true);
        }
    }
}

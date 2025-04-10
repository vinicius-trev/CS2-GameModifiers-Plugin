
using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace GameModifiers.Modifiers;

public abstract class GameModifierRemoveWeapons : GameModifierBase
{
    protected readonly Dictionary<int, List<string>> CachedItems = new();

    public override void Enabled()
    {
        base.Enabled();

        // If this has managed to call again, not much more we can do than just stop
        // keeping track of what we originally had in the list.
        CachedItems.Clear();
        
        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        Utilities.GetPlayers().ForEach(RemoveWeapons);

        GameModifiersUtils.PrintTitleToChatAll("Removing items, they will be returned when the modifier is disabled.");
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.RemoveListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        foreach (var cachedWeaponPair in CachedItems)
        {
            TryReturnWeapons(Utilities.GetPlayerFromSlot(cachedWeaponPair.Key));
        }
        
        GameModifiersUtils.PrintTitleToChatAll("Returning items...");

        base.Disabled();
    }

    private void RemoveWeapons(CCSPlayerController? player)
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
        
        List<CHandle<CBasePlayerWeapon>>? weaponHandles = playerPawn.WeaponServices?.MyWeapons.ToList();
        if (weaponHandles == null)
        {
            return;
        }

        List<string> cachedWeapons = new List<string>();
        foreach (CHandle<CBasePlayerWeapon> weaponHandle in weaponHandles)
        {
            if (weaponHandle.IsValid && weaponHandle.Value != null)
            {
                switch (GameModifiersUtils.GetWeaponType(weaponHandle.Value))
                {
                    case CSWeaponType.WEAPONTYPE_PISTOL:
                    case CSWeaponType.WEAPONTYPE_SUBMACHINEGUN:
                    case CSWeaponType.WEAPONTYPE_RIFLE:
                    case CSWeaponType.WEAPONTYPE_SHOTGUN:
                    case CSWeaponType.WEAPONTYPE_SNIPER_RIFLE:
                    case CSWeaponType.WEAPONTYPE_MACHINEGUN:
                    case CSWeaponType.WEAPONTYPE_TASER:
                    case CSWeaponType.WEAPONTYPE_GRENADE:
                    {
                        cachedWeapons.Add(weaponHandle.Value.DesignerName);
                    } 
                    break;
                    default: break;
                }
            }
        }

        CachedItems.Add(player.Slot, cachedWeapons);

        GameModifiersUtils.RemoveWeapons(player);
    }

    private void TryReturnWeapons(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid  || !player.PawnIsAlive)
        {
            return;
        }
        
        if (CachedItems.ContainsKey(player.Slot))
        {
            GameModifiersUtils.RemoveWeapons(player);
            
            foreach (var itemName in CachedItems[player.Slot])
            {
                player.GiveNamedItem(itemName);
            }

            CachedItems.Remove(player.Slot);
        }
    }
    
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        // While active just remove any weapons on spawn.
        if (IsActive)
        {
            RemoveWeapons(@event.Userid);
            return HookResult.Continue;
        }
        
        Server.NextFrame(() =>
        {
            // While inactive and still bound, there must be dead players waiting for items to be returned.
            TryReturnWeapons(@event.Userid);
        
            // Check if we can deregister this even if we are no longer active and have no tracked items.
            if (!CachedItems.Any() && Core != null)
            {
                Core.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            }
        });
        
        return HookResult.Continue;
    }
    
    private void OnClientDisconnect(int slot)
    {
        if (CachedItems.ContainsKey(slot))
        {
            CachedItems.Remove(slot);
        }
    }
}

public class GameModifierKnifeOnly : GameModifierRemoveWeapons
{
    public override string Name => "KnivesOnly";
    public override string Description => "Buy menu is disabled, knives only";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierRandomWeapon>(),
        GameModifiersUtils.GetModifierName<GameModifierGrenadesOnly>(),
        GameModifiersUtils.GetModifierName<GameModifierRandomWeapons>()
    ];
}

public class GameModifierRandomWeapon : GameModifierRemoveWeapons
{
    public override string Name => "RandomWeapon";
    public override string Description => "Buy menu is disabled, random weapon only";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierKnifeOnly>(),
        GameModifiersUtils.GetModifierName<GameModifierGrenadesOnly>(),
        GameModifiersUtils.GetModifierName<GameModifierRandomWeapons>()
    ];
    
    public override void Enabled()
    {
        base.Enabled();

        ApplyRandomWeapon();
    }

    protected virtual void ApplyRandomWeapon()
    {
        string randomWeaponName = GameModifiersUtils.GetRandomRangedWeaponName();
        GameModifiersUtils.PrintTitleToChatAll($"{randomWeaponName.Substring(7)} round.");

        Utilities.GetPlayers().ForEach(player =>
        {
            GameModifiersUtils.GiveAndEquipWeapon(player, randomWeaponName);
        });
    }
}

public class GameModifierRandomWeapons : GameModifierRandomWeapon
{
    public override string Name => "RandomWeapons";
    public override string Description => "Buy menu is disabled, random weapons are given out";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierKnifeOnly>(),
        GameModifiersUtils.GetModifierName<GameModifierGrenadesOnly>(),
        GameModifiersUtils.GetModifierName<GameModifierRandomWeapon>()
    ];

    protected override void ApplyRandomWeapon()
    {
        Utilities.GetPlayers().ForEach(player =>
        {
            string randomWeaponName = GameModifiersUtils.GetRandomRangedWeaponName();
            GameModifiersUtils.PrintTitleToChat(player, $"{randomWeaponName.Substring(7)} for random weapon round.");
            GameModifiersUtils.GiveAndEquipWeapon(player, randomWeaponName);
        });
    }
}

public class GameModifierGrenadesOnly : GameModifierRemoveWeapons
{
    public override string Name => "GrenadesOnly";
    public override string Description => "Buy menu is disabled, grenades only";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierRandomWeapon>(),
        GameModifiersUtils.GetModifierName<GameModifierKnifeOnly>(),
        GameModifiersUtils.GetModifierName<GameModifierRandomWeapons>()
    ];

    public override void Enabled()
    {
        base.Enabled();

        Utilities.GetPlayers().ForEach(player =>
        {
            GameModifiersUtils.GiveAndEquipWeapon(player, "weapon_molotov");
            GameModifiersUtils.GiveAndEquipWeapon(player, "weapon_smokegrenade");
            GameModifiersUtils.GiveAndEquipWeapon(player, "weapon_hegrenade");
            GameModifiersUtils.GiveAndEquipWeapon(player, "weapon_flashbang");
        });
    }
}

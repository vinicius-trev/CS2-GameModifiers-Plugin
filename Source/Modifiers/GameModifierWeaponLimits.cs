
using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace GameModifiers.Modifiers;

public abstract class GameModifierRemoveWeapons : GameModifierBase
{
    protected readonly Dictionary<int, List<string>> CachedItems = new();

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerSpawned>(OnPlayerSpawned);
        }

        Utilities.GetPlayers().ForEach(player =>
        {
            List<string> weaponNames = GameModifiersUtils.GetWeapons(player)
                .Where(weapon => weapon != null && weapon.IsValid)
                .Select(weapon => weapon!.DesignerName)
                .ToList();

            CachedItems.Add(player.Slot, weaponNames);

            GameModifiersUtils.RemoveWeapons(player);
        });

        GameModifiersUtils.PrintTitleToChatAll("Removing items, they will be returned when the modifier is disabled.");
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerSpawned>(OnPlayerSpawned);
        }

        Utilities.GetPlayers().ForEach(GameModifiersUtils.RemoveWeapons);

        foreach (var cachedWeaponPair in CachedItems)
        {
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(cachedWeaponPair.Key);
            if (player == null || !player.IsValid)
            {
                continue;
            }

            foreach (var itemName in cachedWeaponPair.Value)
            {
                player.GiveNamedItem(itemName);
            }
        }

        CachedItems.Clear();
        GameModifiersUtils.PrintTitleToChatAll("Returning items...");

        base.Disabled();
    }

    private HookResult OnPlayerSpawned(EventPlayerSpawned @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        player.RemoveWeapons();
        return HookResult.Continue;
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

    protected static List<string> RandomWeapons =
    [
        "weapon_scar20",
        "weapon_sg553",
        "weapon_revolver",
        "weapon_m249",
        "weapon_mac10",
        "weapon_ak47",
        "weapon_deagle",
        "weapon_m4a1",
        "weapon_m4a4",
        "weapon_tec9",
        "weapon_xm1014",
        "weapon_p250",
        "weapon_famas",
        "weapon_aug",
        "weapon_mp5sd",
        "weapon_mag7",
        "weapon_bizon",
        "weapon_ssg08",
        "weapon_ump45",
        "weapon_mp9",
        "weapon_p90",
        "weapon_hkp2000",
        "weapon_glock",
        "weapon_awp",
        "weapon_sawedoff",
        "weapon_taser",
        "weapon_mp7",
        "weapon_sg556",
        "weapon_nova",
        "weapon_m4a1_silencer",
        "weapon_fiveseven",
        "weapon_cz75a",
        "weapon_usp_silencer",
        "weapon_g3sg1",
        "weapon_negev"
    ];

    public override void Enabled()
    {
        base.Enabled();

        ApplyRandomWeapon();
    }

    protected virtual void ApplyRandomWeapon()
    {
        string randomWeaponName = RandomWeapons[Random.Shared.Next(RandomWeapons.Count)];
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
            string randomWeaponName = RandomWeapons[Random.Shared.Next(RandomWeapons.Count)];
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

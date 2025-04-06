
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

using GameModifiers.Modifiers;
using GameModifiers.Types;
using Microsoft.VisualBasic.CompilerServices;

namespace GameModifiers;

internal static class GameModifiersUtils
{
    public static List<CCSPlayerController> GetPlayerFromName(string name)
    {
        return Utilities.GetPlayers().FindAll(x => x.PlayerName.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    public static void ShowMessageCentreAll(string message)
    {
        Utilities.GetPlayers().ForEach(controller =>
        {
            controller.PrintToCenter(message);
        });
    }

    public static void PrintTitleToChat(CCSPlayerController? player, string message)
    {
        if (player == null)
        {
            return;
        }

        player.PrintToChat($"[{ChatColors.Red}GameModifiers{ChatColors.Default}] {message}");
    }

    public static void PrintModifiersToChat(CCSPlayerController? player, List<GameModifierBase> modifiers, string message, bool withDescriptions = true)
    {
        if (player == null)
        {
            return;
        }

        PrintTitleToChat(player, message);

        if (modifiers.Count <= 0)
        {
            player.PrintToChat($"None");
            return;
        }

        foreach (var modifier in modifiers)
        {
            string description = withDescriptions ? $" - {ChatColors.Grey}[{modifier.Description}]" : "";
            player.PrintToChat($"• {modifier.Name}{description}");
        }
    }

    public static void PrintTitleToChatAll(string message)
    {
        Utilities.GetPlayers().ForEach(controller =>
        {
            PrintTitleToChat(controller, message);
        });
    }

    public static void PrintToChatAll(string message)
    {
        Utilities.GetPlayers().ForEach(controller =>
        {
            controller.PrintToChat(message);
        });
    }

    public static void ExecuteCommandFromServerOnAllClients(string command)
    {
        Utilities.GetPlayers().ForEach(controller =>
        {
            controller.ExecuteClientCommandFromServer(command);
        });
    }

    public static List<Type> GetAllChildClasses<T>()
    {
        var assembly = Assembly.GetAssembly(typeof(T));
        if (assembly == null)
        {
            return new List<Type>();
        }

        var types = assembly.GetTypes();
        return types.Where(type => type.IsSubclassOf(typeof(T)) && !type.IsAbstract).ToList();
    }

    public static string GetConfigPath(string modulePath)
    {
        DirectoryInfo? moduleDirectory = new FileInfo(modulePath).Directory;
        DirectoryInfo? csSharpDirectory = moduleDirectory?.Parent?.Parent;
        if (csSharpDirectory == null)
        {
            return "";
        }

        return Path.Combine(csSharpDirectory.FullName, "configs", "plugins", moduleDirectory!.Name);
    }

    public static string GetPluginPath(string modulePath)
    {
        DirectoryInfo? moduleDirectory = new FileInfo(modulePath).Directory;
        if (moduleDirectory == null)
        {
            return "";
        }

        return moduleDirectory.FullName;
    }

    public static string GetModifierName<T>() where T : GameModifierBase, new()
    {
        return new T().Name;
    }

    public static string GetConVarStringValue(ConVar? conVar)
    {
        if (conVar == null)
        {
            return "";
        }

        switch (conVar.Type)
        {
            case ConVarType.Bool:
                return conVar.GetPrimitiveValue<bool>().ToString(CultureInfo.InvariantCulture);
            case ConVarType.Float32:
                return conVar.GetPrimitiveValue<float>().ToString(CultureInfo.InvariantCulture);
            case ConVarType.Float64:
                return conVar.GetPrimitiveValue<double>().ToString(CultureInfo.InvariantCulture);
            case ConVarType.UInt16:
                return conVar.GetPrimitiveValue<ushort>().ToString(CultureInfo.InvariantCulture);
            case ConVarType.Int16:
                return conVar.GetPrimitiveValue<short>().ToString(CultureInfo.InvariantCulture);
            case ConVarType.UInt32:
                return conVar.GetPrimitiveValue<uint>().ToString(CultureInfo.InvariantCulture);
            case ConVarType.Int32:
                return conVar.GetPrimitiveValue<int>().ToString(CultureInfo.InvariantCulture);
            case ConVarType.Int64:
                return conVar.GetPrimitiveValue<long>().ToString(CultureInfo.InvariantCulture);
            case ConVarType.UInt64:
                return conVar.GetPrimitiveValue<ulong>().ToString(CultureInfo.InvariantCulture);
            case ConVarType.String:
                return conVar.StringValue;
            case ConVarType.Qangle:
                return conVar.GetNativeValue<QAngle>().ToString();
            case ConVarType.Vector2:
            {
                Vector2D vector2D = conVar.GetNativeValue<Vector2D>();
                return $"{vector2D.X:n2} {vector2D.Y:n2}";
            }
            case ConVarType.Vector3:
                return conVar.GetNativeValue<Vector>().ToString();
            case ConVarType.Vector4:
            case ConVarType.Color:
            {
                Vector4D vector4D = conVar.GetNativeValue<Vector4D>();
                return $"{vector4D.X:n2} {vector4D.Y:n2} {vector4D.Z:n2} {vector4D.W:n2}";
            }
        }

        return "";
    }

    public static bool ApplyEntityGlowEffect(CBaseEntity? entity, out CDynamicProp? modelRelay, out CDynamicProp? modelGlow)
    {
        if (entity == null)
        {
            modelRelay = null;
            modelGlow = null;
            return false;
        }

        modelRelay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        modelGlow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (modelRelay == null || !modelRelay.IsValid || modelGlow == null || !modelGlow.IsValid)
        {
            return false;
        }

        string modelName = entity.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName;
        Console.WriteLine($"Adding glow for: {entity.Globalname} using model: {modelName}");

        modelRelay.Spawnflags = 256u;
        modelRelay.RenderMode = RenderMode_t.kRenderNone;

        modelRelay.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
        modelRelay.SetModel(modelName);
        modelRelay.DispatchSpawn();
        modelRelay.AcceptInput("FollowEntity", entity, modelRelay, "!activator");

        modelGlow.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
        modelGlow.SetModel(modelName);
        modelGlow.DispatchSpawn();
        modelGlow.AcceptInput("FollowEntity", modelRelay, modelGlow, "!activator");

        modelGlow.Render = Color.Black;
        modelGlow.Spawnflags = 256u;
        modelGlow.RenderMode = RenderMode_t.kRenderGlow;
        modelGlow.Glow.GlowRange = 5000;
        modelGlow.Glow.GlowTeam = -1;
        modelGlow.Glow.GlowType = 3;
        modelGlow.Glow.GlowRangeMin = 20;

        return true;
    }

    public static bool RemoveEntityGlowEffect(int relayIndex, int glowIndex)
    {
        CDynamicProp? modelRelay = Utilities.GetEntityFromIndex<CDynamicProp>(relayIndex);
        if (modelRelay != null && modelRelay.IsValid)
        {
            modelRelay.Remove();
        }

        CDynamicProp? modelGlow = Utilities.GetEntityFromIndex<CDynamicProp>(glowIndex);
        if (modelGlow != null && modelGlow.IsValid)
        {
            modelGlow.Remove();
        }

        return true;
    }

    public static bool SetPlayerMaxHealth(CCSPlayerPawn? playerPawn, int health)
    {
        if (playerPawn == null || !playerPawn.IsValid)
        {
            return false;
        }

        playerPawn.MaxHealth = playerPawn.Health = health;
        Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iMaxHealth");
        return true;
    }

    public static bool SetPlayerHealth(CCSPlayerPawn? playerPawn, int health)
    {
        if (playerPawn == null || !playerPawn.IsValid)
        {
            return false;
        }

        playerPawn.Health = health;
        Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
        return true;
    }

    public static Vector? GetRandomLocation()
    {
        return NavMesh.GetRandomPosition();
    }

    public static Vector? GetSpawnLocation(CsTeam team)
    {
        List<SpawnPoint> spawnPoints;
        if (team == CsTeam.Terrorist)
        {
            spawnPoints = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist").ToList();
        }
        else if (team == CsTeam.CounterTerrorist)
        {
            spawnPoints = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist").ToList();
        }
        else return null;

        Random random = new Random();
        return spawnPoints[random.Next(0, spawnPoints.Count)].AbsOrigin;
    }

    public static bool SwapPlayerLocations(CCSPlayerController? firstPlayer, CCSPlayerController? secondPlayer)
    {
        if (firstPlayer == secondPlayer)
        {
            // No point swapping our-self.
            return false;
        }

        if (firstPlayer == null || !firstPlayer.IsValid || secondPlayer == null || !secondPlayer.IsValid)
        {
            return false;
        }

        CCSPlayerPawn? firstPawn = firstPlayer.PlayerPawn.Value;
        CCSPlayerPawn? secondPawn = secondPlayer.PlayerPawn.Value;
        if (firstPawn == null || !firstPawn.IsValid || secondPawn == null || !secondPawn.IsValid)
        {
            return false;
        }

        if (firstPawn.AbsOrigin == null || secondPawn.AbsOrigin == null)
        {
            return false;
        }

        Vector? firstPos = firstPawn.AbsOrigin != null ? new Vector
        {
            X = firstPawn.AbsOrigin.X,
            Y = firstPawn.AbsOrigin.Y,
            Z = firstPawn.AbsOrigin.Z
        } : null;

        TeleportPlayer(firstPlayer, secondPawn.AbsOrigin);
        TeleportPlayer(secondPlayer, firstPos);
        return true;
    }

    public static float GetPlayerSpeedMultiplier(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
        {
            return 1.0f;
        }

        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
        {
            return 1.0f;
        }

        return pawn.VelocityModifier;
    }

    public static bool SetPlayerSpeedMultiplier(CCSPlayerController? player, float speedMultiplier)
    {
        if (player == null || !player.IsValid)
        {
            return false;
        }

        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
        {
            return false;
        }

        pawn.VelocityModifier = speedMultiplier;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        return true;
    }

    public static bool TeleportPlayerToRandomSpot(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
        {
            return false;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
        {
            return false;
        }

        Vector? randomLocation = GetRandomLocation();
        if (randomLocation == null)
        {
            Console.WriteLine($"[GameModifiersUtils::TeleportPlayerToRandomSpot] WARNING: Failed to find random location for {player.PlayerName}!");
            return false;
        }

        TeleportPlayer(player, randomLocation);
        return true;
    }

    public static bool TeleportPlayerToSpawnArea(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
        {
            return false;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
        {
            return false;
        }

        Vector? spawnLocation = GetSpawnLocation(player.Team);
        if (spawnLocation == null)
        {
            Console.WriteLine($"[GameModifiersUtils::TeleportPlayerToSpawnArea] WARNING: Failed to find spawn point for {player.PlayerName}!");
            return false;
        }

        TeleportPlayer(player, spawnLocation);
        return true;
    }

    public static void TeleportPlayer(CCSPlayerController? player, Vector? position, QAngle? angles = null, Vector? velocity = null)
    {
        if (player == null || !player.IsValid)
        {
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
        {
            return;
        }

        pawn.Teleport(position, angles, velocity);

        pawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
        pawn.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
        Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
        Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");

        Server.NextFrame(() =>
        {
            if (!pawn.IsValid || pawn.LifeState != (int)LifeState_t.LIFE_ALIVE)
            {
                return;
            }

            pawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
            pawn.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;

            Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
        });
    }

    public static CBasePlayerWeapon? GetActiveWeapon(CCSPlayerController? player)
    {
        return player?.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
    }

    public static CBasePlayerWeapon? GetWeapon(CCSPlayerController? player, string weaponName)
    {
        return player?.PlayerPawn.Value?.WeaponServices?.MyWeapons.FirstOrDefault(weapon => weapon.Value!.DesignerName.Contains(weaponName))?.Value;
    }

    public static List<CBasePlayerWeapon?> GetWeapons(CCSPlayerController? player)
    {
        List<CHandle<CBasePlayerWeapon>>? weaponHandles = player?.PlayerPawn.Value?.WeaponServices?.MyWeapons.ToList();
        if (weaponHandles == null)
        {
            return new List<CBasePlayerWeapon?>();
        }

        List<CBasePlayerWeapon?> outWeapons = new List<CBasePlayerWeapon?>();
        foreach (CHandle<CBasePlayerWeapon> weaponHandle in weaponHandles)
        {
            if (weaponHandle.IsValid)
            {
                outWeapons.Add(weaponHandle.Value);
            }
        }

        return outWeapons;
    }

    public static CBasePlayerWeapon? GiveAndEquipWeapon(CCSPlayerController? player, string weaponName)
    {
        if (player == null || !player.IsValid)
        {
            return null;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
        {
            return null;
        }

        if (pawn.WeaponServices == null)
        {
            return null;
        }

        CBasePlayerWeapon? weapon = player.GiveNamedItem<CBasePlayerWeapon>(weaponName);
        if (weapon == null || !weapon.IsValid)
        {
            return null;
        }

        pawn.WeaponServices.ActiveWeapon.Raw = weapon.EntityHandle;
        return weapon;
    }

    public static void RemoveWeapons(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
        {
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
        {
            return;
        }

        if (pawn.WeaponServices == null)
        {
            return;
        }

        List<CBasePlayerWeapon?> weapons = GetWeapons(player);
        if (weapons.Count <= 0)
        {
            return;
        }

        foreach (var weapon in weapons)
        {
            if (weapon == null || !weapon.IsValid)
            {
                continue;
            }

            switch (GetWeaponType(weapon))
            {
                case CSWeaponType.WEAPONTYPE_PISTOL:
                case CSWeaponType.WEAPONTYPE_SUBMACHINEGUN:
                case CSWeaponType.WEAPONTYPE_RIFLE:
                case CSWeaponType.WEAPONTYPE_SHOTGUN:
                case CSWeaponType.WEAPONTYPE_SNIPER_RIFLE:
                case CSWeaponType.WEAPONTYPE_MACHINEGUN:
                case CSWeaponType.WEAPONTYPE_TASER:
                case CSWeaponType.WEAPONTYPE_GRENADE:
                case CSWeaponType.WEAPONTYPE_EQUIPMENT:
                {
                    pawn.WeaponServices.ActiveWeapon.Raw = weapon.EntityHandle;
                    player.DropActiveWeapon();
                    weapon.AcceptInput("kill");
                }
                break;
                default: break;
            }
        }

        foreach (var weapon in pawn.WeaponServices.MyWeapons)
        {
            pawn.WeaponServices.ActiveWeapon.Raw = weapon.Raw;
            break;
        }
    }

    public static float GetWeaponDamage(CBasePlayerWeapon? weapon)
    {
        if (weapon == null || !weapon.IsValid)
        {
            return 0.0f;
        }

        CCSWeaponBaseVData? weaponVData = weapon.As<CCSWeaponBase>().VData;
        if (weaponVData == null)
        {
            return 0.0f;
        }

        return weaponVData.Damage;
    }

    public static void ResetWeaponAmmo(CBasePlayerWeapon? weapon)
    {
        if (weapon == null || !weapon.IsValid)
        {
            return;
        }

        CCSWeaponBaseVData? weaponVData = weapon.As<CCSWeaponBase>().VData;
        if (weaponVData == null)
        {
            return;
        }

        // Return on anything other than a weapon.
        if (IsRangedWeapon(weapon) == false)
        {
            return;
        }

        Server.NextFrame(() =>
        {
            weapon.Clip1 = weaponVData.MaxClip1;
            weapon.Clip2 = weaponVData.SecondaryReserveAmmoMax;
            weapon.ReserveAmmo[0] = weaponVData.PrimaryReserveAmmoMax;

            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip2");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
        });
    }

    public static bool IsRangedWeapon(CBasePlayerWeapon? weapon)
    {
        if (weapon == null || !weapon.IsValid)
        {
            return false;
        }

        CCSWeaponBaseVData? weaponVData = weapon.As<CCSWeaponBase>().VData;
        if (weaponVData == null)
        {
            return false;
        }

        switch (weaponVData.WeaponType)
        {
            case CSWeaponType.WEAPONTYPE_PISTOL:
            case CSWeaponType.WEAPONTYPE_SUBMACHINEGUN:
            case CSWeaponType.WEAPONTYPE_RIFLE:
            case CSWeaponType.WEAPONTYPE_SHOTGUN:
            case CSWeaponType.WEAPONTYPE_SNIPER_RIFLE:
            case CSWeaponType.WEAPONTYPE_MACHINEGUN:
                return true;
            default: break;
        }

        return false;
    }

    public static CSWeaponType GetWeaponType(CBasePlayerWeapon? weapon)
    {
        if (weapon == null || !weapon.IsValid)
        {
            return CSWeaponType.WEAPONTYPE_UNKNOWN;
        }

        CCSWeaponBaseVData? weaponVData = weapon.As<CCSWeaponBase>().VData;
        if (weaponVData == null)
        {
            return CSWeaponType.WEAPONTYPE_UNKNOWN;
        }

        return weaponVData.WeaponType;
    }

    public static List<CCSPlayerController> GetSpectatingPlayers()
    {
        return Utilities.GetPlayers().Where(player => player.Team == CsTeam.Spectator).ToList();
    }

    public static List<CCSPlayerController> GetCounterTerroristPlayers()
    {
        return Utilities.GetPlayers().Where(player => player.Team == CsTeam.CounterTerrorist).ToList();
    }

    public static List<CCSPlayerController> GetTerroristPlayers()
    {
        return Utilities.GetPlayers().Where(player => player.Team == CsTeam.Terrorist).ToList();
    }
}

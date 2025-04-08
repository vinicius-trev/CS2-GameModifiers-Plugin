
using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace GameModifiers.Modifiers;

public abstract class GameModifierModelSwap : GameModifierBase
{
    private Dictionary<int, string> _playerModelCache = new();

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        ActivateSwap();
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.RemoveListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        DeactivateSwap();
        _playerModelCache.Clear();

        base.Disabled();
    }

    protected virtual void ActivateSwap()
    {
        Utilities.GetPlayers().ForEach(ApplyPlayerModel);;
    }

    protected virtual void DeactivateSwap()
    {
        Utilities.GetPlayers().ForEach(ResetPlayerModel);
    }

    protected void SetPlayerModel(CCSPlayerController? player, CsTeam teamModel)
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

        string playerModel = playerPawn.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName;
        _playerModelCache.Add(player.Slot, playerModel);

        if (teamModel == CsTeam.Terrorist)
        {
            playerPawn.SetModel("characters/models/tm_phoenix/tm_phoenix.vmdl");
        }
        else if (teamModel == CsTeam.CounterTerrorist)
        {
            playerPawn.SetModel("characters/models/ctm_sas/ctm_sas.vmdl");
        }
        else
        {
            Console.WriteLine("[GameModifierModelSwap::ChangePlayerModel] Attempting to use unsupported team model!");
        }

        Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
    }

    protected virtual void ResetPlayerModel(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
        {
            return;
        }

        if (!_playerModelCache.ContainsKey(player.Slot))
        {
            return;
        }

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
        {
            return;
        }

        playerPawn.SetModel(_playerModelCache[player.Slot]);
    }

    protected virtual void ApplyPlayerModel(CCSPlayerController? player)
    {
        // Implement in child...
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false || player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        ApplyPlayerModel(player);
        return HookResult.Continue;
    }

    private void OnClientDisconnect(int slot)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);
        if (player == null || player.IsValid is not true)
        {
            if (_playerModelCache.ContainsKey(slot))
            {
                _playerModelCache.Remove(slot);
            }

            return;
        }

        ResetPlayerModel(player);
    }
}

public class GameModifierTeamModelSwap : GameModifierModelSwap
{
    public override string Name => "TeamModelSwap";
    public override string Description => "Switches player models for both sides";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierWhosWho>(),
        GameModifiersUtils.GetModifierName<GameModifierImposters>()
    ];

    protected override void ApplyPlayerModel(CCSPlayerController? player)
    {
        if (player != null && player.IsValid)
        {
            CsTeam otherTeam = player.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
            SetPlayerModel(player, otherTeam);
        }
    }
}

public class GameModifierWhosWho : GameModifierModelSwap
{
    public override string Name => "WhosWho";
    public override string Description => "Random player models for both sides";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierTeamModelSwap>(),
        GameModifiersUtils.GetModifierName<GameModifierImposters>()
    ];

    protected override void ApplyPlayerModel(CCSPlayerController? player)
    {
        Random random = new Random();
        SetPlayerModel(player, (CsTeam)random.Next(2, 4));
    }
}

public class GameModifierImposters : GameModifierModelSwap
{
    public override string Name => "Imposters";
    public override string Description => "A random player for each team has swapped sides";
    public override bool SupportsRandomRounds => true;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierTeamModelSwap>(),
        GameModifiersUtils.GetModifierName<GameModifierWhosWho>(),
        GameModifiersUtils.GetModifierName<GameModifierRandomSpawn>()
    ];

    private readonly Dictionary<int, string> _cachedPlayerNames = new();

    protected override void ActivateSwap()
    {
        ApplyImposter(GameModifiersUtils.GetTerroristPlayers());
        ApplyImposter(GameModifiersUtils.GetCounterTerroristPlayers());

        List<string> playerNames = Utilities.GetPlayers()
            .Where(player => player.IsValid && (player.Team == CsTeam.CounterTerrorist || player.Team == CsTeam.Terrorist))
            .Select(player => player.PlayerName)
            .ToList();

        _cachedPlayerNames.Clear();
        Utilities.GetPlayers().ForEach(player =>
        {
            _cachedPlayerNames.Add(player.Slot, player.PlayerName);

            int randomNameIdx = Random.Shared.Next(playerNames.Count);
            player.PlayerName = playerNames[randomNameIdx];
            playerNames.RemoveAt(randomNameIdx);

            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
        });

        GameModifiersUtils.PrintTitleToChatAll("All names have been randomized for the imposter modifier!");
    }

    protected override void DeactivateSwap()
    {
        Utilities.GetPlayers().ForEach(player =>
        {
            if (_cachedPlayerNames.ContainsKey(player.Slot))
            {
                player.PlayerName = _cachedPlayerNames[player.Slot];
                Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
            }
        });

        GameModifiersUtils.PrintTitleToChatAll("All names have been put back to normal!");
        _cachedPlayerNames.Clear();
    }

    private void ApplyImposter(List<CCSPlayerController> players)
    {
        if (players.Count <= 0)
        {
            return;
        }

        int randomPlayerIdx = Random.Shared.Next(players.Count);
        CCSPlayerController player = players[randomPlayerIdx];

        Vector? spawnPosition = null;
        if (player.Team == CsTeam.Terrorist)
        {
            spawnPosition = GameModifiersUtils.GetSpawnLocation(CsTeam.CounterTerrorist);
        }
        else if (player.Team == CsTeam.CounterTerrorist)
        {
            spawnPosition = GameModifiersUtils.GetSpawnLocation(CsTeam.Terrorist);
        }
        else
        {
            Console.WriteLine($"[GameModifierImposters::ApplyImposter] WARNING: Trying to apply imposter to un-supported player type for {player.PlayerName}!");
            return;
        }

        // Apply player model swap
        ApplyPlayerModel(player);

        // Apply spawn pos
        if (spawnPosition != null)
        {
            GameModifiersUtils.TeleportPlayer(player, spawnPosition);
        }
        else
        {
            Console.WriteLine($"[GameModifierImposters::ApplyImposter] WARNING: Could not move {player.PlayerName} to the opposing spawn!");
        }
    }

    protected override void ApplyPlayerModel(CCSPlayerController? player)
    {
        if (player != null && player.IsValid)
        {
            CsTeam otherTeam = player.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
            SetPlayerModel(player, otherTeam);
        }
    }
}



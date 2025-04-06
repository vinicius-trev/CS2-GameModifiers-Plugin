
using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace GameModifiers.Modifiers;

public abstract class GameModifierHealth : GameModifierBase
{
    public virtual int MaxHealth { get; protected set; } = 90;
    protected readonly Dictionary<int, int> CachedOriginalMaxHealth = new();

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        Utilities.GetPlayers().ForEach(ApplyHealthToPlayer);
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Core.RemoveListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        }

        Utilities.GetPlayers().ForEach(ResetHealth);

        base.Disabled();
    }

    protected virtual void ApplyHealthToPlayer(CCSPlayerController? player)
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

        if (!CachedOriginalMaxHealth.ContainsKey(player.Slot))
        {
            CachedOriginalMaxHealth.Add(player.Slot, playerPawn.MaxHealth);
        }

        GameModifiersUtils.SetPlayerMaxHealth(playerPawn, MaxHealth);
    }

    protected void ResetHealth(CCSPlayerController? player)
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

        if (CachedOriginalMaxHealth.ContainsKey(player.Slot))
        {
            GameModifiersUtils.SetPlayerMaxHealth(playerPawn, CachedOriginalMaxHealth[player.Slot]);
        }
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || player.IsValid == false || player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }

        ApplyHealthToPlayer(player);
        return HookResult.Continue;
    }

    private void OnClientDisconnect(int slot)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);
        if (player == null || player.IsValid is not true)
        {
            if (CachedOriginalMaxHealth.ContainsKey(slot))
            {
                CachedOriginalMaxHealth.Remove(slot);
            }

            return;
        }

        ResetHealth(player);
    }
}

public class GameModifierJuggernaut : GameModifierHealth
{
    public override string Name => "Juggernaut";
    public override string Description => "Everyone's max health is set to 500";
    public override bool SupportsRandomRounds => true;
    public override int MaxHealth { get; protected set; } = 500;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierGlassCannon>(),
        GameModifiersUtils.GetModifierName<GameModifierRandomHealth>()
    ];
}

public class GameModifierGlassCannon : GameModifierHealth
{
    public override string Name => "GlassCannon";
    public override string Description => "Everyone is 1 hit to kill";
    public override bool SupportsRandomRounds => true;
    public override int MaxHealth { get; protected set; } = 1;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierJuggernaut>(),
        GameModifiersUtils.GetModifierName<GameModifierRandomHealth>()
    ];
}

public class GameModifierRandomHealth : GameModifierHealth
{
    public override string Name => "RandomHealth";
    public override string Description => "Everyone's health is set to a random number";
    public override bool SupportsRandomRounds => true;
    private readonly Tuple<int, int> HealthRange = new Tuple<int, int>(1, 100);
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierJuggernaut>(),
        GameModifiersUtils.GetModifierName<GameModifierGlassCannon>()
    ];

    protected override void ApplyHealthToPlayer(CCSPlayerController? player)
    {
        Random random = new Random();
        MaxHealth = random.Next(HealthRange.Item1, HealthRange.Item2);
        base.ApplyHealthToPlayer(player);
    }
}

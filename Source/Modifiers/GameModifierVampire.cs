
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace GameModifiers.Modifiers;

public class GameModifierVampire : GameModifierBase
{
    public override string Name => "Vampire";
    public override string Description => "You steal the damage you deal";
    public override bool SupportsRandomRounds { get; protected set; } = true;

    public override void Enabled()
    {
        base.Enabled();

        if (Core != null)
        {
            Core.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurtEvent);
        }
    }

    public override void Disabled()
    {
        if (Core != null)
        {
            Core.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurtEvent);
        }

        // For now we just reset everyones health to default on removal ¯\_(ツ)_/¯
        Utilities.GetPlayers().ForEach(controller =>
        {
            var playerPawn = controller.PlayerPawn.Value;
            if (playerPawn != null && playerPawn.IsValid)
            {
                GameModifiersUtils.SetPlayerHealth(playerPawn, 100);
            }
        });

        base.Disabled();
    }

    private HookResult OnPlayerHurtEvent(EventPlayerHurt @event, GameEventInfo eventInfo)
    {
        CCSPlayerController? attackingPlayer = @event.Attacker;
        CCSPlayerController? damagedPlayer = @event.Userid;
        if (attackingPlayer == null || !attackingPlayer.IsValid || damagedPlayer == null || !damagedPlayer.IsValid || !damagedPlayer.PawnIsAlive)
        {
            return HookResult.Continue;
        }

        var attackingPawn = attackingPlayer.PlayerPawn.Value;
        var damagedPawn = damagedPlayer.PlayerPawn.Value;
        if (attackingPawn == null || !attackingPawn.IsValid || damagedPawn == null || !damagedPawn.IsValid)
        {
            return HookResult.Continue;
        }

        // Return if we are attacking our-self or someone on the same team.
        if (attackingPlayer == damagedPlayer || attackingPlayer.Team == damagedPlayer.Team)
        {
            return HookResult.Continue;
        }

        int increaseHealth = damagedPawn.Health < 0 ? @event.DmgHealth + damagedPawn.Health : @event.DmgHealth;
        GameModifiersUtils.SetPlayerHealth(attackingPawn, attackingPawn.Health + increaseHealth);
        return HookResult.Continue;
    }
}

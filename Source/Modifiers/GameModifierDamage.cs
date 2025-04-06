
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace GameModifiers.Modifiers;

public abstract class GameModifierDamageMultiplier : GameModifierBase
{
    public virtual float DamageMultiplier { get; protected set; } = 1.0f;

    public override void Enabled()
    {
        base.Enabled();

        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    }

    public override void Disabled()
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);

        base.Disabled();
    }

    private HookResult OnTakeDamage(DynamicHook hook)
    {
        CTakeDamageInfo damageInfo = hook.GetParam<CTakeDamageInfo>(1);
        damageInfo.Damage *= DamageMultiplier;
        return HookResult.Continue;
    }
}

public class GameModifierMoreDamage : GameModifierDamageMultiplier
{
    public override string Name => "MoreDamage";
    public override string Description => "Damage dealt is doubled";
    public override bool SupportsRandomRounds => true;
    public override float DamageMultiplier { get; protected set; } = 2.0f;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierLessDamage>()
    ];
}

public class GameModifierLessDamage : GameModifierDamageMultiplier
{
    public override string Name => "LessDamage";
    public override string Description => "Damage dealt is halved";
    public override bool SupportsRandomRounds => true;
    public override float DamageMultiplier { get; protected set; } = 0.5f;
    public override HashSet<string> IncompatibleModifiers =>
    [
        GameModifiersUtils.GetModifierName<GameModifierMoreDamage>()
    ];
}

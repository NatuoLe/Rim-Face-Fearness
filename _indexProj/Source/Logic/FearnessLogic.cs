using RimWorld;
using Verse;
using HarmonyLib;
using System;

namespace Fearness
{

[DefOf]
public static class FearnessDefOf
{
    public static HediffDef Cowardly;
    public static HediffDef Brave;
    public static HediffDef Afraid;
    public static HediffDef Panicked;
    public static HediffDef Calm;
    public static HediffDef CourageLevelUp;
    public static FearnessSettingsDef FearnessSettings;
    public static ThoughtDef SevereBleeding;

    static FearnessDefOf()
    {
    }

    public static CompProperties_Fearness FearnessProps => FearnessSettings?.fearnessProps ?? new CompProperties_Fearness();
    public static CompProperties_Courage CourageProps => FearnessSettings?.courageProps ?? new CompProperties_Courage();
}

public class ThoughtWorker_SevereBleeding : ThoughtWorker
{
    public ThoughtWorker_SevereBleeding() 
    { 
        Log.Message("[Fearness] ThoughtWorker_SevereBleeding constructor called");
    }
    
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (p.health?.hediffSet == null)
        {
            Log.Message($"[Fearness] SevereBleeding: {p?.Name?.ToStringFull ?? "Unknown"} - health.hediffSet is null");
            return ThoughtState.Inactive;
        }

        float bleedRate = p.health.hediffSet.BleedRateTotal;
        float maxBleedRate = GetMaxBleedingRate(p);
        float ratio = maxBleedRate > 0f ? bleedRate / maxBleedRate : 0f;
        
        Log.Message($"[Fearness] SevereBleeding: {p?.Name?.ToStringFull ?? "Unknown"} - BleedRate={bleedRate:F3}, MaxBleedRate={maxBleedRate:F3}, Ratio={ratio:F2}");
        
        if (maxBleedRate > 0f && bleedRate / maxBleedRate >= Define.SevereBleedingThreshold)
        {
            Log.Message($"[Fearness] SevereBleeding: {p?.Name?.ToStringFull ?? "Unknown"} - ACTIVATED");
            return ThoughtState.ActiveAtStage(0);
        }

        return ThoughtState.Inactive;
    }
    
    private static float GetMaxBleedingRate(Pawn pawn)
    {
        if (pawn?.RaceProps?.baseBodySize > 0f)
        {
            return 1.5f / pawn.RaceProps.baseBodySize;
        }
        return 1f;
    }
}

public static class FearnessLogic
{
    public static void ApplyCombatCourageBonus(Pawn pawn, ref float hitChance)
    {
        CourageComponent courage = pawn.GetCourage();
        if (courage != null)
        {
            float courageBonus = (courage.Level - 10f) / 50f;
            hitChance += courageBonus * 0.2f;
        }
    }

    public static void ApplyFearnessCombatPenalty(Pawn pawn, ref float hitChance)
    {
        FearnessComponent fearness = pawn.GetFearness();
        if (fearness != null)
        {
            if (fearness.IsPanicked)
            {
                hitChance *= 0.6f;
            }
            else if (fearness.IsAfraid)
            {
                hitChance *= 0.8f;
            }
        }
    }

    public static void ApplyMeleeDamageBonus(Pawn pawn, ref float damage)
    {
        CourageComponent courage = pawn.GetCourage();
        if (courage != null)
        {
            float damageBonus = (courage.Level - 10f) / 20f;
            damage *= (1f + damageBonus * 0.1f);
        }
    }
}

[HarmonyPatch(typeof(Verb_MeleeAttack), "GetNonMissChance")]
public static class Verb_MeleeAttack_GetNonMissChance_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref float __result, Verb_MeleeAttack __instance)
    {
        if (__instance.CasterPawn != null)
        {
            float originalHitChance = __result;
            FearnessLogic.ApplyCombatCourageBonus(__instance.CasterPawn, ref __result);
            FearnessLogic.ApplyFearnessCombatPenalty(__instance.CasterPawn, ref __result);
            Log.Message($"[Fearness] {__instance.CasterPawn?.Name?.ToStringFull ?? "Unknown"} MeleeHitChance: {originalHitChance:F3} -> {__result:F3}");
        }
    }
}

[HarmonyPatch(typeof(Pawn_MeleeVerbs), "TryMeleeAttack")]
public static class Pawn_MeleeVerbs_TryMeleeAttack_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn ___pawn, Thing target)
    {
        if (___pawn != null && target != null)
        {
            CourageComponent courageComp = ___pawn.GetComp<CourageComponent>();
            courageComp?.Learn(2f);
            Log.Message($"[Courage] {___pawn?.Name?.ToStringFull ?? "Unknown"} Melee Attack: +2 XP");
        }
    }
}

[HarmonyPatch(typeof(Thing), "TakeDamage")]
public static class Thing_TakeDamage_Patch
{
    [HarmonyPostfix]
    public static void Postfix(DamageInfo dinfo, Thing __instance)
    {
        if (dinfo.Amount <= 0) return;
        
        Pawn pawn = __instance as Pawn;
        if (pawn == null) return;
        
        Log.Message($"[Fearness] {pawn?.Name?.ToStringFull ?? "Unknown"} Took damage: {dinfo.Amount} from {dinfo.Def?.label ?? "unknown"}");

        FearnessComponent fearnessComp = pawn.GetComp<FearnessComponent>();
        if (fearnessComp != null)
        {
            float baseDamage = fearnessComp.Props?.baseDamageFearReduction ?? 15f;
            float damageFactor = Math.Min(dinfo.Amount / 10f, 3f);
            float actualReduction = baseDamage * damageFactor;
            
            fearnessComp.AddFearnessReduction(actualReduction);
            Log.Message($"[Fearness] {pawn?.Name?.ToStringFull ?? "Unknown"} Fearless reduced by {actualReduction:F1} (base={baseDamage}, factor={damageFactor:F2})");
        }
        else
        {
            Log.Warning($"[Fearness] {pawn?.Name?.ToStringFull ?? "Unknown"} FearnessComponent not found!");
        }

        CourageComponent courageComp = pawn.GetComp<CourageComponent>();
        if (courageComp != null)
        {
            float xpGain = dinfo.Amount;
            courageComp.Learn(xpGain);
            Log.Message($"[Courage] {pawn?.Name?.ToStringFull ?? "Unknown"} Gained {xpGain:F1} XP from damage");
        }
        else
        {
            Log.Warning($"[Fearness] {pawn?.Name?.ToStringFull ?? "Unknown"} CourageComponent not found!");
        }
    }
}

}
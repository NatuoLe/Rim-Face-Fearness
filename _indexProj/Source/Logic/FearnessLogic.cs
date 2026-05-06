using RimWorld;
using Verse;
using HarmonyLib;

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

    public static CompProperties_Fearness FearnessProps;
    public static CompProperties_Courage CourageProps;

    static FearnessDefOf()
    {
        ResolveComps();
    }

    public static void ResolveComps()
    {
        FearnessProps = new CompProperties_Fearness();
        CourageProps = new CompProperties_Courage();
        Log.Message("[Fearness] CompProperties initialized with defaults: Fearness(decay=" + FearnessProps.decayRate + ", maxLevel=" + FearnessProps.maxLevel + "), Courage(baseLevel=" + CourageProps.baseLevel + ")");
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

[HarmonyPatch(typeof(Verb), "CalculateHitChance")]
public static class Verb_CalculateHitChance_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref float __result, Verb __instance)
    {
        if (__instance.CasterPawn != null)
        {
            float originalHitChance = __result;
            FearnessLogic.ApplyCombatCourageBonus(__instance.CasterPawn, ref __result);
            FearnessLogic.ApplyFearnessCombatPenalty(__instance.CasterPawn, ref __result);
            Log.Message($"[Fearness] {__instance.CasterPawn?.Name?.ToStringFull ?? "Unknown"} HitChance: {originalHitChance:F3} -> {__result:F3}");
        }
    }
}

[HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff")]
public static class Pawn_HealthTracker_AddHediff_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Hediff hediff, Pawn __instance)
    {
        if (hediff.def.defName == "Injury" || hediff.def.defName == "Bleeding")
        {
            FearnessComponent fearnessComp = __instance.GetComp<FearnessComponent>();
            fearnessComp?.AddFear(15f);
            Log.Message($"[Fearness] {__instance?.Name?.ToStringFull ?? "Unknown"} Took Damage: +15 Fear");

            CourageComponent courageComp = __instance.GetComp<CourageComponent>();
            courageComp?.Learn(5f);
            Log.Message($"[Courage] {__instance?.Name?.ToStringFull ?? "Unknown"} Took Damage: +5 XP (from injury)");
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

}
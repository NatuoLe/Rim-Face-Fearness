using HarmonyLib;
using Verse;
using RimWorld;

namespace Fearness
{

[StaticConstructorOnStartup]
public static class HarmonyPatches
{
    static HarmonyPatches()
    {
        Log.Message("[Fearness] === Fearness Mod Loading Started ===");
        Log.Message("[Fearness] Assembly loaded: " + typeof(HarmonyPatches).Assembly.FullName);

        Log.Message("[Fearness] CompProperties: Fearness(decay=" + FearnessDefOf.FearnessProps.decayRate + ", maxLevel=" + FearnessDefOf.FearnessProps.maxLevel + "), Courage(baseLevel=" + FearnessDefOf.CourageProps.baseLevel + ")");

        var harmony = new Harmony("thgold.fearness");
        Log.Message("[Fearness] Patching all methods with Harmony...");

        harmony.PatchAll();

        Log.Message("[Fearness] Harmony patching complete!");
        Log.Message("[Fearness] === Fearness Mod Loaded Successfully ===");
    }
}

[HarmonyPatch(typeof(Pawn), "SpawnSetup")]
public static class Pawn_SpawnSetup_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, Map map, bool respawningAfterLoad)
    {
        if (__instance == null || respawningAfterLoad) return;
        if (!__instance.RaceProps.Humanlike) return;

        Log.Message("[Fearness] Pawn_SpawnSetup_Patch: " + __instance?.Name?.ToStringFull ?? "Unknown");

        EnsureComponents(__instance);
    }

    private static void EnsureComponents(Pawn pawn)
    {
        if (pawn == null) return;
        if (!pawn.RaceProps.Humanlike) return;

        GetOrCreateFearnessComp(pawn);
        GetOrCreateCourageComp(pawn);
    }

    private static FearnessComponent GetOrCreateFearnessComp(Pawn pawn)
    {
        var existingComp = pawn.GetComp<FearnessComponent>();
        if (existingComp != null)
        {
            return existingComp;
        }

        var compProps = FearnessDefOf.FearnessProps;
        if (compProps == null)
        {
            Log.Warning("[Fearness] FearnessProps not found, creating default");
            compProps = new CompProperties_Fearness();
        }

        var comp = new FearnessComponent();
        comp.props = compProps;
        comp.parent = pawn;
        comp.Initialize(compProps);
        pawn.AllComps.Add(comp);

        Log.Message("[Fearness] FearnessComponent added to " + pawn.Name.ToStringFull);
        return comp;
    }

    private static CourageComponent GetOrCreateCourageComp(Pawn pawn)
    {
        var existingComp = pawn.GetComp<CourageComponent>();
        if (existingComp != null)
        {
            return existingComp;
        }

        var compProps = FearnessDefOf.CourageProps;
        if (compProps == null)
        {
            Log.Warning("[Fearness] CourageProps not found, creating default");
            compProps = new CompProperties_Courage();
        }

        var comp = new CourageComponent();
        comp.props = compProps;
        comp.parent = pawn;
        comp.Initialize(compProps);
        pawn.AllComps.Add(comp);

        Log.Message("[Fearness] CourageComponent added to " + pawn.Name.ToStringFull);
        return comp;
    }
}

[HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new[] { typeof(PawnGenerationRequest) })]
public static class PawnGenerator_GeneratePawn_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref Pawn __result)
    {
        if (__result == null) return;
        if (!__result.RaceProps.Humanlike) return;

        Log.Message("[Fearness] PawnGenerator_GeneratePawn_Patch: " + (__result?.Name?.ToStringFull ?? "null"));

        GetOrCreateFearnessComp(__result);
        GetOrCreateCourageComp(__result);
    }

    private static FearnessComponent GetOrCreateFearnessComp(Pawn pawn)
    {
        var existingComp = pawn.GetComp<FearnessComponent>();
        if (existingComp != null)
        {
            return existingComp;
        }

        var compProps = FearnessDefOf.FearnessProps;
        if (compProps == null)
        {
            Log.Warning("[Fearness] FearnessProps not found, creating default");
            compProps = new CompProperties_Fearness();
        }

        var comp = new FearnessComponent();
        comp.props = compProps;
        comp.parent = pawn;
        comp.Initialize(compProps);
        pawn.AllComps.Add(comp);

        Log.Message("[Fearness] [GeneratePawn] FearnessComponent added to " + pawn.Name.ToStringFull);
        return comp;
    }

    private static CourageComponent GetOrCreateCourageComp(Pawn pawn)
    {
        var existingComp = pawn.GetComp<CourageComponent>();
        if (existingComp != null)
        {
            return existingComp;
        }

        var compProps = FearnessDefOf.CourageProps;
        if (compProps == null)
        {
            Log.Warning("[Fearness] CourageProps not found, creating default");
            compProps = new CompProperties_Courage();
        }

        var comp = new CourageComponent();
        comp.props = compProps;
        comp.parent = pawn;
        comp.Initialize(compProps);
        pawn.AllComps.Add(comp);

        Log.Message("[Fearness] [GeneratePawn] CourageComponent added to " + pawn.Name.ToStringFull);
        return comp;
    }
}

[HarmonyPatch(typeof(Pawn), "ExposeData")]
public static class Pawn_ExposeData_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance)
    {
        if (__instance == null) return;

        var fearnessComp = __instance.GetComp<FearnessComponent>();
        fearnessComp?.ExposeData();

        var courageComp = __instance.GetComp<CourageComponent>();
        courageComp?.ExposeData();
    }
}

[HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new[] { typeof(PawnGenerationRequest) })]
public static class PawnGenerator_GeneratePawn_SkillSync_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref Pawn __result)
    {
        if (__result == null) return;
        
        CourageSkillSync.SyncCourageToSkill(__result);
    }
}

public static class CourageSkillSync
{
    public static void SyncCourageToSkill(Pawn pawn)
    {
        if (pawn == null) return;

        var courageComp = pawn.GetComp<CourageComponent>();
        if (courageComp == null) return;

        SkillDef courageSkillDef = DefDatabase<SkillDef>.GetNamed("Courage", false);
        if (courageSkillDef == null)
        {
            Log.Warning("[Fearness] Courage SkillDef not found");
            return;
        }

        SkillRecord skill = pawn.skills?.GetSkill(courageSkillDef);
        if (skill == null)
        {
            Log.Warning("[Fearness] Could not get courage skill record for " + pawn.Name.ToStringFull);
            return;
        }

        skill.levelInt = courageComp.Level - 1;
        skill.xpSinceLastLevel = courageComp.XpSinceLastLevel;
        
        Log.Message("[Fearness] Synced courage to skill: " + pawn.Name.ToStringFull + " Level=" + courageComp.Level + ", XP=" + courageComp.XpSinceLastLevel);
    }
}

}
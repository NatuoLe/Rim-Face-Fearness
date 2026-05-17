using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace Fearness
{

public class CompProperties_Fearness : CompProperties
{
    public float decayRate = 0.5f;
    public float maxLevel = 100f;
    public float baseDamageFearReduction = 15f;

    public CompProperties_Fearness()
    {
        Log.Message("[Fearness] CompProperties_Fearness constructor called");
        compClass = typeof(FearnessComponent);
    }
}

public class FearnessComponent : ThingComp, IExposable
{
    private Pawn Pawn => parent as Pawn;

    private float curLevelInt;
    private float lastLevelInt;

    static FearnessComponent()
    {
        Log.Message("[Fearness] FearnessComponent class loaded");
    }

    public float MaxLevel => Define.FearnessMaxLevel;

    public CompProperties_Fearness Props => (CompProperties_Fearness)props;

    public float CurLevel
    {
        get => curLevelInt;
        set => curLevelInt = (value < 0f) ? 0f : ((value > MaxLevel) ? MaxLevel : value);
    }

    public float CurLevelPercentage => CurLevel / MaxLevel;

    public float LastLevelPercentage => lastLevelInt / MaxLevel;

    public CourageStatus Status
    {
        get
        {
            if (CurLevelPercentage >= 0.8f) return CourageStatus.Brave;
            if (CurLevelPercentage >= 0.5f) return CourageStatus.Normal;
            if (CurLevelPercentage >= 0.2f) return CourageStatus.Afraid;
            return CourageStatus.Panicked;
        }
    }

    public bool IsAfraid => Status <= CourageStatus.Afraid;
    public bool IsPanicked => Status == CourageStatus.Panicked;
    public bool IsBrave => Status == CourageStatus.Brave;

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        curLevelInt = MaxLevel;
        lastLevelInt = MaxLevel;
    }

    public void AddFearnessReduction(float baseAmount)
    {
        lastLevelInt = curLevelInt;

        float courageLevel = 1f;
        CourageComponent courageComp = Pawn?.GetComp<CourageComponent>();
        if (courageComp != null)
        {
            courageLevel = courageComp.Level;
        }

        float reductionFactor = 1f - (courageLevel - 1f) / (Define.CourageMaxLevel - 1f) * Define.CourageFearReductionFactor;
        float actualReduction = baseAmount * reductionFactor;

        CurLevel -= actualReduction;
        Log.Message($"[Fearness] {Pawn?.Name?.ToStringFull ?? "Unknown"} TookDamage: -{actualReduction:F1} (Courage={courageLevel}, Factor={reductionFactor:F2}), Current: {CurLevel:F1} ({Status})");

    }

    public void FearnessInterval()
    {
        if (Pawn == null || Pawn.Suspended) return;

        lastLevelInt = curLevelInt;

        float courageLevel = 1f;
        CourageComponent courageComp = Pawn.GetComp<CourageComponent>();
        if (courageComp != null)
        {
            courageLevel = courageComp.Level;
        }

        float moodFactor = Pawn.needs?.mood?.CurLevel ?? 0.5f;
        float recoveryRate = Define.FearnessDecayRate * (1f + (courageLevel - 1f) / (Define.CourageMaxLevel - 1f) * Define.CourageFearReductionFactor);

        bool hasSevereBleeding = HasSevereBleedingThought();
        float bleedingReduction = 0f;

        if (hasSevereBleeding)
        {
            float baseBleedingReduction = Define.FearnessBaseDamageReduction * Define.FearnessBleedingReductionFactor;
            float reductionFactor = 1f - (courageLevel - 1f) / (Define.CourageMaxLevel - 1f) * Define.CourageFearReductionFactor;
            bleedingReduction = baseBleedingReduction * reductionFactor;
            CurLevel -= bleedingReduction;
            Log.Message($"[Fearness] {Pawn?.Name?.ToStringFull ?? "Unknown"} SevereBleeding: -{bleedingReduction:F3}, Current: {CurLevel:F1}");
        }

        if (CurLevel < MaxLevel && !hasSevereBleeding)
        {
            float recoveryAmount = recoveryRate * moodFactor;
            CurLevel += recoveryAmount;
            Log.Message($"[Fearness] {Pawn?.Name?.ToStringFull ?? "Unknown"} Recovery: +{recoveryAmount:F3}, Current: {CurLevel:F1} ({Status})");
        }
    }

    private bool HasSevereBleedingThought()
    {
        if (Pawn?.needs?.mood?.thoughts?.memories == null) return false;
        
        var severeBleedingDef = DefDatabase<ThoughtDef>.GetNamed("SevereBleeding", false);
        if (severeBleedingDef == null)
        {
            Log.Message("[Fearness] SevereBleeding ThoughtDef not found in database");
            return false;
        }
        
        var memories = Pawn.needs.mood.thoughts.memories.Memories;
        foreach (var memory in memories)
        {
            if (memory.def == severeBleedingDef)
            {
                return true;
            }
        }
        return false;
    }

    public override void CompTick()
    {
        base.CompTick();
        if (Pawn != null && Pawn.IsHashIntervalTick(200))
        {
            FearnessInterval();
        }
    }

    public override string CompInspectStringExtra()
    {
        return "Fearness".Translate() + ": " + CurLevelPercentage.ToStringPercent() + " (" + Status.ToString().Translate() + ")";
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref curLevelInt, "fearnessLevel", MaxLevel);
        Scribe_Values.Look(ref lastLevelInt, "fearnessLastLevel", MaxLevel);
    }
}

public enum CourageStatus
{
    Panicked,
    Afraid,
    Normal,
    Brave
}

}

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Fearness
{

public class CompProperties_FearnessCompProperties_Fearness : CompProperties
{
    public float decayRate = 0.01f;
    public float recoveryRate = 0.005f;
    public float maxLevel = 100f;

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

    public float MaxLevel => Props.maxLevel;

    public CompProperties_Fearness Props => (CompProperties_Fearness)props;

    public float CurLevel
    {
        get => curLevelInt;
        set => curLevelInt = (value < 0f) ? 0f : ((value > MaxLevel) ? MaxLevel : value);
    }

    public float CurLevelPercentage => CurLevel / MaxLevel;

    public float LastLevelPercentage => lastLevelInt / MaxLevel;

    public FearStatus Status
    {
        get
        {
            if (CurLevel < 20f) return FearStatus.Calm;
            if (CurLevel < 50f) return FearStatus.Normal;
            if (CurLevel < 80f) return FearStatus.Afraid;
            return FearStatus.Panicked;
        }
    }

    public bool IsAfraid => Status >= FearStatus.Afraid;
    public bool IsPanicked => Status == FearStatus.Panicked;
    public bool IsCalm => Status == FearStatus.Calm;

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        curLevelInt = 0f;
        lastLevelInt = 0f;
    }

    public void AddFear(float amount)
    {
        lastLevelInt = curLevelInt;
        CurLevel += amount;
        Log.Message($"[Fearness] {Pawn?.Name?.ToStringFull ?? "Unknown"} AddFear: {amount}, Current: {CurLevel} ({Status})");

        CourageComponent courageComp = Pawn?.GetComp<CourageComponent>();
        if (courageComp != null && amount > 0)
        {
            courageComp.Learn(amount);
        }
    }

    public void ReduceFear(float amount)
    {
        lastLevelInt = curLevelInt;
        CurLevel -= amount;
        Log.Message($"[Fearness] {Pawn?.Name?.ToStringFull ?? "Unknown"} ReduceFear: {amount}, Current: {CurLevel} ({Status})");
    }

    public void FearnessInterval()
    {
        if (Pawn == null || Pawn.Suspended) return;

        lastLevelInt = curLevelInt;

        float courageBonus = 0f;
        CourageComponent courageComp = Pawn.GetComp<CourageComponent>();
        if (courageComp != null)
        {
            courageBonus = courageComp.Level * 0.001f;
        }

        float moodFactor = Pawn.needs?.mood?.CurLevel ?? 0.5f;

        if (curLevelInt > 0f)
        {
            float decayAmount = (Props.decayRate + courageBonus) * moodFactor;
            CurLevel -= decayAmount;
            Log.Message($"[Fearness] {Pawn?.Name?.ToStringFull ?? "Unknown"} Decay: -{decayAmount}, Current: {CurLevel} ({Status})");
        }
        else
        {
            float recoveryAmount = Props.recoveryRate * moodFactor;
            CurLevel = (float)System.Math.Max(curLevelInt - recoveryAmount, 0f);
            Log.Message($"[Fearness] {Pawn?.Name?.ToStringFull ?? "Unknown"} Recovery: +{recoveryAmount}, Current: {CurLevel} ({Status})");
        }
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
        return "Fearness".Translate() + ": " + CurLevelPercentage.ToStringPercent() + " (" + Status.ToString() + ")";
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref curLevelInt, "fearnessLevel", 0f);
        Scribe_Values.Look(ref lastLevelInt, "fearnessLastLevel", 0f);
    }
}

public enum FearStatus
{
    Calm,
    Normal,
    Afraid,
    Panicked
}

}
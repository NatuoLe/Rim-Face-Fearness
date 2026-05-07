using RimWorld;
using UnityEngine;
using Verse;

namespace Fearness
{
public class CompProperties_Courage : CompProperties
{
    public int baseLevel = 1;
    public float baseXP = 0f;

    public CompProperties_Courage()
    {
        Log.Message("[Fearness] CompProperties_Courage constructor called");
        compClass = typeof(CourageComponent);
    }
}

public class CourageComponent : ThingComp, IExposable
{
    private Pawn Pawn => parent as Pawn;

    private int levelInt;
    private float xpSinceLastLevel;

    static CourageComponent()
    {
        Log.Message("[Fearness] CourageComponent class loaded");
    }

    public const int MinLevel = 1;
    public const int MaxLevel = 20;

    private static readonly SimpleCurve XpForLevelUpCurve = new SimpleCurve
    {
        new CurvePoint(0f, 1000f),
        new CurvePoint(9f, 10000f),
        new CurvePoint(19f, 30000f)
    };

    public int Level
    {
        get => levelInt;
        set => levelInt = (value < MinLevel) ? MinLevel : ((value > MaxLevel) ? MaxLevel : value);
    }

    public float XpRequiredForLevelUp => XpRequiredToLevelUpFrom(levelInt);

    public float XpProgressPercent => xpSinceLastLevel / XpRequiredForLevelUp;

    public float XpSinceLastLevel => xpSinceLastLevel;

    public float TotalXP
    {
        get
        {
            float total = 0f;
            for (int i = MinLevel - 1; i < levelInt; i++)
            {
                total += XpRequiredToLevelUpFrom(i);
            }
            return total + xpSinceLastLevel;
        }
    }

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        CompProperties_Courage courageProps = props as CompProperties_Courage;
        if (courageProps != null)
        {
            levelInt = courageProps.baseLevel;
            xpSinceLastLevel = courageProps.baseXP;
        }
        else
        {
            levelInt = MinLevel;
            xpSinceLastLevel = 0f;
        }
    }

    public void Learn(float xp)
    {
        if (levelInt >= MaxLevel) return;

        xpSinceLastLevel += xp;
        Log.Message($"[Courage] {Pawn?.Name?.ToStringFull ?? "Unknown"} Learn: +{xp} XP, Current: {xpSinceLastLevel}/{XpRequiredForLevelUp} ({Level})");

        while (xpSinceLastLevel >= XpRequiredForLevelUp)
        {
            xpSinceLastLevel -= XpRequiredForLevelUp;
            Level++;
            OnLevelUp();
        }
    }

    private void OnLevelUp()
    {
        Log.Message($"[Courage] {Pawn?.Name?.ToStringFull ?? "Unknown"} LevelUp! Now Level {Level}");
        if (Pawn != null)
        {
            Messages.Message("CourageLevelUp".Translate(Pawn.LabelShort, Level), Pawn, MessageTypeDefOf.PositiveEvent);
            SyncToSkill();
        }
    }

    private void SyncToSkill()
    {
        if (Pawn == null) return;

        SkillDef courageSkillDef = DefDatabase<SkillDef>.GetNamed("Courage", false);
        if (courageSkillDef == null) return;

        SkillRecord skill = Pawn.skills?.GetSkill(courageSkillDef);
        if (skill == null) return;

        skill.levelInt = Level - 1;
        skill.xpSinceLastLevel = xpSinceLastLevel;
        
        Log.Message($"[Courage] Synced to skill: {Pawn.Name.ToStringFull} Level={Level}");
    }

    private float XpRequiredToLevelUpFrom(int currentLevel)
    {
        return XpForLevelUpCurve.Evaluate(currentLevel);
    }

    public override string CompInspectStringExtra()
    {
        return "CourageLevel".Translate() + ": " + Level + " (" + XpProgressPercent.ToStringPercent() + ")";
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref levelInt, "courageLevel", MinLevel);
        Scribe_Values.Look(ref xpSinceLastLevel, "courageXP", 0f);
    }
}
}
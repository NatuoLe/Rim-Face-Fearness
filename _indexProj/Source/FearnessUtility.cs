using Verse;

namespace Fearness
{

public static class FearnessUtility
{
    public static FearnessComponent GetFearness(this Pawn pawn)
    {
        return pawn?.GetComp<FearnessComponent>();
    }

    public static float GetFearnessValue(this Pawn pawn)
    {
        return pawn.GetFearness()?.CurLevelPercentage ?? 0f;
    }

    public static FearStatus GetFearnessStatus(this Pawn pawn)
    {
        return pawn.GetFearness()?.Status ?? FearStatus.Calm;
    }

    public static void AddFear(this Pawn pawn, float amount)
    {
        pawn.GetFearness()?.AddFear(amount);
    }

    public static bool IsAfraid(this Pawn pawn)
    {
        return pawn.GetFearness()?.IsAfraid ?? false;
    }

    public static bool IsPanicked(this Pawn pawn)
    {
        return pawn.GetFearness()?.IsPanicked ?? false;
    }

    public static bool IsCalm(this Pawn pawn)
    {
        return pawn.GetFearness()?.IsCalm ?? true;
    }
}

public static class CourageUtility
{
    public static CourageComponent GetCourage(this Pawn pawn)
    {
        return pawn?.GetComp<CourageComponent>();
    }

    public static int GetCourageLevel(this Pawn pawn)
    {
        return pawn.GetCourage()?.Level ?? 1;
    }

    public static float GetCourageXP(this Pawn pawn)
    {
        return pawn.GetCourage()?.XpSinceLastLevel ?? 0f;
    }

    public static float GetCourageProgress(this Pawn pawn)
    {
        return pawn.GetCourage()?.XpProgressPercent ?? 0f;
    }

    public static void IncreaseCourage(this Pawn pawn, float xp)
    {
        pawn.GetCourage()?.Learn(xp);
    }
}

}
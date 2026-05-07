using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Fearness
{
    public class Need_Fearness : Need
    {
        public override int GUIChangeArrow
        {
            get
            {
                if (FearnessComp != null)
                {
                    if (CurLevelPercentage < FearnessComp.LastLevelPercentage)
                    {
                        return -1;
                    }
                    if (CurLevelPercentage > FearnessComp.LastLevelPercentage)
                    {
                        return 1;
                    }
                }
                return 0;
            }
        }

        public override float MaxLevel => 100f;

        public override bool ShowOnNeedList => true;

        private FearnessComponent FearnessComp => pawn.GetComp<FearnessComponent>();

        public CourageStatus CourageStatus => FearnessComp?.Status ?? CourageStatus.Normal;

        public Need_Fearness(Pawn pawn) : base(pawn)
        {
            threshPercents = new List<float> { 0.2f, 0.5f, 0.8f };
        }

        public override void SetInitialLevel()
        {
            if (FearnessComp != null)
            {
                CurLevel = FearnessComp.CurLevel;
            }
            else
            {
                CurLevel = MaxLevel;
            }
        }

        public override void NeedInterval()
        {
            if (FearnessComp != null)
            {
                CurLevel = FearnessComp.CurLevel;
            }
        }

        public override string GetTipString()
        {
            string statusStr = CourageStatus.ToString().Translate();
            return "Need_Fearness".Translate() + ": " + CurLevelPercentage.ToStringPercent() + " (" + statusStr + ")\n" + "Need_Fearness_Desc".Translate();
        }
    }
}

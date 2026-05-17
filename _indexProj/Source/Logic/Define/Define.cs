namespace Fearness
{
    public static class Define
    {
        public const string ModName = "RimFaceFearness";

        // Fearness (无畏值) 配置
        public const float FearnessMaxLevel = 100f;
        public const float FearnessDecayRate = 0.5f;
        public const float FearnessBaseDamageReduction = 15f;
        public const float FearnessBleedingReductionFactor = 0.1f;

        // Courage (勇气) 配置
        public const int CourageBaseLevel = 1;
        public const int CourageMaxLevel = 20;
        public const float CourageBaseXP = 0f;
        public const float CourageXPRequiredPerLevel = 1000f;

        // 严重出血检测阈值
        public const float SevereBleedingThreshold = 2.5f;

        // 勇气对无畏值减少的影响系数
        public const float CourageFearReductionFactor = 0.5f;
    }
}

using RimWorld;
using System.Collections.Generic;
using Verse;

namespace SurvivalTools
{
    public class SurvivalToolProperties : DefModExtension
    {

        public static readonly SurvivalToolProperties defaultValues = new SurvivalToolProperties();

        public List<StatModifier> baseWorkStatFactors;

        [NoTranslate]
        public List<string> defaultSurvivalToolAssignmentTags;

        public float toolWearFactor = 1f;

    }
}

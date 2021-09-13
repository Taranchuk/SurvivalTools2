using RimWorld;
using System.Collections.Generic;
using Verse;

namespace SurvivalTools
{
    public class StuffPropsTool : DefModExtension
    {

        public static readonly StuffPropsTool defaultValues = new StuffPropsTool();

        public List<StatModifier> toolStatFactors;

        public float wearFactorMultiplier = 1f;

    }
}

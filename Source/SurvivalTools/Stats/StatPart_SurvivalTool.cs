using RimWorld;
using Verse;

namespace SurvivalTools
{
    public class StatPart_SurvivalTool : StatPart
    {

        public override string ExplanationPart(StatRequest req)
        {
            // The AI will cheat this system for now until tool generation gets figured out
            return req.Thing is Pawn pawn && pawn.CanUseSurvivalTools()
                ? pawn.HasSurvivalToolFor(parentStat, out SurvivalTool tool, out float statFactor)
                    ? tool.LabelCapNoCount + ": x" + statFactor.ToStringPercent()
                    : (string)("NoTool".Translate() + ": x" + NoToolStatFactor.ToStringPercent())
                : null;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.Thing is Pawn pawn && pawn.CanUseSurvivalTools())
            {
                if (pawn.HasSurvivalToolFor(parentStat, out _, out float statFactor))
                {
                    val *= statFactor;
                }
                else
                {
                    val *= NoToolStatFactor;
                }
            }
        }

        public float NoToolStatFactor => SurvivalToolsSettings.reduceNoToolWorkEfficiency
            ? SurvivalToolsSettings.hardcoreMode ? NoToolStatFactorHardcore : noToolStatFactor
            : 1f;

        private readonly float noToolStatFactor = 0.3f;

        private readonly float noToolStatFactorHardcore = -1f;
        private float NoToolStatFactorHardcore => (noToolStatFactorHardcore != -1f) ? noToolStatFactorHardcore : noToolStatFactor;

    }
}

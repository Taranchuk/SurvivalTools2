﻿using RimWorld;
using System.Text;
using UnityEngine;
using Verse;

namespace SurvivalTools
{
    public class StatWorker_EstimatedLifespan : StatWorker
    {

        public static int BaseWearInterval =>
            Mathf.RoundToInt(GenDate.TicksPerHour * ((SurvivalToolsSettings.hardcoreMode) ? 0.67f : 1f)); // Once per hour of continuous work, or ~40 mins with hardcore

        public override bool ShouldShowFor(StatRequest req) =>
            ((BuildableDef)req.Def).IsSurvivalTool() && SurvivalToolsSettings.ToolDegradation;

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            SurvivalTool tool = req.Thing as SurvivalTool;
            return GetBaseEstimatedLifespan(tool, req.Def as BuildableDef);
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            SurvivalTool tool = req.Thing as SurvivalTool;
            return $"{"StatsReport_BaseValue".Translate()}: {GetBaseEstimatedLifespan(tool, req.Def as BuildableDef).ToString("F1")}";
        }

        private float GetBaseEstimatedLifespan(SurvivalTool tool, BuildableDef def)
        {
            SurvivalToolProperties props = def.GetModExtension<SurvivalToolProperties>() ?? SurvivalToolProperties.defaultValues;

            if (!((ThingDef)def).useHitPoints)
                return float.PositiveInfinity;

            // For def
            if (tool == null)
                return GenDate.TicksToDays(Mathf.RoundToInt((BaseWearInterval * def.GetStatValueAbstract(StatDefOf.MaxHitPoints)) / props.toolWearFactor));

            // For thing
            StuffPropsTool stuffProps = tool.Stuff?.GetModExtension<StuffPropsTool>() ?? StuffPropsTool.defaultValues;
            float wearFactor = tool.def.GetModExtension<SurvivalToolProperties>().toolWearFactor * (stuffProps.wearFactorMultiplier);
            return GenDate.TicksToDays(Mathf.RoundToInt((BaseWearInterval * tool.MaxHitPoints) / wearFactor));
        }

        public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
        {
            val /= SurvivalToolsSettings.ToolDegradationFactor;
            base.FinalizeValue(req, ref val, applyPostProcess);
        }

        public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
            StringBuilder finalBuilder = new StringBuilder();
            finalBuilder.AppendLine($"{"Settings_ToolDegradationRate".Translate()}: " +
                $"{(1 / SurvivalToolsSettings.ToolDegradationFactor).ToStringByStyle(ToStringStyle.FloatTwo, ToStringNumberSense.Factor)}");
            finalBuilder.AppendLine();
            finalBuilder.AppendLine(base.GetExplanationFinalizePart(req, numberSense, finalVal));
            return finalBuilder.ToString();
        }

    }
}

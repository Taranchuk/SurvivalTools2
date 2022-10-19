﻿using SettingsHelper;
using UnityEngine;
using Verse;

namespace SurvivalTools
{
    public class SurvivalToolsSettings : ModSettings
    {
        public static bool hardcoreMode = false;
        public static bool reduceNoToolWorkEfficiency = false;
        public static bool toolMapGen = true;
        public static bool toolLimit = true;
        private static float toolDegradationFactor = 1f;
        public static float ToolDegradationFactor => Mathf.Pow(toolDegradationFactor, (toolDegradationFactor < 1f) ? 1 : 2);
        public static bool ToolDegradation => toolDegradationFactor > 0f;
        public static bool toolOptimization = true;

        public void DoWindowContents(Rect wrect)
        {
            Listing_Standard options = new Listing_Standard();
            Color defaultColor = GUI.color;
            options.Begin(wrect);

            GUI.color = defaultColor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            options.Gap();
            // Same GUI colour as Merciless
            GUI.color = new Color(1f, 0.2f, 0.2f);
            options.CheckboxLabeled("Settings_HardcoreMode".Translate(), ref hardcoreMode, "Settings_HardcoreMode_Tooltip".Translate());
            GUI.color = defaultColor;
            options.Gap();
            options.CheckboxLabeled("Settings_ReduceNoToolWorkEfficiency".Translate(), ref reduceNoToolWorkEfficiency, "Settings_ReduceNoToolWorkEfficiency_Tooltip".Translate());
            options.Gap();
            options.CheckboxLabeled("Settings_ToolMapGen".Translate(), ref toolMapGen, "Settings_ToolMapGen_Tooltip".Translate());
            options.Gap();
            options.CheckboxLabeled("Settings_ToolLimit".Translate(), ref toolLimit, "Settings_ToolLimit_Tooltip".Translate());
            options.Gap();
            options.AddLabeledSlider("Settings_ToolDegradationRate".Translate(), ref toolDegradationFactor, 0f, 2f,
                rightAlignedLabel: ToolDegradationFactor.ToStringByStyle(ToStringStyle.FloatTwo, ToStringNumberSense.Factor), roundTo: 0.01f);
            options.Gap();
            options.CheckboxLabeled("Settings_ToolOptimization".Translate(), ref toolOptimization, "Settings_ToolOptimization_Tooltip".Translate());
            options.End();

            Mod.GetSettings<SurvivalToolsSettings>().Write();

        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref hardcoreMode, "hardcoreMode", false);
            Scribe_Values.Look(ref reduceNoToolWorkEfficiency, "reduceNoToolWorkEfficiency", false);
            Scribe_Values.Look(ref toolMapGen, "toolMapGen", true);
            Scribe_Values.Look(ref toolLimit, "toolLimit", true);
            Scribe_Values.Look(ref toolDegradationFactor, "toolDegradationFactor", 1f);
            Scribe_Values.Look(ref toolOptimization, "toolOptimization", true);
        }

    }

    public class SurvivalTools : Mod
    {
        public SurvivalToolsSettings settings;

        public SurvivalTools(ModContentPack content) : base(content)
        {
            GetSettings<SurvivalToolsSettings>();
        }

        public override string SettingsCategory()
        {
            return "SurvivalToolsSettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            GetSettings<SurvivalToolsSettings>().DoWindowContents(inRect);
        }

    }

}

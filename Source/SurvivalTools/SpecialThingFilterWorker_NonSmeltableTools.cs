﻿using Verse;

namespace SurvivalTools
{
    public class SpecialThingFilterWorker_NonSmeltableTools : SpecialThingFilterWorker
    {

        public override bool Matches(Thing t)
        {
            return CanEverMatch(t.def) && !t.Smeltable;
        }

        public override bool CanEverMatch(ThingDef def)
        {
            if (!def.IsSurvivalTool())
                return false;

            if (!def.thingCategories.NullOrEmpty())
                for (int i = 0; i < def.thingCategories.Count; i++)
                    for (ThingCategoryDef thingCategoryDef = def.thingCategories[i]; thingCategoryDef != null; thingCategoryDef = thingCategoryDef.parent)
                        if (thingCategoryDef == ST_ThingCategoryDefOf.SurvivalTools)
                            return true;

            return false;
        }

        public override bool AlwaysMatches(ThingDef def)
        {
            return CanEverMatch(def) && !def.smeltable && !def.MadeFromStuff;
        }

    }
}

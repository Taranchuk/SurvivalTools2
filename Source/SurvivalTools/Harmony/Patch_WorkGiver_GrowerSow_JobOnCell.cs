using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SurvivalTools
{

    [HarmonyPatch(typeof(WorkGiver_GrowerSow))]
    [HarmonyPatch(nameof(WorkGiver_GrowerSow.JobOnCell))]
    public static class Patch_WorkGiver_GrowerSow_JobOnCell
    {

        public static void Postfix(ref Job __result, Pawn pawn)
        {
            if (__result?.def == JobDefOf.CutPlant && __result.targetA.Thing.def.plant.IsTree)
            {
                if (pawn.MeetsWorkGiverStatRequirements(ST_WorkGiverDefOf.FellTrees.GetModExtension<WorkGiverExtension>().requiredStats))
                    __result = new Job(ST_JobDefOf.FellTree, __result.targetA);
                else
                    __result = null;
            }
        }

    }

}

using HarmonyLib;
using RimWorld;
using Verse;

namespace SurvivalTools
{

    public static class Patch_MassUtility
    {

        [HarmonyPatch(typeof(MassUtility), nameof(MassUtility.CountToPickUpUntilOverEncumbered))]
        public static class CountToPickUpUntilOverEncumbered
        {

            public static void Postfix(ref int __result, Pawn pawn, Thing thing)
            {
                if (__result > 0 && pawn.RaceProps.Humanlike && thing as SurvivalTool != null && !pawn.CanCarryAnyMoreSurvivalTools())
                    __result = 0;
            }

        }

        [HarmonyPatch(typeof(MassUtility), nameof(MassUtility.WillBeOverEncumberedAfterPickingUp))]
        public static class WillBeOverEncumberedAfterPickingUp
        {

            public static void Postfix(ref bool __result, Pawn pawn, Thing thing)
            {
                if (pawn.RaceProps.Humanlike && thing as SurvivalTool != null && !pawn.CanCarryAnyMoreSurvivalTools())
                    __result = true;
            }

        }

    }

}

using RimWorld;
using Verse;

namespace SurvivalTools
{

    // More copypasta
    public class JobDriver_HarvestTree_Designated : JobDriver_HarvestTree
    {
        protected override DesignationDef RequiredDesignation
        {
            get
            {
                return DesignationDefOf.HarvestPlant;
            }
        }
    }
}

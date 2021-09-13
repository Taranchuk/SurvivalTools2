using RimWorld;
using Verse;

namespace SurvivalTools
{

    // More copypasta
    public class JobDriver_FellTree_Designated : JobDriver_FellTree
    {
        protected override DesignationDef RequiredDesignation
        {
            get
            {
                return DesignationDefOf.CutPlant;
            }
        }
    }
}

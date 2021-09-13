using RimWorld;
using Verse.AI;

namespace SurvivalTools
{

    // More copypasta
    public class JobDriver_HarvestTree : JobDriver_PlantWork
    {
        protected override void Init()
        {
            this.xpPerTick = 0.085f;
        }

        protected override Toil PlantWorkDoneToil()
        {
            return Toils_General.RemoveDesignationsOnThing(TargetIndex.A, DesignationDefOf.HarvestPlant);
        }
    }
}

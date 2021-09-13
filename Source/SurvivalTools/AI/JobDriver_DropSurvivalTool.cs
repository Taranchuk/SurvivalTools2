using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SurvivalTools
{
    public class JobDriver_DropSurvivalTool : JobDriver
    {

        private const int DurationTicks = 30;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            yield return new Toil
            {
                initAction = () => pawn.pather.StopDead(),
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = DurationTicks
            };
            yield return new Toil
            {
                initAction = () =>
                {
                    if (!pawn.inventory.innerContainer.TryDrop(TargetThingA, pawn.Position, pawn.MapHeld, ThingPlaceMode.Near, out Thing tool))
                        EndJobWith(JobCondition.Incompletable);
                }
            };
        }

    }
}

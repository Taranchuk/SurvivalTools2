using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SurvivalTools
{

    // Decompiled vanilla copypasta

    public abstract class JobDriver_PlantWork : JobDriver
    {
        protected Plant Plant => (Plant)job.targetA.Thing;

        protected virtual DesignationDef RequiredDesignation => null;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            var target = job.GetTarget(TargetIndex.A);
            if (target.IsValid)
            {
                var pawn = this.pawn;
                var target2 = target;
                var job = this.job;
                if (!pawn.Reserve(target2, job, 1, -1, null, errorOnFailed))
                {
                    return false;
                }
            }
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.A), job, 1, -1, null);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Init();
            yield return Toils_JobTransforms.MoveCurrentTargetIntoQueue(TargetIndex.A);
            var initExtractTargetFromQueue = Toils_JobTransforms.ClearDespawnedNullOrForbiddenQueuedTargets(TargetIndex.A, (RequiredDesignation == null) ? null : new Func<Thing, bool>((Thing t) => Map.designationManager.DesignationOn(t, RequiredDesignation) != null));
            yield return initExtractTargetFromQueue;
            yield return Toils_JobTransforms.SucceedOnNoTargetInQueue(TargetIndex.A);
            yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A, true);
            var gotoThing = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).JumpIfDespawnedOrNullOrForbidden(TargetIndex.A, initExtractTargetFromQueue);
            if (RequiredDesignation != null)
            {
                gotoThing.FailOnThingMissingDesignation(TargetIndex.A, RequiredDesignation);
            }
            yield return gotoThing;
            var cut = new Toil();
            cut.tickAction = delegate ()
            {
                var actor = cut.actor;
                SurvivalToolUtility.TryDegradeTool(actor, ST_StatDefOf.TreeFellingSpeed);
                if (actor.skills != null)
                {
                    actor.skills.Learn(SkillDefOf.Plants, xpPerTick, false);
                }
                float statValue = actor.GetStatValue(ST_StatDefOf.TreeFellingSpeed, true);
                float num = statValue;
                var plant = Plant;
                num *= Mathf.Lerp(3.3f, 1f, plant.Growth);
                workDone += num;
                if (workDone >= plant.def.plant.harvestWork)
                {
                    if (plant.def.plant.harvestedThingDef != null)
                    {
                        if (actor.RaceProps.Humanlike && plant.def.plant.harvestFailable && Rand.Value > actor.GetStatValue(StatDefOf.PlantHarvestYield, true))
                        {
                            var loc = (pawn.DrawPos + plant.DrawPos) / 2f;
                            MoteMaker.ThrowText(loc, Map, "TextMote_HarvestFailed".Translate(), 3.65f);
                        }
                        else
                        {
                            int num2 = plant.YieldNow();
                            if (num2 > 0)
                            {
                                var thing = ThingMaker.MakeThing(plant.def.plant.harvestedThingDef, null);
                                thing.stackCount = num2;
                                if (actor.Faction != Faction.OfPlayer)
                                {
                                    thing.SetForbidden(true, true);
                                }
                                GenPlace.TryPlaceThing(thing, actor.Position, Map, ThingPlaceMode.Near, null, null);
                                actor.records.Increment(RecordDefOf.PlantsHarvested);
                            }
                        }
                    }
                    plant.def.plant.soundHarvestFinish.PlayOneShot(actor);
                    plant.PlantCollected(actor, PlantDestructionMode.Cut);
                    workDone = 0f;
                    ReadyForNextToil();
                    return;
                }
            };
            cut.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            if (RequiredDesignation != null)
            {
                cut.FailOnThingMissingDesignation(TargetIndex.A, RequiredDesignation);
            }
            cut.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            cut.defaultCompleteMode = ToilCompleteMode.Never;
            cut.WithEffect(EffecterDefOf.Harvest_Plant, TargetIndex.A);
            cut.WithProgressBar(TargetIndex.A, () => workDone / Plant.def.plant.harvestWork, true, -0.5f);
            cut.PlaySustainerOrSound(() => Plant.def.plant.soundHarvesting);
            cut.activeSkill = () => SkillDefOf.Plants;
            yield return cut;
            var plantWorkDoneToil = PlantWorkDoneToil();
            if (plantWorkDoneToil != null)
            {
                yield return plantWorkDoneToil;
            }
            yield return Toils_Jump.Jump(initExtractTargetFromQueue);
            yield break;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref workDone, "workDone", 0f, false);
        }

        protected virtual void Init()
        {
        }

        protected virtual Toil PlantWorkDoneToil()
        {
            return null;
        }

        private float workDone;

        protected float xpPerTick;

        protected const TargetIndex PlantInd = TargetIndex.A;
    }
}

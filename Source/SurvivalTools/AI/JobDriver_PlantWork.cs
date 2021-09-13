﻿using RimWorld;
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
        protected Plant Plant
        {
            get
            {
                return (Plant)this.job.targetA.Thing;
            }
        }

        protected virtual DesignationDef RequiredDesignation
        {
            get
            {
                return null;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            LocalTargetInfo target = this.job.GetTarget(TargetIndex.A);
            if (target.IsValid)
            {
                Pawn pawn = this.pawn;
                LocalTargetInfo target2 = target;
                Job job = this.job;
                if (!pawn.Reserve(target2, job, 1, -1, null, errorOnFailed))
                {
                    return false;
                }
            }
            this.pawn.ReserveAsManyAsPossible(this.job.GetTargetQueue(TargetIndex.A), this.job, 1, -1, null);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.Init();
            yield return Toils_JobTransforms.MoveCurrentTargetIntoQueue(TargetIndex.A);
            Toil initExtractTargetFromQueue = Toils_JobTransforms.ClearDespawnedNullOrForbiddenQueuedTargets(TargetIndex.A, (this.RequiredDesignation == null) ? null : new Func<Thing, bool>((Thing t) => this.Map.designationManager.DesignationOn(t, this.RequiredDesignation) != null));
            yield return initExtractTargetFromQueue;
            yield return Toils_JobTransforms.SucceedOnNoTargetInQueue(TargetIndex.A);
            yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A, true);
            Toil gotoThing = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).JumpIfDespawnedOrNullOrForbidden(TargetIndex.A, initExtractTargetFromQueue);
            if (this.RequiredDesignation != null)
            {
                gotoThing.FailOnThingMissingDesignation(TargetIndex.A, this.RequiredDesignation);
            }
            yield return gotoThing;
            Toil cut = new Toil();
            cut.tickAction = delegate ()
            {
                Pawn actor = cut.actor;
                SurvivalToolUtility.TryDegradeTool(actor, ST_StatDefOf.TreeFellingSpeed);
                if (actor.skills != null)
                {
                    actor.skills.Learn(SkillDefOf.Plants, this.xpPerTick, false);
                }
                float statValue = actor.GetStatValue(ST_StatDefOf.TreeFellingSpeed, true);
                float num = statValue;
                Plant plant = this.Plant;
                num *= Mathf.Lerp(3.3f, 1f, plant.Growth);
                this.workDone += num;
                if (this.workDone >= plant.def.plant.harvestWork)
                {
                    if (plant.def.plant.harvestedThingDef != null)
                    {
                        if (actor.RaceProps.Humanlike && plant.def.plant.harvestFailable && Rand.Value > actor.GetStatValue(StatDefOf.PlantHarvestYield, true))
                        {
                            Vector3 loc = (this.pawn.DrawPos + plant.DrawPos) / 2f;
                            MoteMaker.ThrowText(loc, this.Map, "TextMote_HarvestFailed".Translate(), 3.65f);
                        }
                        else
                        {
                            int num2 = plant.YieldNow();
                            if (num2 > 0)
                            {
                                Thing thing = ThingMaker.MakeThing(plant.def.plant.harvestedThingDef, null);
                                thing.stackCount = num2;
                                if (actor.Faction != Faction.OfPlayer)
                                {
                                    thing.SetForbidden(true, true);
                                }
                                GenPlace.TryPlaceThing(thing, actor.Position, this.Map, ThingPlaceMode.Near, null, null);
                                actor.records.Increment(RecordDefOf.PlantsHarvested);
                            }
                        }
                    }
                    plant.def.plant.soundHarvestFinish.PlayOneShot(actor);
                    plant.PlantCollected(actor);
                    this.workDone = 0f;
                    this.ReadyForNextToil();
                    return;
                }
            };
            cut.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            if (this.RequiredDesignation != null)
            {
                cut.FailOnThingMissingDesignation(TargetIndex.A, this.RequiredDesignation);
            }
            cut.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            cut.defaultCompleteMode = ToilCompleteMode.Never;
            cut.WithEffect(EffecterDefOf.Harvest_Plant, TargetIndex.A);
            cut.WithProgressBar(TargetIndex.A, () => this.workDone / this.Plant.def.plant.harvestWork, true, -0.5f);
            cut.PlaySustainerOrSound(() => this.Plant.def.plant.soundHarvesting);
            cut.activeSkill = (() => SkillDefOf.Plants);
            yield return cut;
            Toil plantWorkDoneToil = this.PlantWorkDoneToil();
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
            Scribe_Values.Look<float>(ref this.workDone, "workDone", 0f, false);
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

﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SurvivalTools
{
    [StaticConstructorOnStartup]
    internal static class HarmonyPatches
    {

        private static readonly Type patchType = typeof(HarmonyPatches);

        private static MethodInfo FindMakeNewToils(Type type)
        {
            return (from method in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).SelectMany(new Func<Type, IEnumerable<MethodInfo>>(AccessTools.GetDeclaredMethods))
                    where method.ReturnType == typeof(void)
                    where method.GetParameters().Length == 0
                    select method).First((MethodInfo method) => method.Name.Contains("MakeNewToils"));
        }
        static HarmonyPatches()
        {
            Harmony h = new Harmony("XeoNovaDan.SurvivalTools");
            //HarmonyInstance.DEBUG = true;
            // Automatic patches
            h.PatchAll(Assembly.GetExecutingAssembly());

            // Manual patches
            // Plants that obstruct construction zones
            HarmonyMethod postfixHandleBlockingThingJob = new HarmonyMethod(patchType, nameof(Postfix_HandleBlockingThingJob));
            h.Patch(AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.HandleBlockingThingJob)), postfix: postfixHandleBlockingThingJob);
            h.Patch(AccessTools.Method(typeof(RoofUtility), nameof(RoofUtility.HandleBlockingThingJob)), postfix: postfixHandleBlockingThingJob);

            // Mining JobDriver - ResetTicksToPickHit
            HarmonyMethod transpileResetTicksToPickHit = new HarmonyMethod(patchType, nameof(Transpile_JobDriver_Mine_ResetTicksToPickHit));
            h.Patch(AccessTools.Method(typeof(JobDriver_Mine), "ResetTicksToPickHit"), transpiler: transpileResetTicksToPickHit);

            // Thanks Mehni!
            if (!ModCompatibilityCheck.OtherInventoryModsActive)
                h.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), transpiler: new HarmonyMethod(patchType, nameof(Transpile_FloatMenuMakerMad_AddHumanlikeOrders)));

            #region JobDriver Boilerplate
            h.Patch(FindMakeNewToils(typeof(JobDriver_PlantWork)), transpiler: new HarmonyMethod(patchType, nameof(Transpile_JobDriver_PlantWork_MakeNewToils)));
            HarmonyMethod transpileMineMakeNewToils = new HarmonyMethod(patchType, nameof(Transpile_JobDriver_Mine_MakeNewToils));
            h.Patch(FindMakeNewToils(typeof(JobDriver_Mine)), transpiler: transpileMineMakeNewToils);
            h.Patch(FindMakeNewToils(typeof(JobDriver_ConstructFinishFrame)), transpiler: new HarmonyMethod(patchType, nameof(Transpile_JobDriver_ConstructFinishFrame_MakeNewToils)));
            h.Patch(FindMakeNewToils(typeof(JobDriver_Repair)), transpiler: new HarmonyMethod(patchType, nameof(Transpile_JobDriver_Repair_MakeNewToils)));
            h.Patch(AccessTools.Method(typeof(JobDriver_Deconstruct), "TickAction"),
                new HarmonyMethod(patchType, nameof(Prefix_JobDriver_Deconstruct_TickAction)));

            h.Patch(FindMakeNewToils(typeof(JobDriver_AffectRoof)), transpiler: new HarmonyMethod(patchType, nameof(Transpile_JobDriver_AffectRoof_MakeNewToils)));
            #endregion

            // Fluffy Breakdowns
            if (ModCompatibilityCheck.FluffyBreakdowns)
            {
                Type maintenanceDriver = GenTypes.GetTypeInAnyAssembly("Fluffy_Breakdowns.JobDriver_Maintenance", null);
                if (maintenanceDriver != null && typeof(JobDriver).IsAssignableFrom(maintenanceDriver))
                {
                    h.Patch(FindMakeNewToils(maintenanceDriver), transpiler: new HarmonyMethod(patchType, nameof(Transpile_JobDriver_Maintenance_MakeNewToils)));
                }
                else
                    Log.Error("Survival Tools - Could not find Fluffy_Breakdowns.JobDriver_Maintenance type to patch");
            }

            // Quarry
            if (ModCompatibilityCheck.Quarry)
            {
                Type quarryDriver = GenTypes.GetTypeInAnyAssembly("Quarry.JobDriver_MineQuarry", null);
                if (quarryDriver != null && typeof(JobDriver).IsAssignableFrom(quarryDriver))
                {
                    h.Patch(AccessTools.Method(quarryDriver, "Mine"), postfix: new HarmonyMethod(patchType, nameof(Postfix_JobDriver_MineQuarry_Mine)));
                    h.Patch(AccessTools.Method(quarryDriver, "ResetTicksToPickHit"), transpiler: new HarmonyMethod(patchType, nameof(Transpile_JobDriver_MineQuarry_ResetTicksToPickHit)));
                }
                else
                    Log.Error("Survival Tools - Could not find Quarry.JobDriver_MineQuarry type to patch");
            }

            // Turret Extensions
            if (ModCompatibilityCheck.TurretExtensions)
            {
                Type turretExtensionsDriver = GenTypes.GetTypeInAnyAssembly("TurretExtensions.JobDriver_UpgradeTurret", null);
                if (turretExtensionsDriver != null && typeof(JobDriver).IsAssignableFrom(turretExtensionsDriver))
                    h.Patch(AccessTools.Method(turretExtensionsDriver, "Upgrade"), postfix: new HarmonyMethod(patchType, nameof(Postfix_JobDriver_UpgradeTurret_Upgrade)));
                else
                    Log.Error("Survival Tools - Could not find TurretExtensions.JobDriver_UpgradeTurret type to patch");
            }

            // Combat Extended
            if (ModCompatibilityCheck.CombatExtended)
            {
                // Prevent tools from incorrectly being removed based on loadout
                Type combatExtendedHoldTrackerExcessThingClass = GenTypes.GetTypeInAnyAssembly("CombatExtended.Utility_HoldTracker", null);
                if (combatExtendedHoldTrackerExcessThingClass != null)
                    h.Patch(AccessTools.Method(combatExtendedHoldTrackerExcessThingClass, "GetExcessThing"), postfix: new HarmonyMethod(patchType, nameof(Postfix_CombatExtended_Utility_HoldTracker_GetExcessThing)));
                else
                    Log.Error("Survival Tools - Could not find CombatExtended.Utility_HoldTracker type to patch");
                // Prevent pawns from picking up excess tools with Combat Extended's CompInventory
                Type combatExtendedCompInventory = GenTypes.GetTypeInAnyAssembly("CombatExtended.CompInventory");

                if (combatExtendedCompInventory != null)
                {
                    var methodWithThing = AccessTools.GetDeclaredMethods(combatExtendedCompInventory).First(x => x.Name == "CanFitInInventory" && x.GetParameters()[0].ParameterType == typeof(Thing));
                    var methodWithThingDef = AccessTools.GetDeclaredMethods(combatExtendedCompInventory).First(x => x.Name == "CanFitInInventory" && x.GetParameters()[0].ParameterType == typeof(ThingDef));
                    h.Patch(methodWithThing, postfix: new HarmonyMethod(patchType, nameof(Postfix_CombatExtended_CompInventory_CanFitInInventoryThing)));
                    h.Patch(methodWithThingDef, postfix: new HarmonyMethod(patchType, nameof(Postfix_CombatExtended_CompInventory_CanFitInInventoryThingDef)));
                }
                else
                    Log.Error("Survival Tools - Could not find CombatExtended.CompInventory type to patch");
            }

            // Prison Labor
            if (ModCompatibilityCheck.PrisonLabor)
            {
                // Fix pickaxe degradation and use correct mining speed stat
                Type prisonLabourMineJobDriver = GenTypes.GetTypeInAnyAssembly("PrisonLabor.JobDriver_Mine_Tweak", null);
                if (prisonLabourMineJobDriver != null)
                {
                    h.Patch(AccessTools.Method(prisonLabourMineJobDriver, "ResetTicksToPickHit"), transpiler: transpileResetTicksToPickHit);
                    h.Patch(FindMakeNewToils(prisonLabourMineJobDriver), transpiler: transpileMineMakeNewToils);
                }
            }
        }

        public static void Postfix_HandleBlockingThingJob(ref Job __result, Pawn worker)
        {
            if (__result?.def == JobDefOf.CutPlant && __result.targetA.Thing.def.plant.IsTree)
            {
                if (worker.MeetsWorkGiverStatRequirements(ST_WorkGiverDefOf.FellTrees.GetModExtension<WorkGiverExtension>().requiredStats))
                    __result = new Job(ST_JobDefOf.FellTree, __result.targetA);
                else
                    __result = null;
            }
        }

        // Credit to goes Mehni for letting me use this. Thanks!
        public static IEnumerable<CodeInstruction> Transpile_FloatMenuMakerMad_AddHumanlikeOrders(IEnumerable<CodeInstruction> instructions)
        {

            MethodInfo playerHome = AccessTools.Property(typeof(Map), nameof(Map.IsPlayerHome)).GetGetMethod();
            List<CodeInstruction> instructionList = instructions.ToList();

            //instructionList.RemoveRange(instructions.FirstIndexOf(ci => ci.operand == playerHome) - 3, 5);
            //return instructionList;

            bool patched = false;

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if (!patched && instruction.operand == playerHome) // CE, Pick Up And Haul etc.
                //if (instructionList[i + 3].opcode == OpCodes.Callvirt && instruction.operand == playerHome)
                //if (instructionList[i + 3].operand == playerHome)
                {
                    {
                        instruction.opcode = OpCodes.Ldc_I4_0;
                        instruction.operand = null;
                        yield return instruction;
                        patched = true;
                    }
                    //    //{ instructionList[i + 5].labels = instruction.labels;}
                    //    instructionList.RemoveRange(i, 5);
                    //    patched = true;
                }
                yield return instruction;
            }
        }

        #region JobDriver Boilerplate

        // Using the transpiler-friendly overload
        private static MethodInfo TryDegradeTool = AccessTools.Method(typeof(SurvivalToolUtility), nameof(SurvivalToolUtility.TryDegradeTool), new[] { typeof(Pawn), typeof(StatDef) });

        #region Transpile_JobDriver_PlantWork_MakeNewToils
        public static IEnumerable<CodeInstruction> Transpile_JobDriver_PlantWork_MakeNewToils(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            FieldInfo plantHarvestingSpeed = AccessTools.Field(typeof(ST_StatDefOf), nameof(ST_StatDefOf.PlantHarvestingSpeed));

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldloc_0); // actor
                    yield return new CodeInstruction(OpCodes.Ldsfld, plantHarvestingSpeed); // ST_StatDefOf.PlantHarvestingSpeed
                    instruction = new CodeInstruction(OpCodes.Call, TryDegradeTool); // TryDegradeTool(actor, ST_StatDefOf.PlantHarvestingSpeed)
                }

                if (instruction.opcode == OpCodes.Ldsfld && instruction.operand == AccessTools.Field(typeof(StatDefOf), nameof(StatDefOf.PlantWorkSpeed)))
                {
                    instruction.operand = plantHarvestingSpeed;
                }

                yield return instruction;
            }
        }
        #endregion

        #region JobDriver_Mine
        public static IEnumerable<CodeInstruction> Transpile_JobDriver_Mine_MakeNewToils(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldloc_0); // actor
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ST_StatDefOf), nameof(ST_StatDefOf.DiggingSpeed))); // ST_StatDefOf.DiggingSpeed
                    instruction = new CodeInstruction(OpCodes.Call, TryDegradeTool); // TryDegradeTool(actor, ST_StatDefOf.DiggingSpeed)
                }

                yield return instruction;
            }
        }

        public static IEnumerable<CodeInstruction> Transpile_JobDriver_Mine_ResetTicksToPickHit(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Ldsfld && instruction.operand == AccessTools.Field(typeof(StatDefOf), nameof(StatDefOf.MiningSpeed)))
                {
                    instruction.operand = AccessTools.Field(typeof(ST_StatDefOf), nameof(ST_StatDefOf.DiggingSpeed));
                }

                yield return instruction;
            }
        }
        #endregion

        #region Construction JobDrivers

        private static FieldInfo ConstructionSpeed =>
            AccessTools.Field(typeof(StatDefOf), nameof(StatDefOf.ConstructionSpeed));

        #region Transpile_JobDriver_ConstructFinishFrame_MakeNewToils
        public static IEnumerable<CodeInstruction> Transpile_JobDriver_ConstructFinishFrame_MakeNewToils(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldsfld, ConstructionSpeed);
                    instruction = new CodeInstruction(OpCodes.Call, TryDegradeTool);
                }

                yield return instruction;
            }
        }
        #endregion
        #region Transpile_JobDriver_Repair_MakeNewToils
        public static IEnumerable<CodeInstruction> Transpile_JobDriver_Repair_MakeNewToils(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldsfld, ConstructionSpeed);
                    instruction = new CodeInstruction(OpCodes.Call, TryDegradeTool);
                }

                yield return instruction;
            }
        }
        #endregion
        #region Prefix_JobDriver_Deconstruct_TickAction
        public static void Prefix_JobDriver_Deconstruct_TickAction(JobDriver_Deconstruct __instance)
        {
            SurvivalToolUtility.TryDegradeTool(__instance.pawn, StatDefOf.ConstructionSpeed);
        }
        #endregion
        #region Transpile_JobDriver_AffectRoof_MakeNewToils
        public static IEnumerable<CodeInstruction> Transpile_JobDriver_AffectRoof_MakeNewToils(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            bool done = false;
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if (!done)
                {
                    yield return instruction;
                    yield return new CodeInstruction(instructionList[(i + 1)].opcode, instructionList[(i + 1)].operand);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(JobDriver), "pawn"));
                    yield return new CodeInstruction(OpCodes.Ldsfld, ConstructionSpeed);
                    yield return new CodeInstruction(OpCodes.Call, TryDegradeTool);
                    done = true;
                }
                yield return instruction;
            }
        }
        #endregion

        #endregion

        #region Modded JobDrivers

        #region Transpile_JobDriver_Maintenance_MakeNewToils
        public static IEnumerable<CodeInstruction> Transpile_JobDriver_Maintenance_MakeNewToils(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldsfld, ConstructionSpeed);
                    instruction = new CodeInstruction(OpCodes.Call, TryDegradeTool);
                }

                yield return instruction;
            }
        }
        #endregion

        #region Patch JobDriver_MineQuarry
        public static IEnumerable<CodeInstruction> Transpile_JobDriver_MineQuarry_ResetTicksToPickHit(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Ldsfld && instruction.operand == AccessTools.Field(typeof(StatDefOf), nameof(StatDefOf.MiningSpeed)))
                {
                    instruction.operand = AccessTools.Field(typeof(ST_StatDefOf), nameof(ST_StatDefOf.DiggingSpeed));
                }

                yield return instruction;
            }
        }

        public static void Postfix_JobDriver_MineQuarry_Mine(JobDriver __instance, Toil __result)
        {
            Action tickAction = __result.tickAction;
            Pawn pawn = __instance.pawn;
            __result.tickAction = () =>
            {
                SurvivalToolUtility.TryDegradeTool(pawn, ST_StatDefOf.DiggingSpeed);
                tickAction();
            };
            __result.defaultDuration = (int)Mathf.Clamp(3000f / pawn.GetStatValue(ST_StatDefOf.DiggingSpeed), 500f, 10000f);
        }
        #endregion

        #region Postfix_JobDriver_UpgradeTurret_Upgrade
        public static void Postfix_JobDriver_UpgradeTurret_Upgrade(JobDriver __instance, Toil __result)
        {
            Action tickAction = __result.tickAction;
            Pawn pawn = __instance.pawn;
            __result.tickAction = () =>
            {
                SurvivalToolUtility.TryDegradeTool(pawn, StatDefOf.ConstructionSpeed);
                tickAction();
            };
        }
        #endregion

        #endregion

        #endregion

        public static void Postfix_CombatExtended_Utility_HoldTracker_GetExcessThing(ref bool __result, Thing dropThing)
        {
            // If there's an excess thing to be dropped for automatic loadout fixing and that thing is a tool, don't treat it as an excess thing
            if (__result && dropThing as SurvivalTool != null)
                __result = false;
        }

        public static void Postfix_CombatExtended_CompInventory_CanFitInInventoryThing(ThingComp __instance, ref bool __result, Thing thing, ref int count)
        {
            // If the pawn could normally take an item to inventory - check if that item's a tool and obeys the pawn's carrying capacity
            if (__result && thing is SurvivalTool tool)
            {
                Pawn compParentPawn = __instance.parent as Pawn;
                if (compParentPawn != null && !compParentPawn.CanCarryAnyMoreSurvivalTools())
                {
                    count = 0;
                    __result = false;
                }
            }
        }
        public static void Postfix_CombatExtended_CompInventory_CanFitInInventoryThingDef(ThingComp __instance, ref bool __result, ThingDef thingDef, ref int count)
        {
            // If the pawn could normally take an item to inventory - check if that item's a tool and obeys the pawn's carrying capacity
            if (__result && typeof(SurvivalTool).IsAssignableFrom(thingDef.thingClass))
            {
                Pawn compParentPawn = __instance.parent as Pawn;
                if (compParentPawn != null && !compParentPawn.CanCarryAnyMoreSurvivalTools())
                {
                    count = 0;
                    __result = false;
                }
            }
        }
    }
}

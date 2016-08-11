using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse.AI;
using Verse;
using System.Reflection;

namespace ExtendedStorage
{
    internal class _Thing : Thing
    {
        internal static FieldInfo _thingStateInt;
        internal static ThingState GetThingStateInt(Thing thing)
        {
            if (_thingStateInt == null)
            {
                _thingStateInt = typeof(Thing).GetField("thingStateInt", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            return (ThingState)_thingStateInt.GetValue(thing);
        }
        internal static void SetThingStateInt(Thing thing, ThingState value)
        {
            if (_thingStateInt == null)
            {
                _thingStateInt = typeof(Thing).GetField("thingStateInt", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            _thingStateInt.SetValue(thing, value);
        }
        internal static MethodInfo mi_Notify_NoZoneOverlapThingSpawned;
        internal static void rNotify_NoZoneOverlapThingSpawned(Thing thing)
        {
            mi_Notify_NoZoneOverlapThingSpawned = typeof(ZoneManager).GetMethod("Notify_NoZoneOverlapThingSpawned", (BindingFlags)60); // public+nonpublic+instance+static
            mi_Notify_NoZoneOverlapThingSpawned.Invoke(Find.ZoneManager, new object[] { thing });
        }

        internal static MethodInfo mi_Notify_BarrierSpawned;
        internal static void rNotify_BarrierSpawned(Thing thing)
        {
            mi_Notify_BarrierSpawned = typeof(RegionDirtyer).GetMethod("Notify_BarrierSpawned", (BindingFlags)60); // public+nonpublic+instance+static
            mi_Notify_BarrierSpawned.Invoke(null, new object[] { thing });
        }

        internal void _SpawnSetup()
        {
            if (this.Destroyed)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Spawning destroyed thing ",
                    this,
                    " at ",
                    this.Position,
                    ". Correcting."
                }));
                SetThingStateInt(this, ThingState.Unspawned);
                if (this.HitPoints <= 0 && this.def.useHitPoints)
                {
                    this.HitPoints = 1;
                }
            }
            if (this.Spawned)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Tried to spawn already-spawned thing ",
                    this,
                    " at ",
                    this.Position
                }));
                return;
            }

            this.holder = null;
            SetThingStateInt(this, ThingState.Spawned);
            Find.Map.listerThings.Add(this);
            if (Find.TickManager != null)
            {
                Find.TickManager.RegisterAllTickabilityFor(this);
            }
            if (this.def.drawerType != DrawerType.RealtimeOnly)
            {
                CellRect.CellRectIterator iterator = this.OccupiedRect().GetIterator();
                while (!iterator.Done())
                {
                    Find.Map.mapDrawer.MapMeshDirty(iterator.Current, MapMeshFlag.Things);
                    iterator.MoveNext();
                }
            }
            if (this.def.drawerType != DrawerType.MapMeshOnly)
            {
                Find.DynamicDrawManager.RegisterDrawable(this);
            }
            if (this.def.hasTooltip)
            {
                Find.TooltipGiverList.RegisterTooltipGiver(this);
            }
            if (this.def.graphicData != null && this.def.graphicData.Linked)
            {
                LinkGrid.Notify_LinkerCreatedOrDestroyed(this);
                Find.MapDrawer.MapMeshDirty(this.Position, MapMeshFlag.Things, true, false);
            }
            if (!this.def.CanOverlapZones)
            {
                rNotify_NoZoneOverlapThingSpawned(this);
            }
            if (this.def.regionBarrier)
            {
                rNotify_BarrierSpawned(this);
            }
            if (this.def.pathCost != 0 || this.def.passability == Traversability.Impassable)
            {
                Find.PathGrid.RecalculatePerceivedPathCostUnderThing(this);
            }
            if (this.def.passability == Traversability.Impassable)
            {
                Reachability.ClearCache();
            }
            Find.CoverGrid.Register(this);
            if (this.def.category == ThingCategory.Item)
            {
                ListerHaulables.Notify_Spawned(this);
            }
            Find.AttackTargetsCache.Notify_ThingSpawned(this);
            Region validRegionAt_NoRebuild = Find.RegionGrid.GetValidRegionAt_NoRebuild(this.Position);
            Room room = (validRegionAt_NoRebuild != null) ? validRegionAt_NoRebuild.Room : null;
            if (room != null)
            {
                room.Notify_ContainedThingSpawnedOrDespawned(this);
            }
            if (this.def.category == ThingCategory.Item)
            {
                Building_Door building_Door = this.Position.GetEdifice() as Building_Door;
                if (building_Door != null)
                {
                    building_Door.Notify_ItemSpawnedOrDespawnedOnTop(this);
                }
            }
            StealAIDebugDrawer.Notify_ThingChanged(this);
        }
    }
}

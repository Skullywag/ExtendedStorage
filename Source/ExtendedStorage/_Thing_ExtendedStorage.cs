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
    internal class _Thing_ExtendedStorage : Thing
    {
        internal static FieldInfo _mapIndexOrState;
        internal static sbyte GetMapIndexOrState(Thing thing)
        {
            if (_mapIndexOrState == null)
            {
                _mapIndexOrState = typeof(Thing).GetField("mapIndexOrState", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            return (sbyte)_mapIndexOrState.GetValue(thing);
        }
        internal static void SetMapIndexOrState(Thing thing, sbyte value)
        {
            if (_mapIndexOrState == null)
            {
                _mapIndexOrState = typeof(Thing).GetField("mapIndexOrState", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            _mapIndexOrState.SetValue(thing, value);
        }
        internal static MethodInfo mi_Notify_NoZoneOverlapThingSpawned;
        internal static void rNotify_NoZoneOverlapThingSpawned(Thing thing, Map map)
        {
            mi_Notify_NoZoneOverlapThingSpawned = typeof(ZoneManager).GetMethod("Notify_NoZoneOverlapThingSpawned", (BindingFlags)60); // public+nonpublic+instance+static
            mi_Notify_NoZoneOverlapThingSpawned.Invoke(map.zoneManager, new object[] { thing });
        }

        internal static MethodInfo mi_Notify_BarrierSpawned;
        internal static void rNotify_BarrierSpawned(Thing thing, Map map)
        {
            mi_Notify_BarrierSpawned = typeof(RegionDirtyer).GetMethod("Notify_BarrierSpawned", (BindingFlags)60); // public+nonpublic+instance+static
            mi_Notify_BarrierSpawned.Invoke(map.regionDirtyer, new object[] { thing });
        }

        internal void _SpawnSetup(Map map)
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
                SetMapIndexOrState(this, -1);
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
            int num = Find.Maps.IndexOf(map);
            if (num < 0)
            {
                Log.Error("Tried to spawn thing " + this + ", but the map provided does not exist.");
                return;
            }
            this.holdingContainer = null;
            SetMapIndexOrState(this, (sbyte)num);
            this.Map.listerThings.Add(this);
            if (Find.TickManager != null)
            {
                Find.TickManager.RegisterAllTickabilityFor(this);
            }
            if (this.def.drawerType != DrawerType.RealtimeOnly)
            {
                CellRect.CellRectIterator iterator = this.OccupiedRect().GetIterator();
                while (!iterator.Done())
                {
                    this.Map.mapDrawer.MapMeshDirty(iterator.Current, MapMeshFlag.Things);
                    iterator.MoveNext();
                }
            }
            if (this.def.drawerType != DrawerType.MapMeshOnly)
            {
                this.Map.dynamicDrawManager.RegisterDrawable(this);
            }
            if (this.def.hasTooltip)
            {
                this.Map.tooltipGiverList.RegisterTooltipGiver(this);
            }
            if (this.def.graphicData != null && this.def.graphicData.Linked)
            {
                this.Map.linkGrid.Notify_LinkerCreatedOrDestroyed(this);
                this.Map.mapDrawer.MapMeshDirty(this.Position, MapMeshFlag.Things, true, false);
            }
            if (!this.def.CanOverlapZones)
            {
                rNotify_NoZoneOverlapThingSpawned(this, map);
            }
            if (this.def.regionBarrier)
            {
                rNotify_BarrierSpawned(this, map);
            }
            if (this.def.pathCost != 0 || this.def.passability == Traversability.Impassable)
            {
                this.Map.pathGrid.RecalculatePerceivedPathCostUnderThing(this);
            }
            if (this.def.passability == Traversability.Impassable)
            {
                this.Map.reachability.ClearCache();
            }
            this.Map.coverGrid.Register(this);
            if (this.def.category == ThingCategory.Item)
            {
                this.Map.listerHaulables.Notify_Spawned(this);
            }
            this.Map.attackTargetsCache.Notify_ThingSpawned(this);
            Region validRegionAt_NoRebuild = this.Map.regionGrid.GetValidRegionAt_NoRebuild(this.Position);
            Room room = (validRegionAt_NoRebuild != null) ? validRegionAt_NoRebuild.Room : null;
            if (room != null)
            {
                room.Notify_ContainedThingSpawnedOrDespawned(this);
            }
            if (this.def.category == ThingCategory.Item)
            {
                Building_Door building_Door = this.Position.GetEdifice(this.Map) as Building_Door;
                if (building_Door != null)
                {
                    building_Door.Notify_ItemSpawnedOrDespawnedOnTop(this);
                }
            }
            StealAIDebugDrawer.Notify_ThingChanged(this);
            if (this is IThingContainerOwner && Find.ColonistBar != null)
            {
                Find.ColonistBar.MarkColonistsDirty();
            }
        }
    }
}

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

        internal static MethodInfo mi_Notify_ThingAffectingRegionsSpawned;
        internal static void rNotify_ThingAffectingRegionsSpawned(Thing thing, Map map)
        {
            mi_Notify_ThingAffectingRegionsSpawned = typeof(RegionDirtyer).GetMethod("Notify_ThingAffectingRegionsSpawned", (BindingFlags)60); // public+nonpublic+instance+static
            mi_Notify_ThingAffectingRegionsSpawned.Invoke(map.regionDirtyer, new object[] { thing });
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
            SetMapIndexOrState(this, (sbyte)num);
            RegionListersUpdater.RegisterInRegions(this, map);
			if (!map.spawnedThings.TryAdd(this, false))
			{
				Log.Error("Couldn't add thing " + this + " to spawned things.");
			}
			map.listerThings.Add(this);
			map.thingGrid.Register(this);
            if (Find.TickManager != null)
            {
                Find.TickManager.RegisterAllTickabilityFor(this);
            }
            this.DirtyMapMesh(map);
			if (this.def.drawerType != DrawerType.MapMeshOnly)
			{
				map.dynamicDrawManager.RegisterDrawable(this);
			}
            map.tooltipGiverList.Notify_ThingSpawned(this);
            if (this.def.graphicData != null && this.def.graphicData.Linked)
            {
                map.linkGrid.Notify_LinkerCreatedOrDestroyed(this);
                map.mapDrawer.MapMeshDirty(this.Position, MapMeshFlag.Things, true, false);
            }
            if (!this.def.CanOverlapZones)
            {
                rNotify_NoZoneOverlapThingSpawned(this, map);
            }
            if (this.def.AffectsRegions)
			{
				rNotify_ThingAffectingRegionsSpawned(this, map);
			}
            if (this.def.pathCost != 0 || this.def.passability == Traversability.Impassable)
            {
                map.pathGrid.RecalculatePerceivedPathCostUnderThing(this);
            }
            if (this.def.passability == Traversability.Impassable)
            {
                map.reachability.ClearCache();
            }
            map.coverGrid.Register(this);
            if (this.def.category == ThingCategory.Item)
            {
                map.listerHaulables.Notify_Spawned(this);
            }
            map.attackTargetsCache.Notify_ThingSpawned(this);
            Region validRegionAt_NoRebuild = map.regionGrid.GetValidRegionAt_NoRebuild(this.Position);
            Room room = (validRegionAt_NoRebuild != null) ? validRegionAt_NoRebuild.Room : null;
            if (room != null)
            {
                room.Notify_ContainedThingSpawnedOrDespawned(this);
            }
            StealAIDebugDrawer.Notify_ThingChanged(this);
			if (this is IThingHolder && Find.ColonistBar != null)
			{
				Find.ColonistBar.MarkColonistsDirty();
			}
			if (this.def.category == ThingCategory.Item)
			{
				SlotGroup slotGroup = this.Position.GetSlotGroup(map);
				if (slotGroup != null && slotGroup.parent != null)
				{
					slotGroup.parent.Notify_ReceivedThing(this);
				}
			}
        }
    }
}

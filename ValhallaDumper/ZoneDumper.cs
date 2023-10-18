using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static MonoMod.InlineRT.MonoModRule;

namespace ValhallaDumper
{
    internal class ZoneDumper
    {
        static bool loaded = false;

        public static void RecurseObjectPrint(GameObject gameObject, uint depth)
        {
            String currentDepth = new String(' ', (int)depth * 2) + " - ";
            String nestedDepth = new String(' ', (int)(depth + 1) * 2) + " - ";
            
            ZLog.LogWarning(currentDepth + "name: " + gameObject.name);
            var monos = gameObject.GetComponents(typeof(MonoBehaviour));
            if (monos.Length > 0)
            {
                ZLog.LogWarning(currentDepth + "Monos: ");
                foreach (var mono in monos)
                {
                    ZLog.LogWarning(nestedDepth + mono.GetType().Name);
                }
            }

            int childCount = gameObject.transform.childCount;
            if (childCount > 0)
            {
                ZLog.LogWarning(currentDepth + "children: ");
                for (int i = 0; i < childCount; i++)
                {
                    var child = gameObject.transform.GetChild(i).gameObject;
                    RecurseObjectPrint(child, depth + 1);
                }
            }
            
        }

        [HarmonyPatch(typeof(ZoneSystem))]
        class ZoneSystemPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(ZoneSystem.Update))]
            static void UpdatePrefix(ref ZoneSystem __instance)
            {
                if (loaded || !DungeonDB.instance)
                    return;

                loaded = true;

                // we all love a lack of error messages dont we?
                //try
                //{
                    Dump(ref __instance);
                //} catch (Exception e)
                //{
                //    ZLog.LogError("Error while dumping: ");
                //    ZLog.LogError(e.StackTrace.ToString());
                //    ZLog.Log("L: " + new StackTrace(e, true).GetFrame(0).GetFileLineNumber());
                //}
            }

            static void Dump(ref ZoneSystem __instance) {

                /*
                foreach (var pair in __instance.m_locationsByHash)
                {
                    var zoneLocation = pair.Value;

                    Location componentInChildren = zoneLocation.m_location;
                    var interiorTransform = componentInChildren.m_interiorTransform;
                    if (interiorTransform && componentInChildren.m_generator) {
                        ZLog.Log(zoneLocation.m_prefabName);
                        ZLog.Log(" - m_generatorPosition: " + zoneLocation.m_generatorPosition);
                        ZLog.Log(" - m_interiorTransform: ");

                        ZLog.Log("   - .name: " + interiorTransform.name);
                        ZLog.Log("   - .parent.name: " + interiorTransform.parent.name);

                        ZLog.Log("   - .localPosition: " + interiorTransform.localPosition);
                        ZLog.Log("   - .localRotation: " + interiorTransform.localRotation);

                        int count = interiorTransform.childCount;
                        if (count > 0)
                        {
                            ZLog.Log("   - .children: ");
                            for (int i=0; i < count; i++)
                            {
                                ZLog.Log("     - " + interiorTransform.GetChild(i).name);
                            }
                        }                        
                    }
                }*/



                string comment = string.Format("{0:MM/dd/yyyy hh:mm.ss}", DateTime.Now);

                {
                    ZLog.Log("Dumping Prefabs");

                    // Prepare prefabs
                    List<GameObject> prefabs = new List<GameObject>();
                    foreach (var prefab in ZNetScene.instance.m_prefabs)
                    {
                        if (!prefabs.Contains(prefab))
                        {
                            if (prefab.GetComponent<ZNetView>())
                                prefabs.Add(prefab);
                            else
                                ZLog.LogError("Prefab missing ZNetView: " + prefab.name);
                        } else
                        {
                            ZLog.LogError("Prefab already queued: " + prefab.name);
                        }
                    }

                    var staticSolidRayMask = LayerMask.GetMask(new string[]
                    {
                        "static_solid",
                        "terrain"
                    });

                    // Dump prefabs
                    ZPackage pkg = new ZPackage();

                    pkg.Write(comment);
                    pkg.Write(Version.GetVersionString()); // write version for reference purposes
                    pkg.Write(prefabs.Count);
                    foreach (var prefab in prefabs)
                    {
                        var view = prefab.GetComponent<ZNetView>();

                        if ((prefab.layer & staticSolidRayMask) > 0)
                            ZLog.Log("staticSolid: " + prefab.name);

                        pkg.Write(view.GetPrefabName());
                        pkg.Write(view.gameObject.transform.localScale);

                        List<bool> flags = new List<bool>();

                        int type = (int)view.m_type;

                        flags.Add(view.m_syncInitialScale);
                        flags.Add(view.m_distant);
                        flags.Add(view.m_persistent);
                        flags.Add((type & 0b01) == 0b01);
                        flags.Add((type & 0b10) == 0b10);

                        flags.Add(prefab.GetComponent<Piece>()              != null);
                        flags.Add(prefab.GetComponent<Bed>()                != null);
                        flags.Add(prefab.GetComponent<Door>()               != null);
                        flags.Add(prefab.GetComponent<Chair>()              != null);
                        flags.Add(prefab.GetComponent<Ship>()               != null);
                        flags.Add(prefab.GetComponent<Fish>()               != null);   // Fish is also ItemDrop...
                        flags.Add(prefab.GetComponent<Plant>()              != null);
                        flags.Add(prefab.GetComponent<ArmorStand>()         != null);

                        flags.Add(prefab.GetComponent<Projectile>()         != null);
                        flags.Add(prefab.GetComponent<ItemDrop>()           != null);
                        flags.Add(prefab.GetComponent<Pickable>()           != null);
                        flags.Add(prefab.GetComponent<PickableItem>()       != null);

                        flags.Add(prefab.GetComponent<Container>()          != null);
                        flags.Add(prefab.GetComponent<CookingStation>()     != null);
                        flags.Add(prefab.GetComponent<CraftingStation>()    != null);
                        flags.Add(prefab.GetComponent<Smelter>()            != null);
                        flags.Add(prefab.GetComponent<Fireplace>()          != null);

                        flags.Add(prefab.GetComponent<WearNTear>()          != null);
                        flags.Add(prefab.GetComponent<Destructible>()       != null);
                        flags.Add(prefab.GetComponent<ItemStand>()          != null);

                        flags.Add(prefab.GetComponent<AnimalAI>()           != null);
                        flags.Add(prefab.GetComponent<MonsterAI>()          != null);
                        flags.Add(prefab.GetComponent<Tameable>()           != null);
                        flags.Add(prefab.GetComponent<Procreation>()        != null);

                        flags.Add(prefab.GetComponent<MineRock5>()          != null);                                                
                        flags.Add(prefab.GetComponent<TreeBase>()           != null);   // natural tree
                        flags.Add(prefab.GetComponent<TreeLog>()            != null);   // chopped down tree

                        flags.Add(prefab.GetComponent<DungeonGenerator>()   != null);
                        flags.Add(prefab.GetComponent<TerrainModifier>()    != null);
                        flags.Add(prefab.GetComponent<CreatureSpawner>()    != null);
                        flags.Add(prefab.GetComponent<ZSyncTransform>()     != null);

                        ulong mask = 0;
                        for (int i=0; i < flags.Count; i++)
                        {
                            mask |= flags[i] ? (ulong)1 << i : 0;
                        }

                        pkg.Write(mask);

                    }

                    File.WriteAllBytes("./dumped/prefabs.pkg", pkg.GetArray());

                    ZLog.Log("Dumped " + prefabs.Count + "/" + ZNetScene.instance.m_prefabs.Count + " prefabs");
                }



                //List<DungeonGenerator> dungeons = new List<DungeonGenerator>();
                //List<int> dungeons = new List<int>();

                //HashSet<int> dungeons = new HashSet<int>(); // Contains prefab hashes

                Dictionary<int, KeyValuePair<ZoneSystem.ZoneLocation, DungeonGenerator>> dungeons 
                    = new Dictionary<int, KeyValuePair<ZoneSystem.ZoneLocation, DungeonGenerator>>();

                {
                    ZLog.Log("Dumping ZoneLocations");

                    // Prepare locations
                    List<ZoneSystem.ZoneLocation> locations = new List<ZoneSystem.ZoneLocation>();
                    foreach (var loc in from a in ZoneSystem.instance.m_locations
                                        orderby a.m_prioritized descending
                                        select a
                    )
                    {
                        if (loc.m_enable)
                        {
                            if (loc.m_quantity > 0)
                            {
                                if (loc.m_prefab)
                                {
                                    if (loc.m_prefabName != loc.m_prefab.name)
                                        ZLog.LogWarning("ZoneLocation unequal names: " + loc.m_prefabName + ", " + loc.m_prefab.name);

                                    locations.Add(loc);
                                }
                                else
                                    ZLog.LogError("ZoneLocation missing prefab: " + loc.m_prefabName);
                            }
                            else
                                ZLog.LogError("ZoneLocation bad m_quantity: " + loc.m_prefabName + " " + loc.m_quantity);
                        }
                        else
                            ZLog.Log("Skipping dump of " + loc.m_prefabName);
                    }



                    // Dump locations
                    ZPackage pkg = new ZPackage();

                    pkg.Write(comment);
                    pkg.Write(Version.GetVersionString()); // write version for reference purposes
                    pkg.Write(locations.Count);
                    foreach (var loc in locations)
                    {
                        ZLog.Log("Dumping " + loc.m_prefabName);

                        //var generator = loc.m_prefab.GetComponent<Location>().m_generator;
                        //if (generator)
                        //dungeons.Add(loc.m_hash);

                        //if (generator)
                        //dungeons.Add(generator.gameObject.GetComponent<ZNetView>().GetPrefabName().GetStableHashCode());
                        //if (loc.m_location.m_generator)
                        //dungeons.Add(loc.m_hash);
                        //dungeons.Add(loc.m_location.m_generator);

                        ZLog.Log("  Basic data...");

                        loc.m_prefab.transform.position = Vector3.zero;
                        loc.m_prefab.transform.rotation = Quaternion.identity;
                        //if (loc.m_prefab.transform.localScale != ZNetScene.instance.)

                        pkg.Write(loc.m_prefabName);
                        //pkg.Write(loc.m_location.m_generator != null);
                        //pkg.Write(loc.m_location.m_generator ? loc.m_location.m_generator.gameObject.GetComponent<ZNetView>().GetPrefabName().GetStableHashCode() : 0);
                        ///pkg.Write(loc.m_prefab.name);       // m_prefab appears to be null
                        pkg.Write((int)loc.m_biome);
                        pkg.Write((int)loc.m_biomeArea);
                        pkg.Write(loc.m_location.m_applyRandomDamage);
                        pkg.Write(loc.m_centerFirst);
                        pkg.Write(loc.m_location.m_clearArea);
                        //pkg.Write(loc.m_location.m_useCustomInteriorTransform);
                        pkg.Write(loc.m_exteriorRadius);
                        pkg.Write(loc.m_interiorRadius);
                        pkg.Write(loc.m_forestTresholdMin);
                        pkg.Write(loc.m_forestTresholdMax);
                        //pkg.Write(loc.m_interiorPosition);
                        //pkg.Write(loc.m_generatorPosition);
                        pkg.Write(loc.m_group);
                        pkg.Write(loc.m_iconAlways);
                        pkg.Write(loc.m_iconPlaced);
                        pkg.Write(loc.m_inForest);
                        pkg.Write(loc.m_minAltitude);
                        pkg.Write(loc.m_maxAltitude);
                        pkg.Write(loc.m_minDistance);
                        pkg.Write(loc.m_maxDistance);
                        pkg.Write(loc.m_minTerrainDelta);
                        pkg.Write(loc.m_maxTerrainDelta);
                        pkg.Write(loc.m_minDistanceFromSimilar);
                        pkg.Write(loc.m_prioritized ? 200000 : 100000); // spawnAttempts
                        pkg.Write(loc.m_quantity);
                        pkg.Write(loc.m_randomRotation);

                        pkg.Write(loc.m_randomSpawns.Count);
                        foreach (var spawn in loc.m_randomSpawns)
                        {
                            pkg.Write(spawn.m_chanceToSpawn);

                            pkg.Write(spawn.m_childNetViews.Count);
                            foreach (var view in spawn.m_childNetViews)
                            {
                                pkg.Write(view.GetPrefabName().GetStableHashCode());

                                pkg.Write(view.transform.position);
                                pkg.Write(view.transform.rotation);
                            }
                            //pkg.Write((int)spawn.m_dungeonRequireTheme);
                            //pkg.Write((int)spawn.m_requireBiome);
                        }

                        // ?
                        //pkg.Write(69420);

                        pkg.Write(loc.m_slopeRotation);
                        pkg.Write(loc.m_snapToWater);
                        pkg.Write(loc.m_unique);

                        ZLog.Log("  NetViews...");

                        // set all active like in laceLocations()
                        foreach (var view in loc.m_netViews)
                        {
                            view.gameObject.SetActive(true);
                        }

                        List<ZNetView> views = new List<ZNetView>();
                        ZNetView.StartGhostInit();
                        foreach (var view in loc.m_netViews) // .m_prefab.GetComponent<Location>()
                        {
                            if (view.gameObject.activeSelf)
                            {
                                var obj = UnityEngine.Object.Instantiate<GameObject>(view.gameObject,
                                    view.gameObject.transform.position, view.gameObject.transform.rotation);

                                ZNetView nv = obj.GetComponent<ZNetView>();
                                views.Add(nv);

                                int hash = nv.GetPrefabName().GetStableHashCode();

                                var dg = obj.GetComponent<DungeonGenerator>();
                                if (dg && !dungeons.ContainsKey(hash))
                                    dungeons[hash] = new KeyValuePair<ZoneSystem.ZoneLocation, DungeonGenerator>(loc, dg);
                                //dungeons.Add(obj.GetComponent<ZNetView>().GetPrefabName().GetStableHashCode());
                            }
                        }

                        pkg.Write(views.Count);
                        foreach (var view in views)
                        {
                            pkg.Write(view.GetPrefabName().GetStableHashCode());
                            pkg.Write(view.m_zdo.m_position);
                            pkg.Write(Quaternion.Euler(view.m_zdo.m_rotation));
                        }

                        // free (not really needed)
                        //foreach (var view in views)
                            //UnityEngine.GameObject.Destroy(view.gameObject);

                        ZNetView.FinishGhostInit();
                    }

                    File.WriteAllBytes("./dumped/features.pkg", pkg.GetArray());

                    ZLog.Log("Dumped " + locations.Count + "/" + ZoneSystem.instance.m_locations.Count + " ZoneLocations");
                }



                {

                    ZLog.Log("Dumping RandomEvents");

                    var events = new List<RandomEvent>();
                    foreach (var e in RandEventSystem.instance.m_events)
                    {
                        if (e.m_enabled && e.m_random) {
                            events.Add(e);
                            ZLog.LogWarning(" - Including " + e.m_name);
                        } else
                            ZLog.LogWarning(" - Excluding " + e.m_name);
                    }

                    ZLog.LogWarning(" - m_eventIntervalMin: " + RandEventSystem.instance.m_eventIntervalMin);
                    ZLog.LogWarning(" - m_eventChance: " + RandEventSystem.instance.m_eventChance);
                    ZLog.LogWarning(" - m_randomEventRange: " + RandEventSystem.instance.m_randomEventRange);

                    // Dump dungeons
                    ZPackage pkg = new ZPackage();

                    pkg.Write(comment);
                    pkg.Write(Version.GetVersionString()); // write version for reference purposes
                    pkg.Write(dungeons.Count);

                    foreach (var e in events)
                    {
                        pkg.Write(e.m_name);
                        //pkg.Write(e.m_random); // redundant?
                        pkg.Write(e.m_duration);
                        pkg.Write(e.m_nearBaseOnly);
                        pkg.Write(e.m_pauseIfNoPlayerInArea);
                        pkg.Write((int)e.m_biome);

                        pkg.Write(e.m_requiredGlobalKeys.Count);
                        foreach (var key in e.m_requiredGlobalKeys) {
                            pkg.Write(key);
                        }

                        pkg.Write(e.m_notRequiredGlobalKeys.Count);
                        foreach (var key in e.m_notRequiredGlobalKeys)
                        {
                            pkg.Write(key);
                        }
                    }

                    File.WriteAllBytes("./dumped/randomEvents.pkg", pkg.GetArray());
                    ZLog.Log("Dumped " + events.Count + "/" + RandEventSystem.instance.m_events.Count + " RandomEvents");
                }



                {
                    ZLog.Log("Dumping Dungeons");

                    // Dump dungeons
                    ZPackage pkg = new ZPackage();

                    pkg.Write(comment);
                    pkg.Write(Version.GetVersionString()); // write version for reference purposes
                    pkg.Write(dungeons.Count);
                    foreach (var pair in dungeons)
                    {
                        //var loc = __instance.m_locationsByHash[hash];

                        var zoneLocation = pair.Value.Key;
                        var loc = zoneLocation.m_location;
                        var dungeon = pair.Value.Value;

                        pkg.Write(dungeon.GetComponent<ZNetView>().GetPrefabName());

                        pkg.Write(zoneLocation.m_interiorPosition);
                        pkg.Write(zoneLocation.m_generatorPosition);

                        if (loc.m_useCustomInteriorTransform)
                        {
                            //pkg.Write(loc.m_useCustomInteriorTransform);
                            //if (loc.m_interiorTransform && loc.m_generator)
                            {
                                //bool anyNull = zoneLocation.m_interiorPosition == null
                                //|| zoneLocation.m_generatorPosition == null;

                                //if (loc.m_useCustomInteriorTransform != anyNull)
                                //ZLog.LogError("hmm, m_useCustomInteriorTransform but positions are null");

                                //pkg.Write(loc.m_useCustomInteriorTransform);



                                ///ZLog.LogWarning(dungeon.name + " " 
                                ///    + zoneLocation.m_interiorPosition + " | " 
                                ///    + zoneLocation.m_generatorPosition);
                                ///
                                ///RecurseObjectPrint(loc.m_interiorTransform.gameObject, 0);

                                //var interior = loc.m_interiorTransform;

                                //var tf = dungeon.transform;

                                //ZLog.LogWarning(dungeon.name + " " + tf.localPosition + " " + tf.localRotation);

                                //pkg.Write(zoneLocation.m_interiorPosition);
                                //pkg.Write(zoneLocation.m_generatorPosition);

                                //bool anyEmpty = zoneLocation.m_interiorPosition == null || zoneLocation.m_generatorPosition == null
                                //    || zoneLocation.m_interiorPosition == Vector3.zero || zoneLocation.m_generatorPosition == Vector3.zero;
                                //
                                //if (loc.m_useCustomInteriorTransform == anyEmpty)
                                //    ZLog.LogError("Dungeon m_useCustomInteriorTransform mismatch with transform value");

                                //try
                                //{
                                //    ZLog.LogWarning("interior transform: ");
                                //    ZLog.LogWarning(" - name: " + loc.m_interiorTransform.name); // Dungeon name
                                //    ZLog.LogWarning(" - parent.name: " + loc.m_interiorTransform.parent.name); // Location name
                                //
                                //    ZLog.LogWarning(" - children: ");
                                //    int childCount1 = loc.m_interiorTransform.childCount;
                                //    for (int i = 0; i < childCount1; i++)
                                //    {
                                //        var child = loc.m_interiorTransform.GetChild(i);
                                //        var childCount2 = child.childCount;
                                //        ZLog.LogWarning("   - " + child.name + ":");
                                //        for (int j = 0; j < childCount2; j++)
                                //        {
                                //            var child2 = child.GetChild(j);
                                //            ZLog.LogWarning("     - " + child2.name);
                                //        }
                                //    }
                                //}
                                //catch (Exception e) { }
                            }
                        }



                        pkg.Write((int)dungeon.m_algorithm);
                        pkg.Write(dungeon.m_alternativeFunctionality);
                        pkg.Write(dungeon.m_campRadiusMax);
                        pkg.Write(dungeon.m_campRadiusMin);
                        pkg.Write(dungeon.m_doorChance);

                        pkg.Write(dungeon.m_doorTypes.Count);
                        foreach (var door in dungeon.m_doorTypes)
                        {
                            pkg.Write(door.m_prefab.GetComponent<ZNetView>().GetPrefabName().GetStableHashCode());
                            pkg.Write(door.m_connectionType);
                            pkg.Write(door.m_chance);
                        }

                        //ZLog.Log("dungeon.m_gridSize: " + dungeon.m_gridSize);

                        pkg.Write(dungeon.m_gridSize); // redundant? (only used for grid/meadows camp)
                        pkg.Write(dungeon.m_maxRooms);
                        pkg.Write(dungeon.m_maxTilt);
                        pkg.Write(dungeon.m_minAltitude);
                        pkg.Write(dungeon.m_minRequiredRooms);
                        pkg.Write(dungeon.m_minRooms);
                        //pkg.Write(dungeon.m_originalPosition); // TODO fix
                        pkg.Write(dungeon.m_perimeterBuffer);
                        pkg.Write(dungeon.m_perimeterSections);

                        pkg.Write(dungeon.m_requiredRooms.Count);
                        foreach (var room in dungeon.m_requiredRooms)
                        {
                            pkg.Write(room);
                        }

                        pkg.Write(dungeon.m_spawnChance);
                        pkg.Write((int)dungeon.m_themes);
                        pkg.Write(dungeon.m_tileWidth);
                        //pkg.Write(dungeon.m_useCustomInteriorTransform); // TODO fix

                        // Force dungeon to collect its rooms
                        dungeon.SetupAvailableRooms();

                        pkg.Write(DungeonGenerator.m_availableRooms.Count);
                        foreach (var roomData in DungeonGenerator.m_availableRooms)
                        {
                            var room = roomData.m_room;
                            var netviews = roomData.m_netViews;
                            var randomSpawns = roomData.m_randomSpawns;

                            /*
                            {
                                foreach (var view in netviews)
                                {
                                    view.gameObject.SetActive(false);
                                }

                                //var components = roomData.m_room.transform.GetComponents<MonoBehaviour>();
                                var components = roomData.m_room.transform.GetComponents(typeof(MonoBehaviour));

                                ZLog.Log(room.name);
                                ZLog.Log(" - Components: ");
                                foreach (var component in components)
                                {
                                    ZLog.Log("   - " + component.GetType().Name);
                                }

                                var childComponents = roomData.m_room.transform
                                    .GetComponentsInChildren(typeof(MonoBehaviour), false);

                                ZLog.Log(" - ChildComponents: ");
                                foreach (var component in childComponents)
                                {
                                    ZLog.Log("   - " + component.GetType().Name);
                                }

                                ZLog.Log(" - Children: ");
                                var childCount = roomData.m_room.transform.childCount;
                                for (int ic = 0; ic < childCount; ic++)
                                {
                                    var child = roomData.m_room.transform.GetChild(ic);

                                    ZLog.Log("   - " + child.name);
                                }
                            }*/

                            pkg.Write(Utils.GetPrefabName(room.gameObject));
                            pkg.Write(room.m_divider);
                            //pkg.Write(room.m_enabled);
                            pkg.Write(room.m_endCap);
                            pkg.Write(room.m_endCapPrio);
                            pkg.Write(room.m_entrance);
                            pkg.Write(room.m_faceCenter);
                            pkg.Write(room.m_minPlaceOrder);
                            //pkg.Write(room.m_musicPrefab)
                            pkg.Write(room.m_perimeter);

                            var connections = room.GetConnections();
                            pkg.Write(connections.Length);
                            foreach (var conn in connections)
                            {
                                pkg.Write(conn.m_type);
                                pkg.Write(conn.m_entrance);
                                pkg.Write(conn.m_allowDoor);
                                pkg.Write(conn.m_doorOnlyIfOtherAlsoAllowsDoor);

                                pkg.Write(conn.transform.localPosition);
                                pkg.Write(conn.transform.localRotation);
                            }

                            foreach (var view in netviews)
                            {
                                view.gameObject.SetActive(true);
                            }

                            // i have a suspicion that m_randomSpawns is directly tied to m_netViews
                            // ie. RandomSpawn enables/disables specific spawned m_netView instances in Room

                            List<ZNetView> views = new List<ZNetView>();
                            ZNetView.StartGhostInit();
                            foreach (var view in netviews) // .m_prefab.GetComponent<Location>()
                            {
                                if (view.gameObject.activeSelf)
                                {
                                    views.Add(view);
                                }
                            }

                            Quaternion quat = Quaternion.Inverse(room.transform.rotation);

                            pkg.Write(views.Count);
                            foreach (var view in views)
                            {
                                pkg.Write(view.GetPrefabName().GetStableHashCode());
                                //pkg.Write(view.m_zdo.m_position);
                                //pkg.Write(view.m_zdo.m_rotation);

                                // This writes the local transforms (as it should be)
                                //pkg.Write(quat * (view.m_zdo.m_position - room.transform.position));
                                //pkg.Write(quat * view.m_zdo.m_rotation);

                                pkg.Write(quat * (view.gameObject.transform.position - room.transform.position));
                                pkg.Write(quat * view.gameObject.transform.rotation);
                            }



                            // dump randomSpawns



                            pkg.Write(room.m_size);
                            pkg.Write((int)room.m_theme);
                            pkg.Write(room.m_weight);
                            pkg.Write(room.transform.position);
                            pkg.Write(room.transform.rotation);
                        }
                    }

                    File.WriteAllBytes("./dumped/dungeons.pkg", pkg.GetArray());

                    ZLog.Log("Dumped " + dungeons.Count + " dungeons");

                }



                {
                    ZLog.Log("Dumping Vegetation");



                    ZPackage pkg = new ZPackage();

                    List<ZoneSystem.ZoneVegetation> vegetation = new List<ZoneSystem.ZoneVegetation>();
                    foreach (var veg in ZoneSystem.instance.m_vegetation)
                    {
                        if (veg.m_enable)
                        {
                            if (veg.m_prefab && veg.m_prefab.GetComponent<ZNetView>())
                            {
                                vegetation.Add(veg);
                                ZLog.Log("Querying " + veg.m_prefab.name);
                            }
                            else
                                ZLog.LogError("Failed to query ZoneVegetation: " + veg.m_prefab.name);
                        }
                        else
                        {
                            ZLog.Log("Skipping query of " + veg.m_prefab.name);

                            // Most conflict anyways because lazy
                            //if (veg.m_name != veg.m_prefab.name)
                            //ZLog.LogWarning("ZoneVegetation unequal names: " + veg.m_name + ", " + veg.m_prefab.name);
                        }
                    }

                    var layers = Enumerable.Range(0, 31).Select(index => index + ": " + LayerMask.LayerToName(index)).Where(l => !string.IsNullOrEmpty(l)).ToList();

                    int BLOCK_MASK = __instance.m_blockRayMask;
                    int SOLID_MASK = __instance.m_solidRayMask;
                    int STATIC_MASK = __instance.m_staticSolidRayMask;
                    int TERRAIN_MASK = __instance.m_terrainRayMask;

                    ZLog.Log("Layers: \n - " + string.Join("\n - ", layers));

                    ZLog.Log("blockRayMask: " + Convert.ToString(BLOCK_MASK, 2));
                    ZLog.Log("solidRayMask: " + Convert.ToString(SOLID_MASK, 2));
                    ZLog.Log("staticSolidRayMask: " + Convert.ToString(STATIC_MASK, 2));
                    ZLog.Log("terrainRayMask: " + Convert.ToString(TERRAIN_MASK, 2));

                    // nano-sized tidbits (pickable mushrooms, seeds...)
                    // 0.28
                    HashSet<string> nanoRadius = new HashSet<string>(
                        new string[] {"Pickable_Thistle",
                            "Pickable_Mushroom",
                            "Pickable_Dandelion", "Pickable_Flint", "Pickable_Stone",
                            "Pickable_SeedTurnip", "Pickable_SeedCarrot",
                            "Pickable_Mushroom_Magecap", "Pickable_Mushroom_JotunPuffs",
                            "CloudberryBush", "Pickable_Branch",
                        }
                    );

                    // small bushes and small trees
                    // 0.70
                    HashSet<string> tinyRadius = new HashSet<string>(
                        new string[] {
                            "Beech_small1", "Beech_small2", "Bush01", "YggaShoot_small1", "shrub_2",
                            "Bush01_heath"
                        }
                    );

                    // 0.95
                    HashSet<string> smallRadius = new HashSet<string>(
                        new string[] {
                            "Beech1", "Birch1", "Birch1_aut", "Birch2",  "Birch2_aut",
                            "Pinetree_01", "MineRock_Obsidian"
                        }
                    );

                    // 1.10
                    HashSet<string> mildRadius = new HashSet<string>(
                        new string[] {
                            "FirTree", "Bush02_en", "FirTree_small_dead",
                            "FirTree_small",
                            "RaspberryBush", "BlueberryBush", "SwampTree1",
                            "StatueEvil", "MineRock_Tin"
                        }
                    );

                    // 2.20
                    HashSet<string> bigRadius = new HashSet<string>(
                        new string[] {
                             "Oak1", "SwampTree2",
                             "YggaShoot1", "YggaShoot2", "YggaShoot3", "stubbe"
                        }
                    );

                    Dictionary<string, float> customRadius = new Dictionary<string, float>();
                    customRadius.Add("cliff_mistlands1", 11);
                    customRadius.Add("cliff_mistlands1_creep", 11);
                    customRadius.Add("cliff_mistlands2", 11);
                    customRadius.Add("rock4_forest", 12);
                    customRadius.Add("rock4_copper", 12);
                    customRadius.Add("YggdrasilRoot", 9);
                    customRadius.Add("giant_helmet1", 7);
                    customRadius.Add("giant_helmet2", 5); // only front of helmet1
                    customRadius.Add("rock4_coast", 7);

                    // these are not heavily tested or measured
                    /*
                    customRadius.Add("rock4_coast", 13);
                    customRadius.Add("rock4_heath", 13);
                    customRadius.Add("HeathRockPillar", 4);
                    customRadius.Add("rock2_heath", 6);
                    customRadius.Add("rock3_mountain", 7);
                    customRadius.Add("rock1_mountain", 8);
                    customRadius.Add("rock2_mountain", 7);
                    customRadius.Add("SwampTree2", 1);
                    customRadius.Add("SwampTree2_log", 12); // very rectangular
                    customRadius.Add("FirTree_oldLog", 2);
                    customRadius.Add("stubbe", 1.2f);
                    customRadius.Add("StatueEvil", 1.2f);
                    customRadius.Add("shrub_2_heath", .7f);
                    customRadius.Add("shrub_2", .7f);
                    customRadius.Add("Leviathan", 30); // redundant since ocean mostly empty anyways
                    customRadius.Add("YggaShoot1", 3);
                    customRadius.Add("YggaShoot2", 4);
                    customRadius.Add("YggaShoot3", 3);
                    customRadius.Add("cliff_mistlands2", 9);
                    customRadius.Add("giant_helmet1", 6);
                    customRadius.Add("giant_helmet2", 4);
                    customRadius.Add("giant_sword1", 2);
                    customRadius.Add("giant_sword2", 1);
                    customRadius.Add("giant_ribs", 6);
                    
                    customRadius.Add("silvervein", 0);*/



                    pkg.Write(comment);
                    pkg.Write(Version.GetVersionString());
                    pkg.Write(vegetation.Count);
                    foreach (var veg in vegetation)
                    {
                        // test scale, confirm all are 1,1,1 (base scale)
                        //if (veg.m_prefab.transform.localScale != new Vector3(1, 1, 1))
                        //ZLog.LogWarning("Vegetation prefab localScale is not (1, 1, 1): " + veg.m_name + " " + veg.m_prefab.transform.localScale);

                        pkg.Write(veg.m_prefab.GetComponent<ZNetView>().GetPrefabName());
                        //pkg.Write(veg.m_prefab.name);
                        //pkg.Write(veg.m_name);
                        pkg.Write((int)veg.m_biome);
                        pkg.Write((int)veg.m_biomeArea);

                        GameObject obj = UnityEngine.GameObject.Instantiate<GameObject>(veg.m_prefab);

                        var collider = obj.GetComponent<Collider>();
                        if (!collider) collider = obj.GetComponentInChildren<Collider>();
                        if (collider)
                        {
                            // test whether layer has
                            //  "Default",
                            //  "static_solid",
                            //  "Default_small",
                            //  "piece"
                            var extents3 = collider.bounds.extents;
                            var extents2 = new Vector2(extents3.x, extents3.z);

                            float radius = extents2.magnitude; // = extents2.magnitude;

                            if ((collider.gameObject.layer & BLOCK_MASK) == 0)
                                radius = 0;

                            float temp = 0;
                            if (customRadius.TryGetValue(veg.m_prefab.name, out temp))
                                radius = temp;
                            else if (nanoRadius.Contains(veg.m_prefab.name))
                                radius = 0.28f;
                            else if (tinyRadius.Contains(veg.m_prefab.name))
                                radius = 0.70f;
                            else if (smallRadius.Contains(veg.m_prefab.name))
                                radius = 0.95f;
                            else if (mildRadius.Contains(veg.m_prefab.name))
                                radius = 1.10f;
                            else if (bigRadius.Contains(veg.m_prefab.name))
                                radius = 2.20f;

                            ZLog.Log("Dumping vegetation block layer " + veg.m_prefab.name + ", radius: " + radius);
                            pkg.Write(radius);

                            /*
                            if ((collider.gameObject.layer & BLOCK_MASK) > 0)
                            {

                                //radiusMap["mudpile_beacon"] = 0;
                                //radiusMap["MineRock_Tin"]

                                // "ice1" is part of deep north (NYI)

                                // .2 or less


                                pkg.Write((float)extents2.magnitude); // hopefully most vegetation is circular
                                ZLog.Log("Dumping vegetation block layer " + veg.m_prefab.name + ", radius: " + extents2.magnitude);
                            }
                            else
                            {
                                ZLog.LogWarning("Vegetation has no block mask " + veg.m_prefab.name + "(" + Convert.ToString(collider.gameObject.layer, 2) + "), radius: " + extents2.magnitude);
                                //var extents3 = collider.bounds.extents;
                                //var extents2 = new Vector2(extents3.x, extents3.z);
                                //pkg.Write((float)extents2.magnitude); // hopefully most vegetation is circular
                                pkg.Write(0.0f);
                            }*/
                        }
                        else
                        {
                            ZLog.LogWarning("Vegetation has no collider " + veg.m_prefab.name);
                            pkg.Write(0.0f);
                        }

                        UnityEngine.GameObject.Destroy(obj);

                        /*else
                        {
                            ZLog.LogWarning("Vegetation missing collider: " + veg.m_prefab.name);

                            var filter = veg.m_prefab.GetComponent<MeshFilter>();
                            if (!filter) filter = veg.m_prefab.GetComponentInChildren<MeshFilter>();
                            if (filter)
                            {
                                var mesh = filter.mesh;
                                var extents3 = mesh.bounds.extents;
                                var extents2 = new Vector2(extents3.x, extents3.z);
                                pkg.Write((float)extents2.magnitude); // hopefully most vegetation is circular
                            } else
                            {
                                ZLog.LogWarning("Vegetation missing meshfilter");
                                var renderer = veg.m_prefab.GetComponent<Renderer>();
                                if (!renderer) renderer = veg.m_prefab.GetComponentInChildren<Renderer>();
                                if (renderer)
                                {
                                    var extents3 = renderer.bounds.extents;
                                    var extents2 = new Vector2(extents3.x, extents3.z);
                                    pkg.Write((float)extents2.magnitude); // hopefully most vegetation is circular
                                } else
                                {
                                    ZLog.LogError("Vegetation missing renderer: " + veg.m_prefab.name);
                                    pkg.Write(0.15f);

                                    //ZLog.LogWarning("Vegetation missing renderer: " + veg.m_prefab.name);

                                    //var lod = veg.m_prefab.GetComponent<LODGroup>();
                                    //if (lod)
                                    //{
                                    //    
                                    //}
                                    //else
                                    //{
                                    //    ZLog.LogError("Vegetation complete fail: ")
                                    //    pkg.Write(0.15f);
                                    //}

                                }
                            }                            
                        }*/
                        pkg.Write(veg.m_min);
                        pkg.Write(veg.m_max);
                        pkg.Write(veg.m_minTilt);
                        pkg.Write(veg.m_maxTilt);
                        pkg.Write(veg.m_groupRadius);
                        pkg.Write(veg.m_forcePlacement);
                        pkg.Write(veg.m_groupSizeMin);
                        pkg.Write(veg.m_groupSizeMax);
                        pkg.Write(veg.m_scaleMin);
                        pkg.Write(veg.m_scaleMax);
                        pkg.Write(veg.m_randTilt);
                        pkg.Write(veg.m_blockCheck);
                        pkg.Write(veg.m_minAltitude);
                        pkg.Write(veg.m_maxAltitude);
                        pkg.Write(veg.m_minOceanDepth);
                        pkg.Write(veg.m_maxOceanDepth);
                        pkg.Write(veg.m_terrainDeltaRadius);
                        pkg.Write(veg.m_minTerrainDelta);
                        pkg.Write(veg.m_maxTerrainDelta);
                        pkg.Write(veg.m_inForest);
                        pkg.Write(veg.m_forestTresholdMin);
                        pkg.Write(veg.m_forestTresholdMax);
                        pkg.Write(veg.m_snapToWater);
                        pkg.Write(veg.m_snapToStaticSolid);
                        pkg.Write(veg.m_groundOffset);
                        pkg.Write(veg.m_chanceToUseGroundTilt);
                        pkg.Write(veg.m_minVegetation);
                        pkg.Write(veg.m_maxVegetation);

                        // offset by final transforms:
                        //var obj = UnityEngine.GameObject.Instantiate<GameObject>(veg.m_prefab)

                        //pkg.Write(v)
                    }

                    File.WriteAllBytes("./dumped/vegetation.pkg", pkg.GetArray());

                    ZLog.Log("Dumped " + vegetation.Count + "/" + ZoneSystem.instance.m_vegetation.Count + " ZoneVegetation");
                }

                // now dump starting keys?




            }
        }


    }
}

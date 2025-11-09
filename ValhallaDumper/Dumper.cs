using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;
using static MonoMod.InlineRT.MonoModRule;
using static UnityEngine.InputSystem.Layouts.InputControlLayout;

namespace ValhallaDumper
{
    internal class Dumper
    {
        static void LogInfo(object message)
        {
            var fmt = $"<color=#AA2266>[AVL]</color><color=#777777>{message}</color>";
            ZLog.Log(fmt);
            Chat.print(fmt);
        }

        static void LogWarning(object message) {
            var fmt = $"<color=#AA2266>[AVL]</color><color=#CC2222>{message}</color>";
            ZLog.Log(fmt);
            Chat.print(fmt);
        }

        static void LogError(object message) {
            var fmt = $"<color=#AA2266>[AVL]</color><color=#AA0000>{message}</color>";
            ZLog.Log(fmt);
            Chat.print(fmt);
        }

        public static void DumpDocs()
        {
            // Dump documentation
            Directory.CreateDirectory(ValhallaDumper.DOC_PATH);

            {
                /* * * * * * * * * * * * * * * * * * * * * 
                 * 
                 *
                 *      PREFAB DOCUMENTATION DUMPING
                 * 
                 * 
                 * * * * * * * * * * * * * * * * * * * * */

                StringBuilder builder = new StringBuilder();

                builder.Append("Documentation automatically generated from Valheim ").Append(Version.GetVersionString())
                    .AppendLine(",,");
                foreach (var pair in ZNetScene.instance.m_namedPrefabs)
                {
                    var obj = pair.Value;
                    ItemDrop item = obj.GetComponent<ItemDrop>();
                    HoverText hover = obj.GetComponent<HoverText>();
                    // name, hash
                    builder
                        .Append("\"")
                            .Append((obj.name)
                                + (item != null ? " (" + item.m_itemData.m_shared.m_name + ") (" + Localization.instance.Localize(item.m_itemData.m_shared.m_name) + ")" : ""))
                            .Append(hover != null ? (" (" + hover.m_text + ") (" + hover.GetHoverName() + ")") : "")
                        .Append("\", ")
                        .Append(pair.Key).Append(", ");

                    var components = obj.GetComponents<MonoBehaviour>();
                    if (components.Length > 0)
                    {
                        builder.Append("\"");
                        //foreach (var comp in components)
                        for (int i = 0; i < components.Length; i++)
                        {
                            var comp = components[i];
                            builder.Append(comp.GetType().ToString());
                            if (i < components.Length - 1)
                            {
                                builder.Append(", ");
                            }
                        }
                        builder.Append("\"");
                    }

                    // now components, followed by blank commas
                    builder.AppendLine();
                }

                File.WriteAllText(ValhallaDumper.DOC_PATH + "prefabs.csv", builder.ToString());
            }

            {
                /* * * * * * * * * * * * * * * * * * * * * 
                 * 
                 *
                 *      VERSION DOCUMENTATION DUMPING
                 * 
                 * 
                 * * * * * * * * * * * * * * * * * * * * */

                StringBuilder builder = new StringBuilder();

                builder.Append("Documentation automatically generated from Valheim ").Append(Version.GetVersionString())
                    .AppendLine(",,");

                builder.Append("Current version,").Append(Version.CurrentVersion.ToString()).AppendLine();
                builder.Append("Network version,").Append(Version.m_networkVersion).AppendLine();
                builder.Append("World version,").Append(Version.m_worldVersion).AppendLine();
                builder.Append("Worldgen version,").Append(Version.m_worldGenVersion).AppendLine();
                builder.Append("Location version,").Append(ZoneSystem.instance.m_locationVersion).AppendLine();
                builder.Append("Player version,").Append(Version.m_playerVersion).AppendLine();
                builder.Append("Player data version,").Append(Version.m_playerDataVersion).AppendLine();
                builder.Append("Item data version,").Append(Version.m_itemDataVersion).AppendLine();

                File.WriteAllText(ValhallaDumper.DOC_PATH + "version.csv", builder.ToString());
            }

            {
                // dump vegetation

            }

        }

        public static void DumpPackages()
        {
            /*
            foreach (var pair in __instance.m_locationsByHash)
            {
                var zoneLocation = pair.Value;

                Location componentInChildren = zoneLocation.m_location;
                var interiorTransform = componentInChildren.m_interiorTransform;
                if (interiorTransform && componentInChildren.m_generator) {
                    LogWarning(zoneLocation.m_prefabName);
                    LogWarning(" - m_generatorPosition: " + zoneLocation.m_generatorPosition);
                    LogWarning(" - m_interiorTransform: ");

                    LogWarning("   - .name: " + interiorTransform.name);
                    LogWarning("   - .parent.name: " + interiorTransform.parent.name);

                    LogWarning("   - .localPosition: " + interiorTransform.localPosition);
                    LogWarning("   - .localRotation: " + interiorTransform.localRotation);

                    int count = interiorTransform.childCount;
                    if (count > 0)
                    {
                        LogWarning("   - .children: ");
                        for (int i=0; i < count; i++)
                        {
                            LogWarning("     - " + interiorTransform.GetChild(i).name);
                        }
                    }
                }
            }*/

            Directory.CreateDirectory(ValhallaDumper.PKG_PATH);

            string comment = string.Format("{0:MM/dd/yyyy hh:mm.ss}", DateTime.Now);

            {
                // Prepare prefabs
                List<GameObject> prefabs = new List<GameObject>();
                foreach (var prefab in ZNetScene.instance.m_prefabs)
                {
                    if (!prefabs.Contains(prefab))
                    {
                        if (prefab.GetComponent<ZNetView>())
                        {
                            prefabs.Add(prefab);
                        }
                        else
                        {
                            LogError("Prefab missing ZNetView: " + prefab.name);
                        }
                    }
                    else
                    {
                        LogError("Duplicate prefab registered: " + prefab.name);
                    }
                }

                LogInfo("Dumping " + prefabs.Count + " Prefabs");

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

                    //if ((prefab.layer & staticSolidRayMask) > 0)
                        //LogWarning("staticSolid: " + prefab.name);

                    pkg.Write(view.GetPrefabName());
                    pkg.Write(view.gameObject.transform.localScale);

                    List<bool> flags = new List<bool>();

                    int type = (int)view.m_type;

                    flags.Add(view.m_syncInitialScale);
                    flags.Add(view.m_distant);
                    flags.Add(view.m_persistent);
                    flags.Add((type & 0b01) == 0b01);
                    flags.Add((type & 0b10) == 0b10);

                    flags.Add(prefab.GetComponent<Piece>() != null);
                    flags.Add(prefab.GetComponent<Bed>() != null);
                    flags.Add(prefab.GetComponent<Door>() != null);
                    flags.Add(prefab.GetComponent<Chair>() != null);
                    flags.Add(prefab.GetComponent<Ship>() != null);
                    flags.Add(prefab.GetComponent<Fish>() != null);   // Fish is also ItemDrop...
                    flags.Add(prefab.GetComponent<Plant>() != null);
                    flags.Add(prefab.GetComponent<ArmorStand>() != null);

                    flags.Add(prefab.GetComponent<Projectile>() != null);
                    flags.Add(prefab.GetComponent<ItemDrop>() != null);
                    flags.Add(prefab.GetComponent<Pickable>() != null);
                    flags.Add(prefab.GetComponent<PickableItem>() != null);

                    flags.Add(prefab.GetComponent<Container>() != null);
                    flags.Add(prefab.GetComponent<CookingStation>() != null);
                    flags.Add(prefab.GetComponent<CraftingStation>() != null);
                    flags.Add(prefab.GetComponent<Smelter>() != null);
                    flags.Add(prefab.GetComponent<Fireplace>() != null);

                    flags.Add(prefab.GetComponent<WearNTear>() != null);
                    flags.Add(prefab.GetComponent<Destructible>() != null);
                    flags.Add(prefab.GetComponent<ItemStand>() != null);

                    flags.Add(prefab.GetComponent<AnimalAI>() != null);
                    flags.Add(prefab.GetComponent<MonsterAI>() != null);
                    flags.Add(prefab.GetComponent<Tameable>() != null);
                    flags.Add(prefab.GetComponent<Procreation>() != null);

                    flags.Add(prefab.GetComponent<MineRock5>() != null);
                    flags.Add(prefab.GetComponent<TreeBase>() != null);   // natural tree
                    flags.Add(prefab.GetComponent<TreeLog>() != null);   // chopped down tree

                    flags.Add(prefab.GetComponent<DungeonGenerator>() != null);
                    flags.Add(prefab.GetComponent<TerrainModifier>() != null);
                    flags.Add(prefab.GetComponent<CreatureSpawner>() != null);
                    flags.Add(prefab.GetComponent<ZSyncTransform>() != null);

                    ulong mask = 0;
                    for (int i = 0; i < flags.Count; i++)
                    {
                        mask |= flags[i] ? (ulong)1 << i : 0;
                    }

                    pkg.Write(mask);

                }

                File.WriteAllBytes(ValhallaDumper.PKG_PATH + "prefabs.pkg", pkg.GetArray());

                var message = "Dumped " + prefabs.Count + "/" + ZNetScene.instance.m_prefabs.Count + " prefabs";
                LogWarning(message);
                Chat.print(message);
            }



            //List<DungeonGenerator> dungeons = new List<DungeonGenerator>();
            //List<int> dungeons = new List<int>();

            //HashSet<int> dungeons = new HashSet<int>(); // Contains prefab hashes

            Dictionary<int, KeyValuePair<ZoneSystem.ZoneLocation, DungeonGenerator>> dungeons
                = new Dictionary<int, KeyValuePair<ZoneSystem.ZoneLocation, DungeonGenerator>>();

            {
                LogInfo("Dumping ZoneLocations");

                // apparently called already...
                //__instance.SetupLocations();



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
                            if (loc.m_prefab.IsValid)
                            {
                                // prepare
                                //var zviews = Utils.GetEnabledComponentsInChildren<ZNetView>(loc.m_prefab.Asset);
                                loc.m_prefab.Load();

                                //var obj = loc.m_prefab?.Asset.name ?? null;
                                if (loc.m_prefabName
                                    != loc.m_prefab.Asset.name)
                                {
                                    LogWarning("ZoneLocation unequal names: " + loc.m_prefabName + ", " + loc.m_prefab.Asset.name);
                                }

                                locations.Add(loc);
                            }
                            else
                                LogError("ZoneLocation missing prefab: " + loc.m_prefabName);
                        }
                        else
                            LogError("ZoneLocation bad m_quantity: " + loc.m_prefabName + " " + loc.m_quantity);
                    }
                    else
                        LogWarning("Skipping dump of " + loc.m_prefabName);
                }



                // Dump locations
                ZPackage pkg = new ZPackage();

                pkg.Write(comment);
                pkg.Write(Version.GetVersionString()); // write version for reference purposes
                pkg.Write(locations.Count);
                foreach (var zloc in locations)
                {
                    LogInfo("Dumping " + zloc.m_prefabName);

                    //var generator = loc.m_prefab.GetComponent<Location>().m_generator;
                    //if (generator)
                    //dungeons.Add(loc.m_hash);

                    //if (generator)
                    //dungeons.Add(generator.gameObject.GetComponent<ZNetView>().GetPrefabName().GetStableHashCode());
                    //if (loc.m_location.m_generator)
                    //dungeons.Add(loc.m_hash);
                    //dungeons.Add(loc.m_location.m_generator);

                    LogInfo("  Basic data...");

                    zloc.m_prefab.Asset.transform.position = Vector3.zero;
                    zloc.m_prefab.Asset.transform.rotation = Quaternion.identity;
                    //if (loc.m_prefab.transform.localScale != ZNetScene.instance.)

                    var loc = zloc.m_prefab.Asset.GetComponent<Location>();

                    pkg.Write(zloc.m_prefabName);
                    //pkg.Write(loc.m_location.m_generator != null);
                    //pkg.Write(loc.m_location.m_generator ? loc.m_location.m_generator.gameObject.GetComponent<ZNetView>().GetPrefabName().GetStableHashCode() : 0);
                    ///pkg.Write(loc.m_prefab.name);       // m_prefab appears to be null
                    pkg.Write((int)zloc.m_biome);
                    pkg.Write((int)zloc.m_biomeArea);
                    pkg.Write(loc.m_applyRandomDamage);
                    pkg.Write(zloc.m_centerFirst);
                    pkg.Write(loc.m_clearArea);
                    //pkg.Write(loc.m_location.m_useCustomInteriorTransform);
                    pkg.Write(zloc.m_exteriorRadius);
                    pkg.Write(zloc.m_interiorRadius);
                    pkg.Write(zloc.m_forestTresholdMin);
                    pkg.Write(zloc.m_forestTresholdMax);
                    //pkg.Write(loc.m_interiorPosition);
                    //pkg.Write(loc.m_generatorPosition);
                    pkg.Write(zloc.m_group);
                    pkg.Write(zloc.m_iconAlways);
                    pkg.Write(zloc.m_iconPlaced);
                    pkg.Write(zloc.m_inForest);
                    pkg.Write(zloc.m_minAltitude);
                    pkg.Write(zloc.m_maxAltitude);
                    pkg.Write(zloc.m_minDistance);
                    pkg.Write(zloc.m_maxDistance);
                    pkg.Write(zloc.m_minTerrainDelta);
                    pkg.Write(zloc.m_maxTerrainDelta);
                    pkg.Write(zloc.m_minDistanceFromSimilar);
                    pkg.Write(zloc.m_prioritized ? 200000 : 100000); // spawnAttempts
                    pkg.Write(zloc.m_quantity);
                    pkg.Write(zloc.m_randomRotation);

                    var randomSpawns = zloc.m_prefab.Asset.GetComponentsInChildren<RandomSpawn>();

                    pkg.Write(randomSpawns.Length);
                    foreach (var spawn in randomSpawns)
                    {
                        spawn.Prepare();



                        // chance
                        pkg.Write(spawn.m_chanceToSpawn);

                        // theme
                        pkg.Write((int)(spawn.m_dungeonRequireTheme));

                        // biome
                        pkg.Write((int)(spawn.m_requireBiome));

                        // lava?
                        pkg.Write(spawn.m_notInLava);

                        // elevation min
                        pkg.Write(spawn.m_minElevation);

                        // elevation max
                        pkg.Write(spawn.m_maxElevation);



                        //pkg.Write(spawn.m_chanceToSpawn);

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

                    pkg.Write(zloc.m_slopeRotation);
                    pkg.Write(zloc.m_snapToWater);
                    pkg.Write(zloc.m_unique);

                    LogInfo("  NetViews...");

                    // set all active like in laceLocations()
                    var netViews = zloc.m_prefab.Asset.GetComponentsInChildren<ZNetView>();
                    foreach (var view in netViews)
                    {
                        view.gameObject.SetActive(true);
                    }

                    List<ZNetView> views = new List<ZNetView>();
                    ZNetView.StartGhostInit();
                    foreach (var view in netViews) // .m_prefab.GetComponent<Location>()
                    {
                        if (view.gameObject.activeSelf)
                        {
                            var obj = UnityEngine.Object.Instantiate<GameObject>(view.gameObject,
                                view.gameObject.transform.position, view.gameObject.transform.rotation);

                            ZNetView nv = obj.GetComponent<ZNetView>();
                            views.Add(nv);

                            int hash = nv.GetPrefabName().GetStableHashCode();

                            var dungeon = obj.GetComponent<DungeonGenerator>();
                            if (dungeon && !dungeons.ContainsKey(hash))
                            {
                                dungeons[hash] = new KeyValuePair<ZoneSystem.ZoneLocation, DungeonGenerator>(zloc, dungeon);
                                //dungeons.Add(obj.GetComponent<ZNetView>().GetPrefabName().GetStableHashCode());
                            }
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

                File.WriteAllBytes(ValhallaDumper.PKG_PATH + "features.pkg", pkg.GetArray());

                LogInfo("Dumped " + locations.Count + "/" + ZoneSystem.instance.m_locations.Count + " ZoneLocations");
            }



            {

                LogInfo("Dumping RandomEvents");

                var events = new List<RandomEvent>();
                foreach (var e in RandEventSystem.instance.m_events)
                {
                    if (e.m_enabled && e.m_random)
                    {
                        events.Add(e);
                        LogWarning(" - Including " + e.m_name);
                    }
                    else
                    {
                        LogWarning(" - (Excluding) " + e.m_name);
                    }
                }

                //LogWarning(" - m_eventIntervalMin: " + RandEventSystem.instance.m_eventIntervalMin);
                //LogWarning(" - m_eventChance: " + RandEventSystem.instance.m_eventChance);
                //LogWarning(" - m_randomEventRange: " + RandEventSystem.instance.evm_randomEventRange);

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
                    foreach (var key in e.m_requiredGlobalKeys)
                    {
                        pkg.Write(key);
                    }

                    pkg.Write(e.m_notRequiredGlobalKeys.Count);
                    foreach (var key in e.m_notRequiredGlobalKeys)
                    {
                        pkg.Write(key);
                    }
                }

                File.WriteAllBytes(ValhallaDumper.PKG_PATH + "randomEvents.pkg", pkg.GetArray());
                LogInfo("Dumped " + events.Count + "/" + RandEventSystem.instance.m_events.Count + " RandomEvents");
            }

            // To disable saving this broken world
            ZoneSystem.instance.m_didZoneTest = true;

            {
                LogInfo("Dumping " + dungeons.Count + " Dungeons");

                // Dump dungeons
                ZPackage pkg = new ZPackage();

                pkg.Write(comment);
                pkg.Write(Version.GetVersionString()); // write version for reference purposes
                pkg.Write(dungeons.Count);
                foreach (var pair in dungeons)
                {
                    //var loc = __instance.m_locationsByHash[hash];

                    var zoneLocation = pair.Value.Key;

                    var prefab = zoneLocation.m_prefab;
                    prefab.Load();
                    ZNetView[] enabledComponentsInChildren = Utils.GetEnabledComponentsInChildren<ZNetView>(prefab.Asset);
                    RandomSpawn[] enabledComponentsInChildren2 = Utils.GetEnabledComponentsInChildren<RandomSpawn>(prefab.Asset);

                    for (int i = 0; i < enabledComponentsInChildren2.Length; i++)
                    {
                        enabledComponentsInChildren2[i].Prepare();
                    }

                    LogInfo(" - " + zoneLocation.m_name + " (" + prefab.Asset.name + ")");

                    var loc = prefab.Asset.GetComponent<Location>()!;

                    //var dungeon = prefab.Asset.GetComponent<DungeonGenerator>()!;
                    var dungeon = pair.Value.Value!; // we use the instanced generator, not the template one
                    var name = prefab.Asset.name ?? Utils.GetPrefabName(prefab.Asset);
                    //var dungeon = pair
                    //.Value
                    //.Value;

                    //{
                    //
                    //
                    //    Vector3 vector = Vector3.zero;
                    //    Vector3 vector2 = Vector3.zero;
                    //    if (loc.m_interiorTransform)
                    //    {
                    //        vector = loc.m_interiorTransform.localPosition;
                    //        vector2 = loc.m_generator.transform.localPosition;
                    //        pkg.Write(vector);
                    //        pkg.Write()
                    //    }
                    //}

                    //pkg.Write(dungeon.GetComponent<ZNetView>().GetPrefabName());
                    pkg.Write(name);
                    //pkg.Write(loc.m_interiorTransform
                    //    ?.localPosition ?? Vector3.zero); // m_interiorPosition);
                    //pkg.Write(loc.m_generator.transform
                    //    .localPosition); // zoneLocation.m_generatorPosition);

                    bool useTransform = loc.m_useCustomInteriorTransform && loc.m_interiorTransform && loc.m_generator;
                    pkg.Write(useTransform);
                    if (useTransform)
                    {                        
                        //if (loc.m_interiorTransform && loc.m_generator)
                        {
                            //bool anyNull = zoneLocation.m_interiorPosition == null
                            //|| zoneLocation.m_generatorPosition == null;

                            //if (loc.m_useCustomInteriorTransform != anyNull)
                            //LogError("hmm, m_useCustomInteriorTransform but positions are null");

                            //pkg.Write(loc.m_useCustomInteriorTransform);



                            ///LogWarning(dungeon.name + " " 
                            ///    + zoneLocation.m_interiorPosition + " | " 
                            ///    + zoneLocation.m_generatorPosition);
                            ///
                            ///RecurseObjectPrint(loc.m_interiorTransform.gameObject, 0);

                            //var interior = loc.m_interiorTransform;

                            //var tf = dungeon.transform;

                            //LogWarning(dungeon.name + " " + tf.localPosition + " " + tf.localRotation);

                            pkg.Write(loc.m_interiorTransform.localPosition);
                            pkg.Write(loc.m_interiorTransform.localRotation);
                            pkg.Write(loc.m_generator.transform.localPosition);
                            //pkg.Write(zoneLocation.m_generatorPosition);

                            //bool anyEmpty = zoneLocation.m_interiorPosition == null || zoneLocation.m_generatorPosition == null
                            //    || zoneLocation.m_interiorPosition == Vector3.zero || zoneLocation.m_generatorPosition == Vector3.zero;
                            //
                            //if (loc.m_useCustomInteriorTransform == anyEmpty)
                            //    LogError("Dungeon m_useCustomInteriorTransform mismatch with transform value");

                            //try
                            //{
                            //    LogWarning("interior transform: ");
                            //    LogWarning(" - name: " + loc.m_interiorTransform.name); // Dungeon name
                            //    LogWarning(" - parent.name: " + loc.m_interiorTransform.parent.name); // Location name
                            //
                            //    LogWarning(" - children: ");
                            //    int childCount1 = loc.m_interiorTransform.childCount;
                            //    for (int i = 0; i < childCount1; i++)
                            //    {
                            //        var child = loc.m_interiorTransform.GetChild(i);
                            //        var childCount2 = child.childCount;
                            //        LogWarning("   - " + child.name + ":");
                            //        for (int j = 0; j < childCount2; j++)
                            //        {
                            //            var child2 = child.GetChild(j);
                            //            LogWarning("     - " + child2.name);
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
                        var doorPrefab = door.m_prefab;
                        pkg.Write(doorPrefab.GetComponent<ZNetView>().GetPrefabName().GetStableHashCode());
                        pkg.Write(door.m_connectionType);
                        pkg.Write(door.m_chance);
                    }

                    //LogWarning("dungeon.m_gridSize: " + dungeon.m_gridSize);

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
                        roomData.m_prefab.Load();

                        var room = roomData.RoomInPrefab;
                        var netviews = roomData.m_prefab.Asset.GetComponentsInChildren<ZNetView>();
                        // TODO, do we grab OEM ranSpawns, or use the copied/Instantiated ones?
                        //var randomSpawns = roomData.m_prefab.Asset.GetComponentsInChildren<RandomSpawn>();

                        /*
                        {
                            foreach (var view in netviews)
                            {
                                view.gameObject.SetActive(false);
                            }

                            //var components = roomData.m_room.transform.GetComponents<MonoBehaviour>();
                            var components = roomData.m_room.transform.GetComponents(typeof(MonoBehaviour));

                            LogWarning(room.name);
                            LogWarning(" - Components: ");
                            foreach (var component in components)
                            {
                                LogWarning("   - " + component.GetType().Name);
                            }

                            var childComponents = roomData.m_room.transform
                                .GetComponentsInChildren(typeof(MonoBehaviour), false);

                            LogWarning(" - ChildComponents: ");
                            foreach (var component in childComponents)
                            {
                                LogWarning("   - " + component.GetType().Name);
                            }

                            LogWarning(" - Children: ");
                            var childCount = roomData.m_room.transform.childCount;
                            for (int ic = 0; ic < childCount; ic++)
                            {
                                var child = roomData.m_room.transform.GetChild(ic);

                                LogWarning("   - " + child.name);
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

                        /*
                         * TODO
                         *  *very important
                         *  
                         *  RandomSpawns must be dumped IN-ORDER SPECIFIC,
                         *      because of deterministic UnityRandom
                         */

                        // ensure every RandomSpawn has no children
                        {
                            var randomSpawns = roomData.m_prefab.Asset.GetComponentsInChildren<RandomSpawn>();

                            foreach (var randomSpawn in randomSpawns)
                            {
                                //var sub = randomSpawn.GetComponentsInChildren<ZNetView>

                                if (randomSpawn.transform.childCount != 0)
                                {
                                    // This WILL print (I hate unity
                                    //LogWarning("Unexpected children (but expected apparently): " + randomSpawn.transform.parent.gameObject.name);
                                }
                            }

                            //var childCount = roomData.m_prefab.Asset.transform.childCount;
                        }

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

                            // Write RandomSpawns for attached prefab here

                            // Or attach by index (then use later)

                            var spawn = view.GetComponent<RandomSpawn>();
                            pkg.Write(spawn != null);
                            if (spawn != null)
                            {
                                spawn.Prepare();



                                // chance
                                pkg.Write(spawn.m_chanceToSpawn);

                                // theme
                                pkg.Write((int)(spawn.m_dungeonRequireTheme));

                                // biome
                                pkg.Write((int)(spawn.m_requireBiome));

                                // lava?
                                pkg.Write(spawn.m_notInLava);

                                // elevation min
                                pkg.Write(spawn.m_minElevation);

                                // elevation max
                                pkg.Write(spawn.m_maxElevation);



                                //pkg.Write(spawn.m_chanceToSpawn);

                                pkg.Write(spawn.m_childNetViews.Count);
                                foreach (var cview in spawn.m_childNetViews)
                                {
                                    pkg.Write(cview.GetPrefabName().GetStableHashCode());

                                    pkg.Write(cview.transform.position);
                                    pkg.Write(cview.transform.rotation);
                                }
                                //pkg.Write((int)spawn.m_dungeonRequireTheme);
                                //pkg.Write((int)spawn.m_requireBiome);
                            }
                        }



                        // dump randomSpawns

                        //  All RandomSpawns have an attached NetView
                        //  Only (*SOME) NetViews have an attached RandomSpawn

                        //  If object *STILL* enabled after attached randomspawn actions,
                        //  then instantiate object (as prefab copy)
                        //  (only applies during gen)

                        // we still have to dump all relevant info

                        //var randomSpawns = roomData.m_prefab.Asset.GetComponentsInChildren<RandomSpawn>();

                        // try something to avoid many rspawns
                        // just gather all to check for nested ranSpawns

                        // CHECK that all randomSpawns have no nested ...
                        //var randomSpawns = views.GetComponentsInChildren<RandomSpawn>();

                        // Check all random spawns beforehand, for any sub-tied objects
                        //  because we expect only ZNetViews to be the only object
                        /*
                        {
                            roomData.m_prefab.Asset.GetComponentsInChildren<RandomSpawn>();

                            var childCount = roomData.m_prefab.Asset.transform.childCount;
                        }

                        foreach (var view in views)
                        {
                            var randomSpawn = view.GetComponentsInChildren<RandomSpawn>();
                            if (randomSpawn != null)
                            {
                                // check that no sub objects exist
                            }
                        }


                        pkg.Write(randomSpawns.Length);
                        foreach (var rand in randomSpawns)
                        {
                            // write tied <view Object> index (or -1 if none...?)
                            //  Get attached NetView

                            pkg.Write(rand.gameObject)

                            // write offObject index / is this a view...?

                            // chance
                            pkg.Write(rand.m_chanceToSpawn);

                            // theme
                            pkg.Write(nameof(rand.m_dungeonRequireTheme));

                            // biome
                            pkg.Write(nameof(rand.m_requireBiome));

                            // lava?
                            pkg.Write(rand.m_notInLava);

                            // elevation min
                            pkg.Write(rand.m_minElevation);

                            // elevation max
                            pkg.Write(rand.m_maxElevation);
                        }*/


                        pkg.Write(room.m_size);
                        pkg.Write((int)room.m_theme);
                        pkg.Write(room.m_weight);
                        pkg.Write(room.transform.position);
                        pkg.Write(room.transform.rotation);


                    }

                    // everything is disgustingly manual in this game... ugh...
                    foreach (var c in enabledComponentsInChildren2)
                    {
                        c.Reset();
                    }
                }

                File.WriteAllBytes(ValhallaDumper.PKG_PATH + "dungeons.pkg", pkg.GetArray());

                LogInfo("Dumped " + dungeons.Count + " dungeons");
            }



            {
                LogInfo("Dumping Vegetation");



                ZPackage pkg = new ZPackage();

                List<ZoneSystem.ZoneVegetation> vegetation = new List<ZoneSystem.ZoneVegetation>();
                foreach (var veg in ZoneSystem.instance.m_vegetation)
                {
                    if (veg.m_enable)
                    {
                        if (veg.m_prefab && veg.m_prefab.GetComponent<ZNetView>())
                        {
                            vegetation.Add(veg);
                            LogInfo("Querying " + veg.m_prefab.name);
                        }
                        else {
                            LogError("Failed to query ZoneVegetation: " + veg.m_prefab.name);
                        }
                    }
                    else
                    {
                        LogWarning("Skipping query of " + veg.m_prefab.name);

                        // Most conflict anyways because lazy
                        //if (veg.m_name != veg.m_prefab.name)
                        //LogWarning("ZoneVegetation unequal names: " + veg.m_name + ", " + veg.m_prefab.name);
                    }
                }

                var layers = Enumerable.Range(0, 31).Select(index => index + ": " + LayerMask.LayerToName(index)).Where(l => !string.IsNullOrEmpty(l)).ToList();

                int BLOCK_MASK = ZoneSystem.m_instance.m_blockRayMask;
                int SOLID_MASK = ZoneSystem.m_instance.m_solidRayMask;
                int STATIC_MASK = ZoneSystem.m_instance.m_staticSolidRayMask;
                int TERRAIN_MASK = ZoneSystem.m_instance.m_terrainRayMask;

                LogWarning("Layers: \n - " + string.Join("\n - ", layers));

                LogWarning("blockRayMask: " + Convert.ToString(BLOCK_MASK, 2));
                LogWarning("solidRayMask: " + Convert.ToString(SOLID_MASK, 2));
                LogWarning("staticSolidRayMask: " + Convert.ToString(STATIC_MASK, 2));
                LogWarning("terrainRayMask: " + Convert.ToString(TERRAIN_MASK, 2));

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
                    //LogWarning("Vegetation prefab localScale is not (1, 1, 1): " + veg.m_name + " " + veg.m_prefab.transform.localScale);

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

                        LogWarning("Dumping vegetation block layer " + veg.m_prefab.name + ", radius: " + radius);
                        pkg.Write(radius);

                        /*
                        if ((collider.gameObject.layer & BLOCK_MASK) > 0)
                        {

                            //radiusMap["mudpile_beacon"] = 0;
                            //radiusMap["MineRock_Tin"]

                            // "ice1" is part of deep north (NYI)

                            // .2 or less


                            pkg.Write((float)extents2.magnitude); // hopefully most vegetation is circular
                            LogWarning("Dumping vegetation block layer " + veg.m_prefab.name + ", radius: " + extents2.magnitude);
                        }
                        else
                        {
                            LogWarning("Vegetation has no block mask " + veg.m_prefab.name + "(" + Convert.ToString(collider.gameObject.layer, 2) + "), radius: " + extents2.magnitude);
                            //var extents3 = collider.bounds.extents;
                            //var extents2 = new Vector2(extents3.x, extents3.z);
                            //pkg.Write((float)extents2.magnitude); // hopefully most vegetation is circular
                            pkg.Write(0.0f);
                        }*/
                    }
                    else
                    {
                        LogWarning("Vegetation has no collider " + veg.m_prefab.name);
                        pkg.Write(0.0f);
                    }

                    UnityEngine.GameObject.Destroy(obj);

                    /*else
                    {
                        LogWarning("Vegetation missing collider: " + veg.m_prefab.name);

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
                            LogWarning("Vegetation missing meshfilter");
                            var renderer = veg.m_prefab.GetComponent<Renderer>();
                            if (!renderer) renderer = veg.m_prefab.GetComponentInChildren<Renderer>();
                            if (renderer)
                            {
                                var extents3 = renderer.bounds.extents;
                                var extents2 = new Vector2(extents3.x, extents3.z);
                                pkg.Write((float)extents2.magnitude); // hopefully most vegetation is circular
                            } else
                            {
                                LogError("Vegetation missing renderer: " + veg.m_prefab.name);
                                pkg.Write(0.15f);

                                //LogWarning("Vegetation missing renderer: " + veg.m_prefab.name);

                                //var lod = veg.m_prefab.GetComponent<LODGroup>();
                                //if (lod)
                                //{
                                //    
                                //}
                                //else
                                //{
                                //    LogError("Vegetation complete fail: ")
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

                File.WriteAllBytes(ValhallaDumper.PKG_PATH + "vegetation.pkg", pkg.GetArray());

                LogWarning("Dumped " + vegetation.Count + "/" + ZoneSystem.instance.m_vegetation.Count + " ZoneVegetation");
            }

            // now dump starting keys?




        }
       


        //public static bool loaded = false;

        public static void RecurseObjectPrint(GameObject gameObject, uint depth)
        {
            String currentDepth = new String(' ', (int)depth * 2) + " - ";
            String nestedDepth = new String(' ', (int)(depth + 1) * 2) + " - ";
            
            LogWarning(currentDepth + "name: " + gameObject.name);
            var monos = gameObject.GetComponents(typeof(MonoBehaviour));
            if (monos.Length > 0)
            {
                LogWarning(currentDepth + "Monos: ");
                foreach (var mono in monos)
                {
                    LogWarning(nestedDepth + mono.GetType().Name);
                }
            }

            int childCount = gameObject.transform.childCount;
            if (childCount > 0)
            {
                LogWarning(currentDepth + "children: ");
                for (int i = 0; i < childCount; i++)
                {
                    var child = gameObject.transform.GetChild(i).gameObject;
                    RecurseObjectPrint(child, depth + 1);
                }
            }
            
        }

        /*
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

                DumpDocs(ref __instance);

                // we all love a lack of error messages dont we?
                //try
                //{
                DumpPackages(ref __instance);
                //} catch (Exception e)
                //{
                //    LogError("Error while dumping: ");
                //    LogError(e.StackTrace.ToString());
                //    LogWarning("L: " + new StackTrace(e, true).GetFrame(0).GetFileLineNumber());
                //}
            }
        }
        */

    }
}

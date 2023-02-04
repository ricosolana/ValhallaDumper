// JotunnModExample
// A Valheim mod using Jötunn
// Used to demonstrate the libraries capabilities
// 
// File:    JotunnModExample.cs
// Project: JotunnModExample

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JotunnModExample
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    internal class JotunnModExample : BaseUnityPlugin
    {
        // BepInEx' plugin metadata
        public const string PluginGUID = "com.jotunn.JotunnModExample";
        public const string PluginName = "JotunnModExample";
        public const string PluginVersion = "2.7.7";

        Harmony _harmony;

        private void Awake()
        {
            Game.isModded = true;
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);

            ZLog.Log("Loading ValhallaDumper");

            //Directory.CreateDirectory("./locationTraces");


        }

        [HarmonyPatch(typeof(ZoneSystem))]
        class ZoneSystemPatch
        {
            //[HarmonyPrefix]
            //[HarmonyPatch(nameof(ZoneSystem.GenerateLocations), new[] {typeof(ZoneSystem.ZoneLocation)} )]
            static bool ScrewDnspyPrefix(ref ZoneSystem __instance, ref ZoneSystem.ZoneLocation location)
            {
                List<String> trace = new List<String>();

                UnityEngine.Random.InitState(WorldGenerator.instance.GetSeed() + location.m_prefabName.GetStableHashCode());
                int num = 0;
                int num2 = 0;
                int num3 = 0;
                int num4 = 0;
                int num5 = 0;
                int num6 = 0;
                int num7 = 0;
                int num8 = 0;
                float locationRadius = Mathf.Max(location.m_exteriorRadius, location.m_interiorRadius);
                int num9 = location.m_prioritized ? 200000 : 100000;
                int num10 = 0;
                int num11 = __instance.CountNrOfLocation(location);
                float num12 = 10000f;
                if (location.m_centerFirst)
                {
                    num12 = location.m_minDistance;
                }
                if (location.m_unique && num11 > 0)
                {
                    return false;
                }

                int num13 = 0;
                while (num13 < num9 && num11 < location.m_quantity)
                {
                    trace.Add("a:" + num13);

                    Vector2i randomZone = __instance.GetRandomZone(num12);
                    if (location.m_centerFirst)
                    {
                        num12 += 1f;
                    }

                    if (__instance.m_locationInstances.ContainsKey(randomZone))
                    {
                        trace.Add("ContainsKey");
                        num++;
                    }
                    else if (!__instance.IsZoneGenerated(randomZone))
                    {
                        trace.Add("IsZoneGenerated");
                        Vector3 zonePos = __instance.GetZonePos(randomZone);
                        Heightmap.BiomeArea biomeArea = WorldGenerator.instance.GetBiomeArea(zonePos);
                        if ((location.m_biomeArea & biomeArea) == (Heightmap.BiomeArea)0)
                        {
                            num4++;
                            trace.Add("biomeArea");
                        }
                        else
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                trace.Add("i:" + i);
                                num10++;
                                Vector3 randomPointInZone = __instance.GetRandomPointInZone(randomZone, locationRadius);
                                float magnitude = randomPointInZone.magnitude;
                                if (location.m_minDistance != 0f && magnitude < location.m_minDistance)
                                {
                                    num2++;
                                    trace.Add("m_minDistance");
                                }
                                else if (location.m_maxDistance != 0f && magnitude > location.m_maxDistance)
                                {
                                    num2++;
                                    trace.Add("m_maxDistance");
                                }
                                else
                                {
                                    Heightmap.Biome biome = WorldGenerator.instance.GetBiome(randomPointInZone);
                                    if ((location.m_biome & biome) == Heightmap.Biome.None)
                                    {
                                        num3++;
                                        trace.Add("m_biome");
                                    }
                                    else
                                    {
                                        randomPointInZone.y = WorldGenerator.instance.GetHeight(randomPointInZone.x, randomPointInZone.z);
                                        float num14 = randomPointInZone.y - __instance.m_waterLevel;
                                        if (num14 < location.m_minAltitude || num14 > location.m_maxAltitude)
                                        {
                                            num5++;
                                            trace.Add("altitude");
                                        }
                                        else
                                        {
                                            if (location.m_inForest)
                                            {
                                                float forestFactor = WorldGenerator.GetForestFactor(randomPointInZone);
                                                if (forestFactor < location.m_forestTresholdMin || forestFactor > location.m_forestTresholdMax)
                                                {
                                                    num6++;
                                                    trace.Add("forestFactor");
                                                    goto IL_27C;
                                                }
                                                trace.Add("noForestFactor");
                                            }
                                            float num15;
                                            Vector3 vector;
                                            WorldGenerator.instance.GetTerrainDelta(randomPointInZone, location.m_exteriorRadius, out num15, out vector);
                                            if (num15 > location.m_maxTerrainDelta || num15 < location.m_minTerrainDelta)
                                            {
                                                num8++;
                                                trace.Add("terrainDelta");
                                            }
                                            else
                                            {
                                                if (location.m_minDistanceFromSimilar <= 0f || !__instance.HaveLocationInRange(location.m_prefabName, location.m_group, randomPointInZone, location.m_minDistanceFromSimilar))
                                                {
                                                    __instance.RegisterLocation(location, randomPointInZone, false);
                                                    num11++;
                                                    trace.Add("register");
                                                    break;
                                                }
                                                num7++;
                                                trace.Add("noRegister");
                                            }
                                        }
                                    }
                                }
                                IL_27C:;
                            }
                        }
                    }
                    num13++;
                }

                // CRLF...
                //File.WriteAllLines("./locationTraces/" + location.m_prefabName + ".trace", trace);

                File.WriteAllText("./locationTraces/" + location.m_prefabName + ".trace", 
                    string.Join("\n", trace));

                if (num11 < location.m_quantity)
                {
                    ZLog.LogWarning(string.Concat(new string[]
                    {
                        "Failed to place all ",
                        location.m_prefabName,
                        ", placed ",
                        num11.ToString(),
                        " out of ",
                        location.m_quantity.ToString()
                    }));
                    //ZLog.DevLog("errorLocationInZone " + num.ToString());
                    //ZLog.DevLog("errorCenterDistance " + num2.ToString());
                    //ZLog.DevLog("errorBiome " + num3.ToString());
                    //ZLog.DevLog("errorBiomeArea " + num4.ToString());
                    //ZLog.DevLog("errorAlt " + num5.ToString());
                    //ZLog.DevLog("errorForest " + num6.ToString());
                    //ZLog.DevLog("errorSimilar " + num7.ToString());
                    //ZLog.DevLog("errorTerrainDelta " + num8.ToString());
                }

                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(ZoneSystem.Start))]
            static void StartPostfix(ref ZoneSystem __instance)
            {

                String date = String.Format("{0:MM/dd/yyyy hh:mm.ss}", DateTime.Now);

                Directory.CreateDirectory("./dumped");

                {
                    ZLog.Log("Dumping Prefabs");

                    // Prepare prefabs
                    List<GameObject> prefabs = new List<GameObject>();
                    foreach (var prefab in ZNetScene.instance.m_prefabs)
                    {
                        if (prefab.GetComponent<ZNetView>())
                            prefabs.Add(prefab);
                        else
                            ZLog.LogError("Prefab missing ZNetView: " + prefab.name);
                    }

                    var staticSolidRayMask = LayerMask.GetMask(new string[]
                    {
                                "static_solid",
                                "terrain"
                    });

                    // Dump prefabs
                    ZPackage pkg = new ZPackage();

                    pkg.Write(date);
                    pkg.Write(Version.GetVersionString()); // write version for reference purposes
                    pkg.Write(prefabs.Count);
                    foreach (var prefab in prefabs)
                    {
                        var view = prefab.GetComponent<ZNetView>();

                        if ((prefab.layer & staticSolidRayMask) > 0)
                            ZLog.Log("staticSolid: " + prefab.name);

                        pkg.Write(view.GetPrefabName());
                        pkg.Write(view.m_distant);
                        pkg.Write(view.m_persistent);
                        pkg.Write((int)view.m_type);
                        pkg.Write(view.m_syncInitialScale);
                        if (view.m_syncInitialScale)
                            pkg.Write(view.gameObject.transform.localScale);
                    }

                    File.WriteAllBytes("./dumped/prefabs.pkg", pkg.GetArray());

                    ZLog.Log("Dumped " + prefabs.Count + "/" + ZNetScene.instance.m_prefabs.Count + " prefabs");
                }



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

                    pkg.Write(date);
                    pkg.Write(Version.GetVersionString()); // write version for reference purposes
                    pkg.Write(locations.Count);
                    foreach (var loc in locations)
                    {
                        loc.m_prefab.transform.position = Vector3.zero;
                        loc.m_prefab.transform.rotation = Quaternion.identity;
                        //if (loc.m_prefab.transform.localScale != ZNetScene.instance.)

                        pkg.Write(loc.m_prefabName);
                        ///pkg.Write(loc.m_prefab.name);       // m_prefab appears to be null
                        pkg.Write((int)loc.m_biome);
                        pkg.Write((int)loc.m_biomeArea);
                        pkg.Write(loc.m_location.m_applyRandomDamage);
                        pkg.Write(loc.m_centerFirst);
                        pkg.Write(loc.m_location.m_clearArea);
                        pkg.Write(loc.m_location.m_useCustomInteriorTransform);
                        pkg.Write(loc.m_exteriorRadius);
                        pkg.Write(loc.m_interiorRadius);
                        pkg.Write(loc.m_forestTresholdMin);
                        pkg.Write(loc.m_forestTresholdMax);
                        pkg.Write(loc.m_interiorPosition);
                        pkg.Write(loc.m_generatorPosition);
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
                        //pkg.Write(loc.m_prioritized);
                        pkg.Write(loc.m_prioritized ? 200000 : 100000); // spawnAttempts
                        pkg.Write(loc.m_quantity);
                        pkg.Write(loc.m_randomRotation);
                        //pkg.Write(loc.m_randomSpawns); // implement later
                        pkg.Write(loc.m_slopeRotation);
                        pkg.Write(loc.m_snapToWater);
                        pkg.Write(loc.m_unique);

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

                                views.Add(obj.GetComponent<ZNetView>());
                            }
                        }

                        pkg.Write(views.Count);
                        foreach (var view in views)
                        {
                            pkg.Write(view.GetPrefabName().GetStableHashCode());
                            //pkg.Write(view.gameObject.transform.position);
                            //pkg.Write(view.gameObject.transform.rotation);
                            pkg.Write(view.m_zdo.m_position);
                            pkg.Write(view.m_zdo.m_rotation);
                        }

                        // free (not really needed)
                        foreach (var view in views)
                            UnityEngine.GameObject.Destroy(view.gameObject);

                        ZNetView.FinishGhostInit();
                    }

                    File.WriteAllBytes("./dumped/zoneLocations.pkg", pkg.GetArray());

                    ZLog.Log("Dumped " + locations.Count + "/" + ZoneSystem.instance.m_locations.Count + " ZoneLocations");
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



                    pkg.Write(date);
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

                        GameObject obj = Instantiate<GameObject>(veg.m_prefab);

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
                        } else
                        {
                            ZLog.LogWarning("Vegetation has no collider " + veg.m_prefab.name);
                            pkg.Write(0.0f);
                        }

                        Destroy(obj);
                        
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
            }
        }

        private void Destroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

}
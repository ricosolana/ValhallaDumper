using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValhallaDumper
{
    internal class ZoneDumper
    {

        [HarmonyPatch(typeof(ZoneSystem))]
        class ZoneSystemPatch
        {
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


    }
}

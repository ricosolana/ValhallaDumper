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
        }

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



                    // Dump prefabs
                    ZPackage pkg = new ZPackage();

                    pkg.Write(date);
                    pkg.Write(Version.GetVersionString()); // write version for reference purposes
                    pkg.Write(prefabs.Count);
                    foreach (var prefab in prefabs)
                    {
                        var view = prefab.GetComponent<ZNetView>();

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

                        List<ZNetView> views = new List<ZNetView>();
                        foreach (var view in loc.m_netViews)
                        {
                            if (view.gameObject.activeSelf)
                                views.Add(view);
                        }

                        pkg.Write(views.Count);
                        foreach (var view in views)
                        {
                            pkg.Write(view.GetPrefabName().GetStableHashCode());
                            pkg.Write(view.gameObject.transform.position);
                            pkg.Write(view.gameObject.transform.rotation);
                        }
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
                                vegetation.Add(veg);
                            else
                                ZLog.LogError("Failed to serialize ZoneVegetation: " + veg.m_name);
                        }
                        else
                        {
                            ZLog.Log("Skipping dump of " + veg.m_name);

                            if (veg.m_name != veg.m_prefab.name)
                                ZLog.LogWarning("ZoneVegetation unequal names: " + veg.m_name + ", " + veg.m_prefab.name);
                        }
                    }

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
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
    internal class DungeonDumper
    {
        [HarmonyPatch(typeof(DungeonGenerator))]
        class DungeonGeneratorPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(DungeonGenerator.Generate), new Type[] { typeof(int), typeof(ZoneSystem.SpawnMode) })]
            static void GeneratePostfix(ref DungeonGenerator __instance)
            {
                ZLog.LogWarning("Dungeon '" + __instance.name 
                    + "', seed: " + __instance.m_generatedSeed 
                    + ", pos: " + __instance.transform.position 
                    + ", rot: " + __instance.transform.rotation
                );
            }
        }
    }
}

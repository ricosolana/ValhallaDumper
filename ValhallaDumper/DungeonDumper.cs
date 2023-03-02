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
        /*
        [HarmonyPatch(typeof(DungeonDB))]
        class DungeonDBPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(DungeonDB.Start))]
            static void StartPostfix(ref DungeonDB __instance)
            {
                //__instance.m_rooms.

                String date = String.Format("{0:MM/dd/yyyy hh:mm.ss}", DateTime.Now);

                {
                    ZLog.Log("Dumping Dungeon Rooms");

                    List<Room> prefabs = new List<Room>();
                    foreach (var room in __instance.r)

                }

                {
                    ZLog.Log("Dumping Dungeons");



                }

            }
        }*/
    }
}

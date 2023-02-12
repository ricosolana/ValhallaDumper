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

namespace ValhallaDumper
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    internal class ValhallaDumper : BaseUnityPlugin
    {
        // BepInEx' plugin metadata
        public const string PluginGUID = "com.crzi.ValhallaDumper";
        public const string PluginName = "ValhallaDumper";
        public const string PluginVersion = "1.0.0";

        Harmony _harmony;

        private void Awake()
        {
            Game.isModded = true;
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);

            ZLog.Log("Loading ValhallaDumper");
        }

        private void Destroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

}
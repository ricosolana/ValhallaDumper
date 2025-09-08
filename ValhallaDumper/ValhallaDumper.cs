using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
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
//using Jotunn.Utils;

namespace ValhallaDumper
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    //[NetworkCompatibility(CompatibilityLevel.ServerMustHaveMod, VersionStrictness.Minor)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class ValhallaDumper : BaseUnityPlugin
    {
        // BepInEx' plugin metadata
        public const string PluginGUID = "com.crzi.ValhallaDumper";
        public const string PluginName = "ValhallaDumper";
        public const string PluginVersion = "1.0.0";

        public const string DUMPER_PATH = "dumped/";
        public const string PKG_PATH = DUMPER_PATH + "pkg/";
        public const string DOC_PATH = DUMPER_PATH + "doc/";

        Harmony _harmony;

        private void Awake()
        {
            Game.isModded = true;

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);

            CommandManager.Instance.AddConsoleCommand(new AvlDumpCommand());

            ZLog.Log("Loading ValhallaDumper");
        }

        private void Destroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

}
using BepInEx;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLevelLoader.Extras;
using LethalLevelLoader.Modules;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using Unity.Netcode;
using UnityEngine;
using static LethalLevelLoader.Modules.Enemies;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace LethalLevelLoader
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin {
        public const string ModGUID = "imabatby.lethallevelloader";
        public const string ModName = "LethalLevelLoader";
        public const string ModVersion = "0.0.1";

        public static AssetBundle MainAssets;
        private static readonly Harmony Harmony = new Harmony(ModGUID);

        public static BepInEx.Logging.ManualLogSource logger;


        private void Awake() {
            logger = Logger;

            Logger.LogInfo($"LethalLevelLoader loaded!!");
            new ILHook(typeof(StackTrace).GetMethod("AddFrames", BindingFlags.Instance | BindingFlags.NonPublic), IlHook);


            Harmony.PatchAll(typeof(DebugHelper));
            Harmony.PatchAll(typeof(DebugOrderOfExecution));

            Harmony.PatchAll(typeof(ContentExtractor));

            Enemies.Init();

            Items.Init();
            Unlockables.Init();
            MapObjects.Init();

            //Dungeon.Init();
            Weathers.Init();
            LethalLevelLoader.Modules.NetworkPrefabs.Init();

            Harmony.PatchAll(typeof(Dungeon));
            Harmony.PatchAll(typeof(Levels));
            Harmony.PatchAll(typeof(TerminalUtils));
            Harmony.PatchAll(typeof(LevelLoader));

            Harmony.PatchAll(typeof(AssetBundleLoader));
            AssetBundleLoader.FindBundles();
        }


        private void IlHook(ILContext il) {
            var cursor = new ILCursor(il);
            cursor.GotoNext(
                x => x.MatchCallvirt(typeof(StackFrame).GetMethod("GetFileLineNumber", BindingFlags.Instance | BindingFlags.Public))
            );
            cursor.RemoveRange(2);
            cursor.EmitDelegate<Func<StackFrame, string>>(GetLineOrIL);
        }

        private static string GetLineOrIL(StackFrame instance) {
            var line = instance.GetFileLineNumber();
            if (line == StackFrame.OFFSET_UNKNOWN || line == 0) {
                return "IL_" + instance.GetILOffset().ToString("X4");
            }

            return line.ToString();
        }

    }
}
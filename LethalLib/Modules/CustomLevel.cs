using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalLib.Modules
{
    public class CustomLevel
    {
        public TerminalKeyword LevelKeyword;
        public TerminalNode TerminalRoute;
        public SelectableLevel SelectableLevel;
        public TerminalNode LevelTerminalInfo;
        public enum LevelType { Vanilla, Modded }
        public LevelType levelType;
        public GameObject LevelPrefab;
        public static int MoonID = 9;
        public static bool isCustomLevel;
        public string MoonFriendlyName;
        private static List<string> ObjectsToDestroy = new List<string> {
                "CompletedVowTerrain",
                "tree",
                "Tree",
                "Rock",
                "StaticLightingSky",
                "ForestAmbience",
                "Local Volumetric Fog",
                "GroundFog",
                "Sky and Fog Global Volume",
                "SunTexture"
            };

        public CustomLevel(SelectableLevel newSelectableLevel, TerminalKeyword newTerminalAsset,
        TerminalNode NewRoute, TerminalNode newTerminalInfo, GameObject newLevelPrefab)
        {
            MoonID = MoonID++;
            SelectableLevel = newSelectableLevel;
            SelectableLevel.levelID = MoonID;
            LevelKeyword = newTerminalAsset;
            TerminalRoute = NewRoute;
            NewRoute.buyRerouteToMoon = MoonID;
            NewRoute.terminalOptions[1].result.buyRerouteToMoon = MoonID;
            LevelTerminalInfo = newTerminalInfo;
            LevelPrefab = newLevelPrefab;
            MoonFriendlyName = SelectableLevel.PlanetName;

            Levels.AddCustomLevel(this);
        }

        public static void AddObjectToDestroyList(string NewObjectName)
        {
            ObjectsToDestroy.Add(NewObjectName);
        }
        public static void ClearObjectToDestroyList()
        {
            ObjectsToDestroy.Clear();
        }

        public List<string> GetDestroyList() { return ObjectsToDestroy; }
    }
}

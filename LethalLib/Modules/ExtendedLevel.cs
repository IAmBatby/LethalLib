using HarmonyLib;
using LethalLib.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LethalLib.Modules
{
    public enum LevelType { Vanilla, Custom, Any } //Any included for built in checks.

    public class ExtendedLevel
    {
        public SelectableLevel selectableLevel;

        public string sourceName = "Lethal Company"; //Levels from AssetBundles will have this as their Assembly Name.
        public string NumberlessPlanetName => GetNumberlessPlanetName(selectableLevel);
        public int fireExitsAmount = 0;

        //ChatGPT did this one i'll be honest.
        public GameObject levelPrefab;

        public LevelType levelType;

        public ExtendedLevel(SelectableLevel newSelectableLevel, LevelType newLevelType, bool generateTerminalAssets, GameObject newLevelPrefab = null, string newSourceName = "Lethal Company")
        {
            DebugHelper.Log("Creating New Extended Level For Moon: " + ExtendedLevel.GetNumberlessPlanetName(newSelectableLevel));
            levelType = newLevelType;
            selectableLevel = newSelectableLevel;
            sourceName = newSourceName;

            if (newLevelType == LevelType.Custom)
            {
                selectableLevel.levelID = 9 + Levels.customLevelsList.Count;
                selectableLevel.sceneName = "InitSceneLaunchOptions"; //BAD FIX THIS LATER.
                levelPrefab = newLevelPrefab;
                fireExitsAmount = levelPrefab.GetComponentsInChildren<EntranceTeleport>().Length - 1; //-1 Becuase this includes Main Entrance.
            }

            if (generateTerminalAssets == true)
                TerminalUtils.CreateLevelTerminalData(this);

            Levels.AddSelectableLevel(this);
        }

        public static string GetNumberlessPlanetName(SelectableLevel selectableLevel)
        {
            if (selectableLevel != null)
                return new string(selectableLevel.PlanetName.SkipWhile(c => !char.IsLetter(c)).ToArray());
            else
                return string.Empty;
        }
    }
}
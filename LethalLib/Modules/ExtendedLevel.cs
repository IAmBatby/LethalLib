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

    [CreateAssetMenu(menuName = "LethalLib/ExtendedLevel")]
    public class ExtendedLevel : ScriptableObject
    {
        public SelectableLevel selectableLevel;
        public GameObject levelPrefab;

        public LevelType levelType;
        public string sourceName = "Lethal Company"; //Levels from AssetBundles will have this as their Assembly Name.
        public string NumberlessPlanetName => GetNumberlessPlanetName(selectableLevel);
        public int fireExitsAmount = 0;
        public bool enableCustomDungeonInjection = true;

        public ExtendedLevel(SelectableLevel newSelectableLevel, LevelType newLevelType, bool generateTerminalAssets, GameObject newLevelPrefab = null, string newSourceName = "Lethal Company")
        {
            DebugHelper.Log("Creating New Extended Level For Moon: " + ExtendedLevel.GetNumberlessPlanetName(newSelectableLevel));
            levelType = newLevelType;
            selectableLevel = newSelectableLevel;
            sourceName = newSourceName;

            if (newLevelType == LevelType.Custom)
            {
                levelPrefab = newLevelPrefab;
                ProcessCustomLevel(this);
            }


            Levels.AddSelectableLevel(this);
        }

        public void Initialize(SelectableLevel newSelectableLevel, LevelType newLevelType, bool generateTerminalAssets, GameObject newLevelPrefab = null, string newSourceName = "Lethal Company")
        {
            DebugHelper.Log("Creating New Extended Level For Moon: " + ExtendedLevel.GetNumberlessPlanetName(newSelectableLevel));
            levelType = newLevelType;
            selectableLevel = newSelectableLevel;
            sourceName = newSourceName;

            if (newLevelType == LevelType.Custom)
                levelPrefab = newLevelPrefab;


        }

        public static void ProcessCustomLevel(ExtendedLevel extendedLevel)
        {
            extendedLevel.selectableLevel.levelID = 9 + Levels.customLevelsList.Count;
            extendedLevel.selectableLevel.sceneName = "InitSceneLaunchOptions"; //BAD FIX THIS LATER.
            extendedLevel.fireExitsAmount = extendedLevel.levelPrefab.GetComponentsInChildren<EntranceTeleport>().Length - 1; //-1 Becuase this includes Main Entrance.
            TerminalUtils.CreateLevelTerminalData(extendedLevel);
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
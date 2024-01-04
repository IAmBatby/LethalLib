using DunGen.Graph;
using HarmonyLib;
using LethalLevelLoader;
using LethalLevelLoader.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public enum LevelType { Vanilla, Custom, Any } //Any included for built in checks.

namespace LethalLevelLoader.Modules
{

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
        //public List<(DungeonFlow, IntWithRarity)> dungeonFlowsList = new List<(DungeonFlow, IntWithRarity)>();
        public List<ExtendedDungeonFlowWithRarity> extendedDungeonFlowsList = new List<ExtendedDungeonFlowWithRarity>();

        public List<string> levelTags = new List<string>();

        public void Initialize(SelectableLevel newSelectableLevel, LevelType newLevelType, bool generateTerminalAssets, GameObject newLevelPrefab = null, string newSourceName = "Lethal Company")
        {
            DebugHelper.Log("Creating New Extended Level For Moon: " + ExtendedLevel.GetNumberlessPlanetName(newSelectableLevel));
            levelType = newLevelType;
            selectableLevel = newSelectableLevel;
            sourceName = newSourceName;


            if (newLevelType == LevelType.Custom)
            {
                levelPrefab = newLevelPrefab;
                levelTags.Add("Custom");
            }

            List<IntWithRarity> tempFlowTypes = new List<IntWithRarity>(selectableLevel.dungeonFlowTypes);

            foreach (IntWithRarity dungeonFlow in selectableLevel.dungeonFlowTypes)
                if (dungeonFlow == null)
                    tempFlowTypes.Remove(dungeonFlow);

            selectableLevel.levelID = Levels.allLevelsList.Count;

            selectableLevel.dungeonFlowTypes = tempFlowTypes.ToArray();
        }

        public static void ProcessCustomLevel(ExtendedLevel extendedLevel)
        {
            extendedLevel.selectableLevel.sceneName = Levels.injectionSceneName; //BAD FIX THIS LATER.
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
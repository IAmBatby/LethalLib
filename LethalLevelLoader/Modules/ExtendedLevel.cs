using DunGen.Graph;
using HarmonyLib;
using LethalLevelLoader;
using LethalLevelLoader.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public enum ContentType { Vanilla, Custom, Any } //Any & All included for built in checks.

namespace LethalLevelLoader.Modules
{
    [CreateAssetMenu(menuName = "LethalLib/ExtendedLevel")]
    public class ExtendedLevel : ScriptableObject
    {
        public SelectableLevel selectableLevel;
        public GameObject levelPrefab;

        public ContentType levelType;
        public string sourceName = "Lethal Company"; //Levels from AssetBundles will have this as their Assembly Name.
        public string NumberlessPlanetName => GetNumberlessPlanetName(selectableLevel);
        public int routePrice;
        public ContentType allowedDungeonTypes = ContentType.Any;

        public List<string> levelTags = new List<string>();

        public void Initialize(SelectableLevel newSelectableLevel, ContentType newLevelType, int newRoutePrice, bool generateTerminalAssets, GameObject newLevelPrefab = null, string newSourceName = "Lethal Company")
        {
            DebugHelper.Log("Creating New Extended Level For Moon: " + ExtendedLevel.GetNumberlessPlanetName(newSelectableLevel));

            if (selectableLevel == null)
                selectableLevel = newSelectableLevel;

            if (sourceName != newSourceName)
                sourceName = newSourceName;

            levelType = newLevelType;
            routePrice = newRoutePrice;

        }

        public static void ProcessCustomLevel(ExtendedLevel extendedLevel)
        {
            extendedLevel.levelTags.Add("Custom");
            extendedLevel.selectableLevel.levelID = Levels.allLevelsList.Count;
            extendedLevel.selectableLevel.sceneName = Levels.injectionSceneName;
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
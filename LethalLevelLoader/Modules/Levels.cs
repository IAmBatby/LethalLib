using DunGen.Graph;
using HarmonyLib;
using LethalLevelLoader.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalLevelLoader.Modules.Dungeon;
using Object = UnityEngine.Object;

namespace LethalLevelLoader.Modules
{
    public class Levels
    {
        [Flags]
        public enum LevelTypes
        {
            None = 1 << 0,
            ExperimentationLevel = 1 << 1,
            AssuranceLevel = 1 << 2,
            VowLevel = 1 << 3,
            OffenseLevel = 1 << 4,
            MarchLevel = 1 << 5,
            RendLevel = 1 << 6,
            DineLevel = 1 << 7,
            TitanLevel = 1 << 8,
            All = ExperimentationLevel | AssuranceLevel | VowLevel | OffenseLevel | MarchLevel | RendLevel | DineLevel | TitanLevel
        }

        public static string injectionSceneName = "InitSceneLaunchOptions";


        public static List<ExtendedLevel> allLevelsList = new List<ExtendedLevel>();
        public static List<ExtendedLevel> vanillaLevelsList = new List<ExtendedLevel>();
        public static List<ExtendedLevel> customLevelsList = new List<ExtendedLevel>();

        public static List<SelectableLevel> AllSelectableLevelsList
        {
            get
            {
                List<SelectableLevel> returnList = new List<SelectableLevel>();
                foreach (ExtendedLevel extendedLevel in allLevelsList)
                    returnList.Add(extendedLevel.selectableLevel);
                return (returnList);
            }
        }


        [HarmonyPatch(typeof(RoundManager), "SpawnMapObjects")]
        [HarmonyPrefix]
        public static void SpawnMapObjects_Prefix(RoundManager __instance)
        {
            List<RandomMapObject> spawnableMapObjects = Object.FindObjectsOfType<RandomMapObject>().ToList();

            string debugString = string.Empty;

            debugString += "MapPropsContainer Is: " + (GameObject.FindGameObjectWithTag("MapPropsContainer") != null) + "\n";
            debugString += "AnomalyRandom Is: " + (__instance.AnomalyRandom != null);

            debugString += "Current Level SpawnMapObject List; Count Is: " + __instance.currentLevel.spawnableMapObjects.Length + "\n";

            foreach (SpawnableMapObject spawnMapObject in __instance.currentLevel.spawnableMapObjects)
            {
                if (spawnMapObject != null)
                {
                    debugString += "SpawnMapObject Found" + "\n";
                    if (spawnMapObject.prefabToSpawn != null)
                    {
                        debugString += "PrefabToSpawn Name: " + spawnMapObject.prefabToSpawn.name + "\n";
                        debugString += "Amount" + spawnMapObject.numberToSpawn.ToString() + "\n";
                        debugString += "NetworkObject Is: " + (spawnMapObject.prefabToSpawn.GetComponent<NetworkObject>() != null);
                    }
                    else
                        debugString += "PrefabToSpawn Was Null!" + "\n";
                }
                else
                    debugString += "SpawnMapObject Was Null!" + "\n";
            }

            debugString += "\n" + "Current Dungeon RandomMapObject List; Count Is: " + spawnableMapObjects.Count + "\n";

            foreach (RandomMapObject randomMapObject in spawnableMapObjects)
            {
                if (randomMapObject != null)
                {
                    debugString += "\n" + "RandomMapObject Name: " + randomMapObject.name + "\n";
                    foreach (GameObject randomObject in randomMapObject.spawnablePrefabs)
                    {
                        if (randomObject != null)
                            debugString += "SpawnablePrefab Name: " + randomObject.name + "\n";
                        else
                            debugString += "SpawnablePrefab Was Null!" + "\n";

                    }
                }
                else
                    debugString += "RandomMapObject Was Null!" + "\n";
            }

            debugString += "End Of SpawnMapObjects Log." + "\n";

            DebugHelper.Log(debugString);
        }

        [HarmonyPatch(typeof(StartOfRound), "ChangeLevel")]
        [HarmonyPrefix]
        public static void ChangeLevel_Prefix(int levelID) //Gotta look into this properlly
        {
            if (levelID >= 9)
                levelID = 0;
        }

        [HarmonyPatch(typeof(RoundManager), "Start")]
        [HarmonyPrefix]
        public static void RoundManager_Start(RoundManager __instance)
        {
            PatchVanillaLevelLists();

            ManuallyAssignTagsToVanillaLevels();

            foreach (ExtendedLevel customLevel in Levels.customLevelsList)
                AssetBundleLoader.RestoreVanillaLevelAssetReferences(customLevel);

            foreach (ExtendedDungeonFlow customDungeonFlow in Dungeon.customDungeonFlowsList)
                AssetBundleLoader.RestoreVanillaDungeonAssetReferences(customDungeonFlow);
        }

        public static void AddSelectableLevel(ExtendedLevel extendedLevel)
        {
            DebugHelper.Log("Adding Selectable Level: " + extendedLevel.NumberlessPlanetName);
            if (extendedLevel.levelType == ContentType.Custom)
                customLevelsList.Add(extendedLevel);
            else
                vanillaLevelsList.Add(extendedLevel);

            allLevelsList.Add(extendedLevel);
        }

        public static void PatchVanillaLevelLists()
        {
            DebugHelper.Log("Patching Vanilla Level List!");
            Terminal terminal = GameObject.FindAnyObjectByType<Terminal>();
            StartOfRound startOfRound = StartOfRound.Instance;

            List<SelectableLevel> allSelectableLevels = new List<SelectableLevel>();

            foreach (ExtendedLevel extendedLevel in allLevelsList)
                allSelectableLevels.Add(extendedLevel.selectableLevel);

            startOfRound.levels = allSelectableLevels.ToArray();
            terminal.moonsCatalogueList = allSelectableLevels.ToArray();

            DebugHelper.Log("StartOfRound Levels List Length Is: " + startOfRound.levels.Length);
            DebugHelper.Log("Terminal Levels List Length Is: " + terminal.moonsCatalogueList.Length);
        }

        public static bool TryGetExtendedLevel(SelectableLevel selectableLevel, out ExtendedLevel returnExtendedLevel, ContentType levelType = ContentType.Any)
        {
            returnExtendedLevel = null;
            List<ExtendedLevel> extendedLevelsList = new List<ExtendedLevel>();

            switch (levelType)
            {
                case ContentType.Vanilla:
                    extendedLevelsList = vanillaLevelsList;
                    break;
                case ContentType.Custom:
                    extendedLevelsList = customLevelsList;
                    break;
                case ContentType.Any:
                    extendedLevelsList = allLevelsList;
                    break;
            }

            foreach (ExtendedLevel extendedLevel in extendedLevelsList)
                if (extendedLevel.selectableLevel == selectableLevel)
                    returnExtendedLevel = extendedLevel;

            return (returnExtendedLevel != null);
        }

        public static void ManuallyAssignTagsToVanillaLevels()
        {
            foreach (ExtendedLevel vanillaLevel in vanillaLevelsList)
            {
                vanillaLevel.levelTags.Add("Vanilla");

                if (vanillaLevel.NumberlessPlanetName == "Experimentation")
                    vanillaLevel.levelTags.Add("Wasteland");
                else if (vanillaLevel.NumberlessPlanetName == "Assurance")
                {
                    vanillaLevel.levelTags.Add("Desert");
                    vanillaLevel.levelTags.Add("Canyon");
                }
                else if (vanillaLevel.NumberlessPlanetName == "Vow")
                {
                    vanillaLevel.levelTags.Add("Forest");
                    vanillaLevel.levelTags.Add("Valley");
                }
                else if (vanillaLevel.NumberlessPlanetName == "Gordion")
                {
                    vanillaLevel.levelTags.Add("Company");
                    vanillaLevel.levelTags.Add("Quota");
                }
                else if (vanillaLevel.NumberlessPlanetName == "Offense")
                {
                    vanillaLevel.levelTags.Add("Desert");
                    vanillaLevel.levelTags.Add("Canyon");
                }
                else if (vanillaLevel.NumberlessPlanetName == "March")
                {
                    vanillaLevel.levelTags.Add("Forest");
                    vanillaLevel.levelTags.Add("Valley");
                }
                else if (vanillaLevel.NumberlessPlanetName == "Rend")
                {
                    vanillaLevel.levelTags.Add("Snow");
                    vanillaLevel.levelTags.Add("Ice");
                    vanillaLevel.levelTags.Add("Tundra");
                    vanillaLevel.levelCost = 600;
                }
                else if (vanillaLevel.NumberlessPlanetName == "Dine")
                {
                    vanillaLevel.levelTags.Add("Snow");
                    vanillaLevel.levelTags.Add("Ice");
                    vanillaLevel.levelTags.Add("Tundra");
                    vanillaLevel.levelCost = 650;
                }
                else if (vanillaLevel.NumberlessPlanetName == "Titan")
                {
                    vanillaLevel.levelTags.Add("Snow");
                    vanillaLevel.levelTags.Add("Ice");
                    vanillaLevel.levelTags.Add("Tundra");
                    vanillaLevel.levelCost = 700;
                }
            }
        }
    }
}
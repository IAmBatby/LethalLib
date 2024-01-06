using DunGen.Graph;
using HarmonyLib;
using LethalLevelLoader.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LethalLevelLoader.Modules.Dungeon;
using Object = UnityEngine.Object;
using Random = System.Random;

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


        [HarmonyPatch(typeof(StartOfRound), "SetPlanetsWeather")]
        [HarmonyPostfix]
        public static void SetPlanetsWeather_Postfix()
        {

            string debugString = string.Empty;

            debugString += "Start Of SetPlanetWeather() Postfix." + "\n";

            debugString += "RandomMapSeed Is: " + StartOfRound.Instance.randomMapSeed + "\n";

            debugString += "End Of SetPlanetWeather() Postfix." + "\n" + "\n";

            DebugHelper.Log(debugString);
        }

        [HarmonyPatch(typeof(StartOfRound), "SetPlanetsWeather")]
        [HarmonyPrefix]
        public static void SetPlanetsWeather_Prefix(StartOfRound __instance, int connectedPlayersOnServer)
        {
            DebugPlanetWeatherRandomisation(connectedPlayersOnServer, prePatchedLevelsList);
            DebugPlanetWeatherRandomisation(connectedPlayersOnServer, __instance.levels.ToList<SelectableLevel>());
        }

        public static void DebugPlanetWeatherRandomisation(int players, List<SelectableLevel> selectableLevelsList)
        {
            StartOfRound startOfRound = StartOfRound.Instance;

            List<SelectableLevel> selectableLevels = new List<SelectableLevel>(selectableLevelsList);

            //Recreate Weather Random Stuff


            foreach (SelectableLevel selectableLevel in selectableLevels)
                selectableLevel.currentWeather = LevelWeatherType.None;

            Random weatherRandom = new Random(startOfRound.randomMapSeed + 31);

            float playerRandomFloat = 1f;

            if (players + 1 > 1 && startOfRound.daysPlayersSurvivedInARow > 2 && startOfRound.daysPlayersSurvivedInARow % 3 == 0)
                playerRandomFloat = (float)weatherRandom.Next(15, 25) / 10f;

            int randomPlanetWeatherCurve = Mathf.Clamp((int)(Mathf.Clamp(startOfRound.planetsWeatherRandomCurve.Evaluate((float)weatherRandom.NextDouble()) * playerRandomFloat, 0f, 1f) * (float)selectableLevels.Count), 0, selectableLevels.Count);

            //Debug Logging

            string debugString = string.Empty;
            debugString += "Start Of SetPlanetWeather() Prefix." + "\n";
            debugString += "Planet Weather Being Set! Details Below;" + "\n" + "\n";
            debugString += "RandomMapSeed Is: " + startOfRound.randomMapSeed + "\n";
            debugString += "Planet Random Is: " + weatherRandom + "\n";
            debugString += "Player Random Is: " + playerRandomFloat + "\n";
            debugString += "Result From PlanetWeatherRandomCurve Is: " + randomPlanetWeatherCurve + "\n";
            debugString += "All SelectableLevels In StartOfRound: " + "\n" + "\n";


            foreach (SelectableLevel selectableLevel in selectableLevels)
            {
                debugString += selectableLevel.PlanetName + " | " + selectableLevel.currentWeather + " | " + selectableLevel.overrideWeather + "\n";
                foreach (RandomWeatherWithVariables randomWeather in selectableLevel.randomWeathers)
                    debugString += randomWeather.weatherType.ToString() + " | " + randomWeather.weatherVariable + " | " + randomWeather.weatherVariable2 + "\n";

                debugString += "\n";
            }

            debugString += "SelectableLevels Chosen Using Random Variables Should Be: " + "\n" + "\n";

            for (int j = 0; j < randomPlanetWeatherCurve; j++)
            {
                SelectableLevel selectableLevel = selectableLevels[weatherRandom.Next(0, selectableLevels.Count)];
                debugString += "SelectableLevel Chosen! Planet Name Is: " + selectableLevel.PlanetName;
                if (selectableLevel.randomWeathers != null && selectableLevel.randomWeathers.Length != 0)
                {
                    int randomSelection = weatherRandom.Next(0, selectableLevel.randomWeathers.Length);
                    debugString += " --- Selected For Weather Change! Setting WeatherType From: " + selectableLevel.currentWeather + " To: " + selectableLevel.randomWeathers[randomSelection].weatherType + "\n";
                    debugString += "          Random Selection Results Were: " + randomSelection + " (Range: 0 - " + selectableLevel.randomWeathers.Length + ") Level RandomWeathers Choices Were: " + "\n" + "          ";

                    int index = 0;
                    foreach (RandomWeatherWithVariables weatherType in selectableLevel.randomWeathers)
                    {
                        debugString += index + " . - " + weatherType.weatherType + ", ";
                        index++;
                    }
                    debugString += "\n" + "\n";
                }
                else
                    debugString += "\n";
                selectableLevels.Remove(selectableLevel);
            }

            debugString += "End Of SetPlanetWeather() Prefix." + "\n" + "\n";

            DebugHelper.Log(debugString);
        }

        /*[HarmonyPatch(typeof(StartMatchLever), "StartGame")]
        [HarmonyPrefix]
        public static void StartGame_Prefix()
        {
            DebugHelper.Log("StartMatchLeverStartGame_Prefix.");
            if (TryGetExtendedLevel(StartOfRound.Instance.currentLevel, out ExtendedLevel extendedLevel))
                if (extendedLevel.levelType == ContentType.Custom)
                    StartOfRound.Instance.currentLevel.sceneName = injectionSceneName;
        }

        [HarmonyPatch(typeof(StartOfRound), "PassTimeToNextDay")]
        [HarmonyPostfix]
        public static void PassTimeToNextDay_Prefix()
        {
            DebugHelper.Log("PassTimeToNextDay_Prefix.");
            if (TryGetExtendedLevel(StartOfRound.Instance.currentLevel, out ExtendedLevel extendedLevel))
                if (extendedLevel.levelType == ContentType.Custom)
                    StartOfRound.Instance.currentLevel.sceneName = extendedLevel.NumberlessPlanetName;
        }*/

        /*[HarmonyPatch(typeof(StartOfRound), "SceneManager_OnUnloadComplete")]
        [HarmonyPostfix]
        public static void OnUnloadComplete_Prefix(ref string sceneName)
        {
            if (patchedLoadSceneName == true)
            {
                DebugHelper.Log("OnUnloadComplete_Prefix.");
                StartOfRound.Instance.currentLevel.sceneName = oldSceneName;
                oldSceneName = string.Empty;
                patchedLoadSceneName = false;
            }
        }*/

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

        public static List<SelectableLevel> prePatchedLevelsList = new List<SelectableLevel>();

        public static void PatchVanillaLevelLists()
        {
            DebugHelper.Log("Patching Vanilla Level List!");
            Terminal terminal = GameObject.FindAnyObjectByType<Terminal>();
            StartOfRound startOfRound = StartOfRound.Instance;

            foreach (ExtendedLevel vanillaLevel in vanillaLevelsList)
                foreach (CompatibleNoun compatibleRouteNoun in TerminalUtils.RouteKeyword.compatibleNouns)
                    if (compatibleRouteNoun.noun.name.Contains(vanillaLevel.NumberlessPlanetName))
                        vanillaLevel.routePrice = compatibleRouteNoun.result.itemCost;

            List<SelectableLevel> allSelectableLevels = new List<SelectableLevel>();

            foreach (ExtendedLevel extendedLevel in allLevelsList)
                allSelectableLevels.Add(extendedLevel.selectableLevel);

            prePatchedLevelsList = startOfRound.levels.ToList();

            startOfRound.levels = allSelectableLevels.ToArray();
            terminal.moonsCatalogueList = allSelectableLevels.ToArray();

            foreach (ExtendedLevel extendedLevel in allLevelsList)
                DebugHelper.Log("Route Price Check: " + extendedLevel.NumberlessPlanetName +  " - " + extendedLevel.routePrice);

            DebugHelper.Log("StartOfRound Levels List Length Is: " + startOfRound.levels.Length);
            DebugHelper.Log("Terminal Levels List Length Is: " + terminal.moonsCatalogueList.Length);
        }


        [HarmonyPatch(typeof(StartOfRound), "OnPlayerConnectedClientRpc")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        public static void OnPlayerConnectedClientRpc_Postfix()
        {
            DebugHelper.Log("OnPlayerConnectedClientRpc_PostFix");
            if (StartOfRound.Instance.currentLevel != null)
                DebugHelper.Log(StartOfRound.Instance.currentLevel.PlanetName);
            else
                DebugHelper.Log("CurrentLevel Was Null!");
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
                }
                else if (vanillaLevel.NumberlessPlanetName == "Dine")
                {
                    vanillaLevel.levelTags.Add("Snow");
                    vanillaLevel.levelTags.Add("Ice");
                    vanillaLevel.levelTags.Add("Tundra");
                }
                else if (vanillaLevel.NumberlessPlanetName == "Titan")
                {
                    vanillaLevel.levelTags.Add("Snow");
                    vanillaLevel.levelTags.Add("Ice");
                    vanillaLevel.levelTags.Add("Tundra");
                }
            }
        }
    }
}
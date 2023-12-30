using HarmonyLib;
using LethalLib.Extras;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalLib.Modules
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

        /* This class is called levels so I'm putting all the custom level code here.
         * If I need to move it to a seperate class let me know -Skull
         */

        //public static Dictionary<string, CustomLevel> CustomLevelList;
        //public static Dictionary<int, CustomLevel> customLevelsDict;

        public static List<ExtendedSelectableLevel> allLevelsList = new List<ExtendedSelectableLevel>();
        public static List<ExtendedSelectableLevel> vanillaLevelsList = new List<ExtendedSelectableLevel>();
        public static List<ExtendedSelectableLevel> customLevelsList = new List<ExtendedSelectableLevel>();

        private static bool originalListNeedsRefresh;

        [HarmonyPatch(typeof(StartOfRound), "ChangeLevel")]
        [HarmonyPrefix]
        public static void ChangeLevel(int levelID)
        {
            if (levelID >= 9)
                levelID = 0;
        }

        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        [HarmonyPriority(0)]
        public static void InitializeLevelData(StartOfRound __instance)
        {

            StartOfRound startOfRound = __instance; //Redeclared for more readable variable name

            Debug.Log("LethalLib (Batby): Initializing Level Data");
            foreach (SelectableLevel vanillaSelectableLevel in startOfRound.levels)
                new ExtendedSelectableLevel(vanillaSelectableLevel);

            //Our AsssetBundle stuff runs before this function so atleast for now I just manually re-order the allLevelsList so its Vanilla -> Custom

            List<ExtendedSelectableLevel> tempAllLevelsList = new List<ExtendedSelectableLevel>();

            foreach (ExtendedSelectableLevel vanillaLevel in vanillaLevelsList)
                tempAllLevelsList.Add(vanillaLevel);
            foreach (ExtendedSelectableLevel customLevel in customLevelsList)
                tempAllLevelsList.Add(customLevel);

            allLevelsList = new List<ExtendedSelectableLevel>(tempAllLevelsList);



            //ValidateLevelData() //TODO Later

            //TerminalUtils.AddMoonsToCatalogue();
        }

        public static void AddSelectableLevel(ExtendedSelectableLevel extendedLevel)
        {
            if (extendedLevel.levelType == ExtendedSelectableLevel.LevelType.Custom)
                customLevelsList.Add(extendedLevel);
            else
                vanillaLevelsList.Add(extendedLevel);

            allLevelsList.Add(extendedLevel);

            RefreshOriginalLevelList();
        }

        public static void RefreshOriginalLevelList()
        {
            DebugHelper.Log("Refreshing Original Level List!");

            if (StartOfRound.Instance != null && TerminalUtils.Terminal != null)
            {
                List<SelectableLevel> allSelectableLevels = new List<SelectableLevel>();

                foreach (ExtendedSelectableLevel extendedLevel in allLevelsList)
                    allSelectableLevels.Add(extendedLevel.SelectableLevel);

                StartOfRound.Instance.levels = allSelectableLevels.ToArray();
                TerminalUtils.Terminal.moonsCatalogueList = allSelectableLevels.ToArray();

                DebugHelper.DebugAllLevels();
                DebugHelper.DebugInjectedLevels();

                foreach (ExtendedSelectableLevel extendedLevel in customLevelsList)
                    PatchCustomLevel(extendedLevel);
            }
        }

        public static void PatchCustomLevel(ExtendedSelectableLevel extendedLevel)
        {
            DebugHelper.Log("Patching Custom Level: " + extendedLevel.SelectableLevel.PlanetName);
            extendedLevel.SelectableLevel.spawnableScrap = vanillaLevelsList[6].SelectableLevel.spawnableScrap;
            extendedLevel.SelectableLevel.spawnableOutsideObjects = vanillaLevelsList[6].SelectableLevel.spawnableOutsideObjects;
            extendedLevel.SelectableLevel.spawnableMapObjects = vanillaLevelsList[6].SelectableLevel.spawnableMapObjects;
            extendedLevel.SelectableLevel.Enemies = vanillaLevelsList[6].SelectableLevel.Enemies;
            extendedLevel.SelectableLevel.OutsideEnemies = vanillaLevelsList[6].SelectableLevel.OutsideEnemies;
            extendedLevel.SelectableLevel.DaytimeEnemies = vanillaLevelsList[6].SelectableLevel.DaytimeEnemies;
        }

        public static bool TryGetExtendedLevel(SelectableLevel selectableLevel, out ExtendedSelectableLevel returnExtendedLevel)
        {
            returnExtendedLevel = null;

            foreach (ExtendedSelectableLevel extendedLevel in allLevelsList)
                if (extendedLevel.SelectableLevel == selectableLevel)
                    returnExtendedLevel = extendedLevel;

            return (returnExtendedLevel != null);
        }

        public static bool TryGetExtendedLevel(SelectableLevel selectableLevel, out ExtendedSelectableLevel returnExtendedLevel, ExtendedSelectableLevel.LevelType levelType)
        {
            returnExtendedLevel = null;

            if (levelType == ExtendedSelectableLevel.LevelType.Vanilla)
            {
                foreach (ExtendedSelectableLevel extendedLevel in vanillaLevelsList)
                    if (extendedLevel.SelectableLevel == selectableLevel)
                        returnExtendedLevel = extendedLevel;
            }

            else if (levelType == ExtendedSelectableLevel.LevelType.Custom)
            {
                foreach (ExtendedSelectableLevel extendedLevel in customLevelsList)
                    if (extendedLevel.SelectableLevel == selectableLevel)
                        returnExtendedLevel = extendedLevel;
            }

            return (returnExtendedLevel != null);
        }

        /*[HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix]
        public static void InitializeMoon(StartOfRound __instance)
        {
            StartOfRound startOfRound = __instance;

            DebugHelper.Log(startOfRound.currentLevel + " , " + startOfRound.currentLevelID);
            TryGetExtendedLevel(startOfRound.currentLevel, out ExtendedSelectableLevel extendedLevel, levelType: ExtendedSelectableLevel.LevelType.Custom);
            DebugHelper.Log((extendedLevel != null).ToString());
            if (extendedLevel != null)
            {
                //ExtendedSelectableLevel currentCustomLevel = GetCustomLevel(startOfRound.currentLevel);
                Debug.Log(" LethalLib Moon Tools: Loading into level " + extendedLevel.SelectableLevel.PlanetName);

                foreach (GameObject ObjToDestroy in GameObject.FindObjectsOfType<GameObject>())
                {
                    if (ObjToDestroy.name.Contains("Models2VowFactory"))
                        ObjToDestroy.SetActive(false);

                    if (ObjToDestroy.name.Contains("Plane") && (ObjToDestroy.transform.parent.gameObject.name.Contains("Foliage") || ObjToDestroy.transform.parent.gameObject.name.Contains("Mounds")))
                        GameObject.Destroy(ObjToDestroy);

                    foreach (string UnwantedObjString in extendedLevel.CustomLevel.GetDestroyList())
                    {
                        //If the object has any of the names in the list, it's gotta go
                        if (ObjToDestroy.name.Contains(UnwantedObjString))
                        {
                            GameObject.Destroy(ObjToDestroy);
                            continue;
                        }
                    }
                }

                //Load our custom prefab
                GameObject.Instantiate(extendedLevel.CustomLevel.levelPrefab);
            }
        }*/




        /*public static SelectableLevel GetDefaultSelectableLevel()
        {
            SelectableLevel newSelectableLevel = new SelectableLevel();

            if (vanillaLevelsList.Count > 6) //Weird check but I like being safe
            {
                newSelectableLevel.planetPrefab = vanillaLevelsList[2].planetPrefab;
                newSelectableLevel.spawnableMapObjects = vanillaLevelsList[2].spawnableMapObjects;
                newSelectableLevel.spawnableOutsideObjects = vanillaLevelsList[2].spawnableOutsideObjects;
                newSelectableLevel.spawnableScrap = vanillaLevelsList[2].spawnableScrap;
                newSelectableLevel.Enemies = vanillaLevelsList[5].Enemies;
                newSelectableLevel.levelAmbienceClips = vanillaLevelsList[2].levelAmbienceClips;
                newSelectableLevel.OutsideEnemies = vanillaLevelsList[0].OutsideEnemies;
                newSelectableLevel.DaytimeEnemies = vanillaLevelsList[0].DaytimeEnemies;
            }
            else
                Debug.LogError("LethalLib: Failed To Get Default SelectableLevel Settings!");

            return (newSelectableLevel);
        }*/
    }
}
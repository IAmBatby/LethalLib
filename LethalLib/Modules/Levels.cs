using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalLib.Modules
{
    public class Levels {
        [Flags]
        public enum LevelTypes {
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

        private static List<CustomLevel> _customLevelList; //Needs talk about renaming

        public static List<SelectableLevel> allLevelsList;
        public static List<SelectableLevel> vanillaLevelsList;
        public static List<SelectableLevel> customLevelsList;

        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        [HarmonyPriority(0)]
        public static void InitializeLevelData(StartOfRound __instance) {

            StartOfRound startOfRound = __instance; //Redeclared for more readable variable name

            InitalizeVanillaLevelData(startOfRound);
            InitalizeCustomLevelData(startOfRound);
            //ValidateLevelData() //TODO Later

            TerminalUtils.AddMoonsToCatalogue();
        }

        public static void InitalizeVanillaLevelData(StartOfRound startOfRound)
        {
            foreach (SelectableLevel vanillaSelectableLevel in startOfRound.levels)
            {
                allLevelsList.Add(vanillaSelectableLevel);
                vanillaLevelsList.Add(vanillaSelectableLevel);
            }
        }

        public static void InitalizeCustomLevelData(StartOfRound startOfRound)
        {
            foreach (CustomLevel customLevel in _customLevelList)
            {
                if (customLevel.SelectableLevel == null)
                    customLevel.SelectableLevel = GetDefaultSelectableLevel(); //If the CustomLevel doesn't have a supplied SelectableLevel, We make one for it.
                else
                {
                    allLevelsList.Add(customLevel.SelectableLevel);
                    customLevelsList.Add(customLevel.SelectableLevel);
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix]
        public static void InitializeMoon(StartOfRound __instance) {
            StartOfRound startOfRound = __instance;

            if (customLevelsList.Contains(startOfRound.currentLevel))
            {
                CustomLevel currentCustomLevel = GetCustomLevel(startOfRound.currentLevel);
                Debug.Log(" LethalLib Moon Tools: Loading into level " + startOfRound.currentLevel.PlanetName);

                foreach (GameObject ObjToDestroy in GameObject.FindObjectsOfType<GameObject>())
                {
                    if (ObjToDestroy.name.Contains("Models2VowFactory"))
                        ObjToDestroy.SetActive(false);

                    if (ObjToDestroy.name.Contains("Plane") && (ObjToDestroy.transform.parent.gameObject.name.Contains("Foliage") || ObjToDestroy.transform.parent.gameObject.name.Contains("Mounds")))
                        GameObject.Destroy(ObjToDestroy);

                    foreach (string UnwantedObjString in currentCustomLevel.GetDestroyList())
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
                GameObject.Instantiate(currentCustomLevel.LevelPrefab);
            }
        }

        public static CustomLevel GetCustomLevel(SelectableLevel selectableLevel)
        {
            foreach (CustomLevel customLevel in _customLevelList)
                if (customLevel.SelectableLevel == selectableLevel)
                    return (customLevel);

            Debug.LogError("LethalLib: Failed To Find CustomLevel For " + selectableLevel.sceneName);
            return (null);
        }

        public static SelectableLevel GetDefaultSelectableLevel()
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
        }

        public static void AddCustomLevel(CustomLevel newCustomLevel)
        {
            if (!_customLevelList.Contains(newCustomLevel))
                _customLevelList.Add(newCustomLevel);
            else
                Debug.LogError("LethalLib: Failed To Add New CustomLevel, Already Added!");
        }
    }
}

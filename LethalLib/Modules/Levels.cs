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

        public static int timeofdaycount = 0;


        [HarmonyPatch(typeof(StartOfRound), "ChangeLevel")]
        [HarmonyPrefix]
        public static void ChangeLevel_Prefix(int levelID)
        {
            if (levelID >= 9)
                levelID = 0;
        }

        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        [HarmonyPriority(0)]
        public static void Awake_Prefix(StartOfRound __instance)
        {
            StartOfRound startOfRound = __instance; //Redeclared for more readable variable name

            Debug.Log("LethalLib (Batby): Initializing Level Data");
            foreach (SelectableLevel vanillaSelectableLevel in startOfRound.levels)
                new ExtendedLevel(vanillaSelectableLevel);

            //Our AsssetBundle stuff runs before this function so atleast for now I just manually re-order the allLevelsList so its Vanilla -> Custom

            List<ExtendedLevel> tempAllLevelsList = new List<ExtendedLevel>();

            foreach (ExtendedLevel vanillaLevel in vanillaLevelsList)
                tempAllLevelsList.Add(vanillaLevel);
            foreach (ExtendedLevel customLevel in customLevelsList)
                tempAllLevelsList.Add(customLevel);

            allLevelsList = new List<ExtendedLevel>(tempAllLevelsList);
        }

        public static void AddSelectableLevel(ExtendedLevel extendedLevel)
        {
            if (extendedLevel.levelType == ExtendedLevel.LevelType.Custom)
                customLevelsList.Add(extendedLevel);
            else
                vanillaLevelsList.Add(extendedLevel);

            allLevelsList.Add(extendedLevel);

            //PatchVanillaLevelLists();
        }

        public static void PatchVanillaLevelLists()
        {
            DebugHelper.Log("Patching Vanilla Level List!");

            if (StartOfRound.Instance != null && TerminalUtils.Terminal != null)
            {
                List<SelectableLevel> allSelectableLevels = new List<SelectableLevel>();

                foreach (ExtendedLevel extendedLevel in allLevelsList)
                    allSelectableLevels.Add(extendedLevel.selectableLevel);

                StartOfRound.Instance.levels = allSelectableLevels.ToArray();
                TerminalUtils.Terminal.moonsCatalogueList = allSelectableLevels.ToArray();

                foreach (ExtendedLevel extendedLevel in customLevelsList)
                    PatchCustomLevel(extendedLevel);
            }
        }

        //This is a janky function to pull some data from a vanilla level (Dine). Because Unity -> LethalLib scriptableobject references aren't implemented correctly.
        public static void PatchCustomLevel(ExtendedLevel extendedLevel)
        {
            //extendedLevel.selectableLevel.spawnableScrap = vanillaLevelsList[6].selectableLevel.spawnableScrap;
            //extendedLevel.selectableLevel.spawnableOutsideObjects = vanillaLevelsList[6].selectableLevel.spawnableOutsideObjects;
            //extendedLevel.selectableLevel.spawnableMapObjects = vanillaLevelsList[6].selectableLevel.spawnableMapObjects;
            //extendedLevel.selectableLevel.Enemies = vanillaLevelsList[6].selectableLevel.Enemies;
            //extendedLevel.selectableLevel.OutsideEnemies = vanillaLevelsList[6].selectableLevel.OutsideEnemies;
            //extendedLevel.selectableLevel.DaytimeEnemies = vanillaLevelsList[6].selectableLevel.DaytimeEnemies;
        }


        [HarmonyPatch(typeof(TimeOfDay), "Awake")]
        [HarmonyPrefix]
        public static void Awake_Prefix(TimeOfDay __instance)
        {
            DebugHelper.Log("TimeOfDay Spawning!");
            timeofdaycount++;

            if (__instance.currentLevel != null)
                DebugHelper.Log("CurrentLevel Is: " + __instance.currentLevel.PlanetName);
        }

        public static bool fakeTimeStartedThisFrame = false;
        public static SelectableLevel cachedTimeOfDayLevel = null;

        [HarmonyPatch(typeof(TimeOfDay), "Update")]
        [HarmonyPrefix]
        public static void Update_Prefix(TimeOfDay __instance)
        {
            if (__instance.currentLevel != null)
            {
                if (cachedTimeOfDayLevel == null)
                {
                    cachedTimeOfDayLevel = __instance.currentLevel;
                    DebugHelper.Log("TimeOfDay CurrentLevel Changed From (Null) To " + __instance.currentLevel.PlanetName);
                }
                else if (cachedTimeOfDayLevel != __instance.currentLevel)
                {
                    cachedTimeOfDayLevel = __instance.currentLevel;
                    DebugHelper.Log("TimeOfDay CurrentLevel Changed From " + cachedTimeOfDayLevel.PlanetName + " To " + __instance.currentLevel.PlanetName);
                }
            }


            if (fakeTimeStartedThisFrame == false)
            {
                DebugHelper.Log("Time Started This Frame!");
                fakeTimeStartedThisFrame = true;
            }
        }

        public static bool TryGetExtendedLevel(SelectableLevel selectableLevel, out ExtendedLevel returnExtendedLevel, ExtendedLevel.LevelType levelType = ExtendedLevel.LevelType.Any)
        {
            returnExtendedLevel = null;
            List<ExtendedLevel> extendedLevelsList = new List<ExtendedLevel>();

            switch (levelType)
            {
                case ExtendedLevel.LevelType.Vanilla:
                    extendedLevelsList = vanillaLevelsList;
                    break;
                case ExtendedLevel.LevelType.Custom:
                    extendedLevelsList = customLevelsList;
                    break;
                case ExtendedLevel.LevelType.Any:
                    extendedLevelsList = allLevelsList;
                    break;
            }

            foreach (ExtendedLevel extendedLevel in extendedLevelsList)
                if (extendedLevel.selectableLevel == selectableLevel)
                    returnExtendedLevel = extendedLevel;

            return (returnExtendedLevel != null);
        }
    }
}
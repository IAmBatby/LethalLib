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

            RefreshOriginalLevelList();
        }

        public static void RefreshOriginalLevelList()
        {
            DebugHelper.Log("Refreshing Original Level List!");

            if (StartOfRound.Instance != null && TerminalUtils.Terminal != null)
            {
                List<SelectableLevel> allSelectableLevels = new List<SelectableLevel>();

                foreach (ExtendedLevel extendedLevel in allLevelsList)
                    allSelectableLevels.Add(extendedLevel.SelectableLevel);

                StartOfRound.Instance.levels = allSelectableLevels.ToArray();
                TerminalUtils.Terminal.moonsCatalogueList = allSelectableLevels.ToArray();

                DebugHelper.DebugAllLevels();
                DebugHelper.DebugInjectedLevels();

                foreach (ExtendedLevel extendedLevel in customLevelsList)
                    PatchCustomLevel(extendedLevel);
            }
        }

        //This is a janky function to pull some data from a vanilla level (Dine). Because Unity -> LethalLib scriptableobject references aren't implemented correctly.
        public static void PatchCustomLevel(ExtendedLevel extendedLevel)
        {
            DebugHelper.Log("Patching Custom Level: " + extendedLevel.SelectableLevel.PlanetName);
            extendedLevel.SelectableLevel.spawnableScrap = vanillaLevelsList[6].SelectableLevel.spawnableScrap;
            extendedLevel.SelectableLevel.spawnableOutsideObjects = vanillaLevelsList[6].SelectableLevel.spawnableOutsideObjects;
            extendedLevel.SelectableLevel.spawnableMapObjects = vanillaLevelsList[6].SelectableLevel.spawnableMapObjects;
            extendedLevel.SelectableLevel.Enemies = vanillaLevelsList[6].SelectableLevel.Enemies;
            extendedLevel.SelectableLevel.OutsideEnemies = vanillaLevelsList[6].SelectableLevel.OutsideEnemies;
            extendedLevel.SelectableLevel.DaytimeEnemies = vanillaLevelsList[6].SelectableLevel.DaytimeEnemies;
        }

        public static bool TryGetExtendedLevel(SelectableLevel selectableLevel, out ExtendedLevel returnExtendedLevel)
        {
            returnExtendedLevel = null;

            foreach (ExtendedLevel extendedLevel in allLevelsList)
                if (extendedLevel.SelectableLevel == selectableLevel)
                    returnExtendedLevel = extendedLevel;

            return (returnExtendedLevel != null);
        }

        public static bool TryGetExtendedLevel(SelectableLevel selectableLevel, out ExtendedLevel returnExtendedLevel, ExtendedLevel.LevelType levelType)
        {
            returnExtendedLevel = null;

            if (levelType == ExtendedLevel.LevelType.Vanilla)
            {
                foreach (ExtendedLevel extendedLevel in vanillaLevelsList)
                    if (extendedLevel.SelectableLevel == selectableLevel)
                        returnExtendedLevel = extendedLevel;
            }

            else if (levelType == ExtendedLevel.LevelType.Custom)
            {
                foreach (ExtendedLevel extendedLevel in customLevelsList)
                    if (extendedLevel.SelectableLevel == selectableLevel)
                        returnExtendedLevel = extendedLevel;
            }

            return (returnExtendedLevel != null);
        }
    }
}
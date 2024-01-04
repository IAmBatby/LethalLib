using DunGen.Graph;
using HarmonyLib;
using LethalLevelLoader.Extras;
using System;
using System.Collections.Generic;
using UnityEngine;
using static LethalLevelLoader.Modules.Dungeon;

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

            foreach (ExtendedLevel customLevel in Levels.customLevelsList)
                AssetBundleLoader.RestoreVanillaAssetReferences(customLevel.selectableLevel);

            foreach (ExtendedLevel extendedLevel in allLevelsList)
                InjectCustomDungeonsIntoSelectableLevel(extendedLevel);
        }

        public static void AddSelectableLevel(ExtendedLevel extendedLevel)
        {
            DebugHelper.Log("Adding Selectable Level: " + extendedLevel.NumberlessPlanetName);
            if (extendedLevel.levelType == LevelType.Custom)
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

        public static void InjectCustomDungeonsIntoSelectableLevel(ExtendedLevel extendedLevel)
        {
            /*string debugString = "ExtendedLevel: " + extendedLevel.NumberlessPlanetName + " Patched Available DungeonFlow List: " + "\n" + "\n";

            extendedLevel.extendedDungeonFlowsList = new List<(ExtendedDungeonFlow, int)>();

            foreach (IntWithRarity intWithRarity in extendedLevel.selectableLevel.dungeonFlowTypes)
                if (RoundManager.Instance.dungeonFlowTypes[intWithRarity.id] != null)
                    if (Dungeon.TryGetExtendedDungeonFlow(RoundManager.Instance.dungeonFlowTypes[intWithRarity.id], out ExtendedDungeonFlow extendedDungeonFlow, LevelType.Vanilla))
                    {
                        extendedLevel.extendedDungeonFlowsList.Add((extendedDungeonFlow, intWithRarity.rarity));
                        debugString += extendedDungeonFlow.dungeonFlow.name + " | " + intWithRarity.rarity + "\n";
                    }

            foreach (ExtendedDungeonFlow extendedDungeonFlow in Dungeon.customDungeonFlowsList)
            {
                extendedLevel.extendedDungeonFlowsList.Add((extendedDungeonFlow, extendedDungeonFlow.dungeonRarity));
                debugString += extendedDungeonFlow.dungeonFlow.name + " | " + extendedDungeonFlow.dungeonRarity + "\n";
            }

            DebugHelper.Log(debugString + "\n");*/
        }

        public static bool TryGetExtendedLevel(SelectableLevel selectableLevel, out ExtendedLevel returnExtendedLevel, LevelType levelType = LevelType.Any)
        {
            returnExtendedLevel = null;
            List<ExtendedLevel> extendedLevelsList = new List<ExtendedLevel>();

            switch (levelType)
            {
                case LevelType.Vanilla:
                    extendedLevelsList = vanillaLevelsList;
                    break;
                case LevelType.Custom:
                    extendedLevelsList = customLevelsList;
                    break;
                case LevelType.Any:
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
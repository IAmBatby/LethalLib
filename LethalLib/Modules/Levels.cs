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



        [HarmonyPatch(typeof(StartOfRound), "ChangeLevel")]
        [HarmonyPrefix]
        public static void ChangeLevel_Prefix(int levelID) //Gotta look into this properlly
        {
            if (levelID >= 9)
                levelID = 0;
        }

        //This stuff is a little gross idk.
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        [HarmonyPriority(0)]
        public static void Awake_Prefix(StartOfRound __instance)
        {
            StartOfRound startOfRound = __instance; //Redeclared for more readable variable name

            Debug.Log("LethalLib (Batby): Initializing Level Data");
            foreach (SelectableLevel vanillaSelectableLevel in startOfRound.levels)
                new ExtendedLevel(vanillaSelectableLevel, LevelType.Vanilla, generateTerminalAssets: false);

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
            if (extendedLevel.levelType == LevelType.Custom)
                customLevelsList.Add(extendedLevel);
            else
                vanillaLevelsList.Add(extendedLevel);

            allLevelsList.Add(extendedLevel);
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
            }
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

        //Item Dropship Debugging

        public static bool updatecheck;
        [HarmonyPatch(typeof(StartOfRound), "Update")]
        [HarmonyPostfix]
        public static void StartOfRoundUpdate(StartOfRound __instance)
        {
            if (updatecheck == false && __instance.shipHasLanded == true)
            {
                DebugHelper.Log("StartOfRound - Ship Has Landed!");
                updatecheck = true;
            }
        }

        [HarmonyPatch(typeof(ItemDropship), "Update")]
        [HarmonyPrefix]
        public static void ItemDropship_Prefix(ItemDropship __instance)
        {
            //DebugItemDropship(__instance);
        }

        [HarmonyPatch(typeof(ItemDropship), "Update")]
        [HarmonyPostfix]
        public static void ItemDropship_Postfix(ItemDropship __instance)
        {
            DebugItemDropship(__instance);
        }

        public static string previousShipCheck;
        public static int previousShipTimerCheck = -1;

        public static void DebugItemDropship(ItemDropship __instance)
        {
            string debugString = "Is Item Dropship Delivering?: " + __instance.deliveringOrder + "\n";
            debugString += "Is Server?: " + __instance.IsServer + "\n";
            debugString += "Is Owned By Server?: " + __instance.IsOwnedByServer + "\n";
            debugString += "Terminal Ordred Items Count: " + __instance.terminalScript.orderedItemsFromTerminal.Count + "\n";
            debugString += "Is This The Players First Order?: " + __instance.playersFirstOrder.ToString() + "\n";
            debugString += "Has The Ship Landed?: " + __instance.playersManager.shipHasLanded.ToString() + "\n";
            debugString += "Ship Timer Is Currently: " + __instance.shipTimer.ToString() + "\n";

            if (debugString != previousShipCheck && ((int)__instance.shipTimer !=  previousShipTimerCheck))
            {
                DebugHelper.Log(debugString + "\n");
                previousShipCheck = debugString;
                previousShipTimerCheck = (int)__instance.shipTimer;
            }
        }

        [HarmonyPatch(typeof(ItemDropship), "Start")]
        [HarmonyPostfix]
        public static void ItemDropship_PostFix1(ItemDropship __instance)
        {
            DebugHelper.Log("Item Dropship Start() Successfully Ran!" + "\n" +
            "StartOfRound Reference Is: " + __instance.playersManager.ToString() + ". Terminal Reference Is: " + __instance.terminalScript.ToString());
        }

        [HarmonyPatch(typeof(ItemDropship), "LandShipOnServer")]
        [HarmonyPostfix]
        public static void ItemDropship_PostFix2()
        {
            DebugHelper.Log("Item Dropship LandShipOnServer() Successfully Ran!");
        }

        [HarmonyPatch(typeof(ItemDropship), "LandShipClientRpc")]
        [HarmonyPostfix]
        public static void ItemDropship_PostFix()
        {
            DebugHelper.Log("Item Dropship LandShipClientRPC() Successfully Ran!");
        }

    }
}
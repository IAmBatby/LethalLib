using DunGen;
using DunGen.Graph;
using HarmonyLib;
using LethalLevelLoader.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DunGen.Graph.DungeonFlow;
using Random = System.Random;
//using static LethalLib.Modules.Items;

namespace LethalLevelLoader.Modules
{
    [System.Serializable]
    public class ExtendedDungeonFlowWithRarity
    {
        public ExtendedDungeonFlow extendedDungeonFlow;
        public int rarity;

        public ExtendedDungeonFlowWithRarity(ExtendedDungeonFlow newExtendedDungeonFlow, int newRarity)
        {
            extendedDungeonFlow = newExtendedDungeonFlow;
            rarity = newRarity;
        }
    }
    
    public class Dungeon
    {
        public static List<ExtendedDungeonFlow> allExtendedDungeonsList = new List<ExtendedDungeonFlow>();
        public static List<ExtendedDungeonFlow> vanillaDungeonFlowsList = new List<ExtendedDungeonFlow>();
        public static List<ExtendedDungeonFlow> customDungeonFlowsList = new List<ExtendedDungeonFlow>();

        public static void AddExtendedDungeonFlow(ExtendedDungeonFlow extendedDungeonFlow)
        {
            DebugHelper.Log("Adding Dungeon Flow: " + extendedDungeonFlow.dungeonFlow.name);
            if (extendedDungeonFlow.dungeonType == ContentType.Custom)
                customDungeonFlowsList.Add(extendedDungeonFlow);
            else
                vanillaDungeonFlowsList.Add(extendedDungeonFlow);

            allExtendedDungeonsList.Add(extendedDungeonFlow);
        }

        [HarmonyPatch(typeof(DungeonGenerator), "Generate")]
        [HarmonyPrefix]
        public static void Generate_Prefix(DungeonGenerator __instance)
        {
            DebugHelper.Log("Started To Prefix Patch DungeonGenerator Generate!");
            Scene scene = RoundManager.Instance.dungeonGenerator.gameObject.scene;

            if (Levels.TryGetExtendedLevel(RoundManager.Instance.currentLevel, out ExtendedLevel extendedLevel))
            {
                SetDungeonFlow(__instance, extendedLevel);
                PatchFireEscapes(__instance, extendedLevel, scene);
                //LevelLoader.SyncLoadedLevel(scene);
            }
        }

        public static void SetDungeonFlow(DungeonGenerator dungeonGenerator, ExtendedLevel extendedLevel)
        {
            DebugHelper.Log("Setting DungeonFlow!");
            RoundManager roundManager = RoundManager.Instance;

            Random levelRandom = RoundManager.Instance.LevelRandom;

            int randomisedDungeonIndex = -1;

            List<int> randomWeightsList = new List<int>();
            string debugString = "Current Level + (" + extendedLevel.NumberlessPlanetName + ") Weights List: " + "\n" + "\n";

            List<ExtendedDungeonFlowWithRarity> availableExtendedFlowsList = DungeonUtils.GetValidExtendedDungeonFlows(extendedLevel).ToList();

            foreach (ExtendedDungeonFlowWithRarity extendedDungeon in availableExtendedFlowsList)
                randomWeightsList.Add(extendedDungeon.rarity);

            randomisedDungeonIndex = roundManager.GetRandomWeightedIndex(randomWeightsList.ToArray(), levelRandom);

            foreach (ExtendedDungeonFlowWithRarity extendedDungeon in availableExtendedFlowsList)
            {
                debugString += extendedDungeon.extendedDungeonFlow.dungeonFlow.name + " | " + extendedDungeon.rarity;
                if (extendedDungeon.extendedDungeonFlow == availableExtendedFlowsList[randomisedDungeonIndex].extendedDungeonFlow)
                    debugString += " - Selected DungeonFlow" + "\n";
                else
                    debugString += "\n";
            }

            DebugHelper.Log(debugString + "\n");

            dungeonGenerator.DungeonFlow = availableExtendedFlowsList[randomisedDungeonIndex].extendedDungeonFlow.dungeonFlow;
        }

        public static void PatchFireEscapes(DungeonGenerator dungeonGenerator, ExtendedLevel extendedLevel, Scene scene)
        {
            string debugString = "Fire Exit Patch Report, Details Below;" + "\n" + "\n";

            List<EntranceTeleport> entranceTeleports = new List<EntranceTeleport>();
            int fireEscapesAmount = 0;

            foreach (GameObject rootObject in scene.GetRootGameObjects())
                foreach (EntranceTeleport entranceTeleport in rootObject.GetComponentsInChildren<EntranceTeleport>())
                {
                    fireEscapesAmount++;
                    entranceTeleport.dungeonFlowId = 5;
                    entranceTeleport.firstTimeAudio = RoundManager.Instance.firstTimeDungeonAudios[0];
                }

            if (fireEscapesAmount != 0)
                debugString += "EntranceTeleport's Found, " + extendedLevel.NumberlessPlanetName + " Contains " + fireEscapesAmount + " Entrances! ( " + fireEscapesAmount + " Fire Escapes) " + "\n";

            bool foundProp = false;
            Vector2 oldCount = Vector2.zero;
            foreach (GlobalPropSettings globalPropSettings in dungeonGenerator.DungeonFlow.GlobalProps)
            {
                if (globalPropSettings.ID == 1231)
                {
                    globalPropSettings.Count = new IntRange(fireEscapesAmount, fireEscapesAmount);
                    foundProp = true;
                    oldCount = new Vector2(globalPropSettings.Count.Min, globalPropSettings.Count.Max);
                }

                if (foundProp == true)
                    debugString += "Found Fire Escape GlobalProp: (ID: 1231), Modifying Spawnrate Count From (" + oldCount.x + "," + oldCount.y + ") To (" + fireEscapesAmount + "," + fireEscapesAmount + ")" + "\n";
                else
                    debugString += "Fire Escape GlobalProp Could Not Be Found! Fire Escapes Will Not Be Patched!" + "\n";

                DebugHelper.Log(debugString + "\n");
            }
        }

        public static bool TryGetExtendedDungeonFlow(DungeonFlow dungeonFlow, out ExtendedDungeonFlow returnExtendedDungeonFlow, ContentType levelType = ContentType.Any)
        {
            returnExtendedDungeonFlow = null;
            List<ExtendedDungeonFlow> extendedDungeonFlowsList = new List<ExtendedDungeonFlow>();

            switch (levelType)
            {
                case ContentType.Vanilla:
                    extendedDungeonFlowsList = vanillaDungeonFlowsList;
                    break;
                case ContentType.Custom:
                    extendedDungeonFlowsList = customDungeonFlowsList;
                    break;
                case ContentType.Any:
                    extendedDungeonFlowsList = allExtendedDungeonsList;
                    break;
            }

            foreach (ExtendedDungeonFlow extendedDungeonFlow in extendedDungeonFlowsList)
                if (extendedDungeonFlow.dungeonFlow == dungeonFlow)
                    returnExtendedDungeonFlow = extendedDungeonFlow;

            return (returnExtendedDungeonFlow != null);
        }

    }
}

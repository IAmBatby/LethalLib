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
        public static List<CustomDungeonArchetype> customDungeonArchetypes = new List<CustomDungeonArchetype>();
        public static List<CustomGraphLine> customGraphLines = new List<CustomGraphLine>();
        public static Dictionary<string, TileSet> extraTileSets = new Dictionary<string, TileSet>();
        public static Dictionary<string, GameObjectChance> extraRooms = new Dictionary<string, GameObjectChance>();
        public static List<CustomDungeon> customDungeons = new List<CustomDungeon>();

        public static List<ExtendedDungeonFlow> allExtendedDungeonsList = new List<ExtendedDungeonFlow>();
        public static List<ExtendedDungeonFlow> vanillaDungeonFlowsList = new List<ExtendedDungeonFlow>();
        public static List<ExtendedDungeonFlow> customDungeonFlowsList = new List<ExtendedDungeonFlow>();

        public class CustomDungeonArchetype
        {
            public DungeonArchetype archeType;
            public Levels.LevelTypes LevelTypes;
            public int lineIndex = -1;
        }

        public class CustomGraphLine
        {
            public GraphLine graphLine;
            public Levels.LevelTypes LevelTypes;
        }

        public class CustomDungeon
        {
            public int rarity;
            public DungeonFlow dungeonFlow;
            public Levels.LevelTypes LevelTypes;
            public string[] levelOverrides;
            public int dungeonIndex = -1;
            public AudioClip firstTimeDungeonAudio;
        }

        public static void AddExtendedDungeonFlow(ExtendedDungeonFlow extendedDungeonFlow)
        {
            DebugHelper.Log("Adding Dungeon Flow: " + extendedDungeonFlow.dungeonFlow.name);
            if (extendedDungeonFlow.dungeonType == LevelType.Custom)
                customDungeonFlowsList.Add(extendedDungeonFlow);
            else
                vanillaDungeonFlowsList.Add(extendedDungeonFlow);

            allExtendedDungeonsList.Add(extendedDungeonFlow);
        }

        public static ExtendedDungeonFlowWithRarity[] GetValidExtendedDungeonFlows(ExtendedLevel extendedLevel)
        {
            List<ExtendedDungeonFlowWithRarity> initialReturnExtendedDungeonFlowsList = new List<ExtendedDungeonFlowWithRarity>();
            List<ExtendedDungeonFlowWithRarity> returnExtendedDungeonFlowsList = new List<ExtendedDungeonFlowWithRarity>();

            foreach (IntWithRarity intWithRarity in extendedLevel.selectableLevel.dungeonFlowTypes)
                if (RoundManager.Instance.dungeonFlowTypes[intWithRarity.id] != null)
                    if (Dungeon.TryGetExtendedDungeonFlow(RoundManager.Instance.dungeonFlowTypes[intWithRarity.id], out ExtendedDungeonFlow outExtendedDungeonFlow, LevelType.Vanilla))
                        returnExtendedDungeonFlowsList.Add(new ExtendedDungeonFlowWithRarity(outExtendedDungeonFlow, intWithRarity.rarity));
            
            foreach (ExtendedDungeonFlow customDungeonFlow in customDungeonFlowsList)
                initialReturnExtendedDungeonFlowsList.Add(new ExtendedDungeonFlowWithRarity(customDungeonFlow, customDungeonFlow.dungeonRarity));


            foreach (ExtendedDungeonFlowWithRarity returnDungeonFlow in initialReturnExtendedDungeonFlowsList)
                foreach (string levelTag in extendedLevel.levelTags)
                    if (returnDungeonFlow.extendedDungeonFlow.extendedDungeonPreferences.targetedLevelTags.Contains(levelTag))
                        returnExtendedDungeonFlowsList.Add(returnDungeonFlow);


            return (returnExtendedDungeonFlowsList.ToArray());
        }

        [HarmonyPatch(typeof(DungeonGenerator), "Generate")]
        [HarmonyPrefix]
        public static void Generate_Prefix(DungeonGenerator __instance)
        {
            DebugHelper.Log("Started To Prefix Patch DungeonGenerator Generate!");

            if (Levels.TryGetExtendedLevel(RoundManager.Instance.currentLevel, out ExtendedLevel extendedLevel))
            {
                Scene scene = RoundManager.Instance.dungeonGenerator.gameObject.scene;

                SetDungeonFlow(__instance, extendedLevel);
                PatchFireEscapes(__instance, extendedLevel, scene);


                //LevelLoader.SyncLoadedLevel(scene);
            }
        }

        [HarmonyPatch(typeof(DungeonGenerator), "Generate")]
        [HarmonyPostfix]
        public static void Generate_Postfix(DungeonGenerator __instance)
        {
            DebugHelper.Log("Started To Postfix Patch DungeonGenerator Generate!");
        }

        public static void SetDungeonFlow(DungeonGenerator dungeonGenerator, ExtendedLevel extendedLevel)
        {
            DebugHelper.Log("Setting DungeonFlow!");
            RoundManager roundManager = RoundManager.Instance;

            Random levelRandom = RoundManager.Instance.LevelRandom;

            int randomisedDungeonIndex = -1;

            List<int> randomWeightsList = new List<int>();
            string debugString = "Current Level + (" + extendedLevel.NumberlessPlanetName + ") Weights List: " + "\n" + "\n";

            List<ExtendedDungeonFlowWithRarity> availableExtendedFlowsList = GetValidExtendedDungeonFlows(extendedLevel).ToList();

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

        [HarmonyPatch(typeof(DungeonProxy), "AddTile")]
        [HarmonyPostfix]
        public static void TilePrefix(DungeonProxy __instance, TileProxy tile)
        {
            DebugHelper.Log("Tile Spawning: " + tile.Prefab.name);
        }

        public static void PatchFireEscapes(DungeonGenerator dungeonGenerator, ExtendedLevel extendedLevel, Scene scene)
        {
            DebugHelper.Log("Patching Fire Escapes!");
            List<EntranceTeleport> entranceTeleports = new List<EntranceTeleport>();
            int fireEscapesAmount = -1;

            foreach (GameObject rootObject in scene.GetRootGameObjects())
                foreach (EntranceTeleport entranceTeleport in rootObject.GetComponentsInChildren<EntranceTeleport>())
                {
                    fireEscapesAmount++;
                    entranceTeleport.dungeonFlowId = 5;
                    entranceTeleport.firstTimeAudio = RoundManager.Instance.firstTimeDungeonAudios[0];
                }



            foreach (GlobalPropSettings globalPropSettings in dungeonGenerator.DungeonFlow.GlobalProps)
                if (globalPropSettings.ID == 1231)
                    globalPropSettings.Count = new IntRange(fireEscapesAmount, fireEscapesAmount);
        }

        public static bool TryGetExtendedDungeonFlow(DungeonFlow dungeonFlow, out ExtendedDungeonFlow returnExtendedDungeonFlow, LevelType levelType = LevelType.Any)
        {
            returnExtendedDungeonFlow = null;
            List<ExtendedDungeonFlow> extendedDungeonFlowsList = new List<ExtendedDungeonFlow>();

            switch (levelType)
            {
                case LevelType.Vanilla:
                    extendedDungeonFlowsList = vanillaDungeonFlowsList;
                    break;
                case LevelType.Custom:
                    extendedDungeonFlowsList = customDungeonFlowsList;
                    break;
                case LevelType.Any:
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

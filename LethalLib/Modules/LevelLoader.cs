using BepInEx.Logging;
using HarmonyLib;
using System;
using Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using LethalLib.Modules;
using LethalLib.Extras;
using DunGen;
using DunGen.Graph;
using static DunGen.Graph.DungeonFlow;
using System.Collections.Generic;
using Unity.Netcode;
using static LethalLib.Modules.Dungeon;
using Unity.Netcode.Components;

//Jank hotfix to load terrain later so Unity doesn't get overwhelmed.
public class TerrainInfo
{
    public TerrainCollider terrainCollider;
    public TerrainData terrainData;
    public Terrain terrain;
}

public class LevelLoader
{
    public static bool isInGame;
    public static TerrainInfo sceneTerrainInfo;
    public static bool isMoonInjected;

    //Jank hotfix to load terrain later so Unity doesn't get overwhelmed.
    public static int terrainFrameDelay = 0;
    public static int terrainFrameDelayMax = 1200;


    //Tiny temp terrain generated to preload terrain shaders so Unity doesn't get overwhelmed (Credit: Holo)
    public static GameObject terrainfixer;
    private static int width = 256;
    private static int height = 256;
    private static int depth = 20;
    private static float scale = 20f;
    static float[,] GenerateHeights()
    {
        float[,] heights = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heights[x, y] = CalculateHeight(x, y);
            }
        }
        return heights;
    }
    static float CalculateHeight(int x, int y)
    {
        float xCoord = (float)x / width * scale;
        float yCoord = (float)y / height * scale;

        return Mathf.PerlinNoise(xCoord, yCoord);
    }

    [HarmonyPatch(typeof(RoundManager), "Update")]
    [HarmonyPrefix]
    public static void Update_Prefix(RoundManager __instance)
    {
        if (__instance.timeScript == null) //I don't know why but RoundManager loses it's TimeOfDay reference.
            __instance.timeScript = TimeOfDay.Instance;
    }

    [HarmonyPatch(typeof(RoundManager), "Update")]
    [HarmonyPostfix]
    public static void Update_PostFix(RoundManager __instance)
    {
        if (sceneTerrainInfo != null)
        {
            if (terrainFrameDelay == terrainFrameDelayMax)
            {
                EnableTerrain();
                sceneTerrainInfo = null;
                terrainFrameDelay = 0;
            }
            else
                terrainFrameDelay++;
        }
    }

    [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
    [HarmonyPostfix]
    public static void SceneManager_OnLoadComplete1_PostFix()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            foreach (GameObject rootObject in SceneManager.GetSceneAt(i).GetRootGameObjects())
                ContentExtractor.TryExtractAudioMixerGroups(rootObject.GetComponentsInChildren<AudioSource>());

        if (SceneManager.GetSceneByName("SampleSceneRelay") != null)
            PreloadTerrainShader();
        if (SceneManager.GetSceneByName("MainMenu") != null) //IsInGame check to stop us from trying to inject before the intended InitSceneLaunchOptions usage.
            isInGame = true;
        if (SceneManager.GetSceneByName("Level4March") != null)
            if (isInGame == true)
                InjectCustomMoon(SceneManager.GetSceneByName("Level4March"), true);
    }

    public static void InjectCustomMoon(Scene scene, bool disableTerrainOnFirstFrame = false)
    {
        if (terrainfixer != null)
            terrainfixer.SetActive(false);

        if (isMoonInjected == false)
        {
            foreach (GameObject obj in scene.GetRootGameObjects()) //Disable everything in the Scene were injecting into
                obj.SetActive(false);

            if (Levels.TryGetExtendedLevel(StartOfRound.Instance.currentLevel, out ExtendedLevel extendedLevel))
            {
                if (extendedLevel.levelPrefab != null)
                {
                    GameObject mainPrefab = GameObject.Instantiate(extendedLevel.levelPrefab);
                    if (mainPrefab != null)
                    {
                        SceneManager.MoveGameObjectToScene(mainPrefab, scene); //We move and detatch to replicate vanilla moon scene hierarchy.

                        SyncLoadedLevel(mainPrefab);

                        //mainPrefab.transform.DetachChildren();
                        //mainPrefab.SetActive(false);

                        if (disableTerrainOnFirstFrame == true) //Jank hotfix to load terrain later so Unity doesn't get overwhelmed.
                            DisableTerrain();
                    }
                }
            }
            isMoonInjected = true;
            DebugHelper.DebugSelectableLevelReferences(extendedLevel);
        }
    }

    public class CachedChildedNetworkObjectData
    {
        public GameObject childObject;
        public NetworkObject childNetworkObject;
        public Transform childParentTransform;

        public CachedChildedNetworkObjectData(GameObject newChildGameObject, Transform newIntendedParent, NetworkObject newChildNetworkObject)
        { childNetworkObject = newChildNetworkObject;  childObject = newChildGameObject; childParentTransform = newIntendedParent; }
    }

    public static void SyncLoadedLevel(GameObject levelPrefab)
    {
        List<CachedChildedNetworkObjectData> cachedNetworkObjectParentList = new List<CachedChildedNetworkObjectData>();
        List<NetworkObject> networkObjectsList = new List<NetworkObject>(levelPrefab.GetComponentsInChildren<NetworkObject>());

        foreach (NetworkObject networkObject in new List<NetworkObject>(networkObjectsList))
            cachedNetworkObjectParentList.Add(new CachedChildedNetworkObjectData(networkObject.gameObject, networkObject.transform.parent, networkObject));

        foreach (CachedChildedNetworkObjectData cachedNetworkChild in cachedNetworkObjectParentList)
        {
            DebugHelper.Log("Attempting To Parent & Spawn: " + cachedNetworkChild.childObject.name);
            //cachedNetworkChild.childObject.transform.SetParent(null);
            cachedNetworkChild.childNetworkObject.Spawn();
            //cachedNetworkChild.childNetworkObject.TrySetParent(cachedNetworkChild.childParentTransform);
        }
    }

    public static List<(GlobalPropSettings, IntRange)> cachedGlobalPropList = new List<(GlobalPropSettings, IntRange)>();
    public static List<DungeonFlow> cachedManagerDungeonFlowTypes = new List<DungeonFlow>();
    public static List<IntWithRarity> cachedLevelDungeonFlowTypes = new List<IntWithRarity>();

    [HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
    [HarmonyPrefix]
    public static void GenerateNewFloor_PreFix(RoundManager __instance)
    {
        RoundManager roundManager = __instance;
        RuntimeDungeon runtimeDungeon = UnityEngine.Object.FindObjectOfType<RuntimeDungeon>(false);
        List<DungeonFlow> dynamicManagerDungeonFlows;
        List<IntWithRarity> dynamicLevelDungeonFlows;


        //Inject Custom Dungeons

        cachedManagerDungeonFlowTypes = new List<DungeonFlow>(roundManager.dungeonFlowTypes);
        cachedLevelDungeonFlowTypes = new List<IntWithRarity>(roundManager.currentLevel.dungeonFlowTypes);
        //dynamicManagerDungeonFlows = new List<DungeonFlow>(roundManager.dungeonFlowTypes);
        dynamicManagerDungeonFlows = new List<DungeonFlow>();
        //dynamicLevelDungeonFlows = new List<IntWithRarity>(roundManager.currentLevel.dungeonFlowTypes);
        dynamicLevelDungeonFlows = new List<IntWithRarity>();

        DebugHelper.Log("RoundManager DungeonFlows Pre Patch: " + "\n" + DebugHelper.GetDungeonFlowsLog(dynamicManagerDungeonFlows));
        DebugHelper.Log("CurrentLevel DungeonFlows Count Pre Patch: " + "\n" + dynamicLevelDungeonFlows.Count);


        roundManager.currentLevel.dungeonFlowTypes = null;

        foreach (CustomDungeon dungeon in customDungeons)
        {
            dynamicManagerDungeonFlows.Add(dungeon.dungeonFlow);
            IntWithRarity newInt = new IntWithRarity();
            newInt.id = dynamicLevelDungeonFlows.Count;
            newInt.rarity = dungeon.rarity;
            dynamicLevelDungeonFlows.Add(newInt);
        }

        roundManager.dungeonFlowTypes = dynamicManagerDungeonFlows.ToArray();
        roundManager.currentLevel.dungeonFlowTypes = dynamicLevelDungeonFlows.ToArray();

        DebugHelper.Log("RoundManager DungeonFlows Post Patch: " + "\n" + DebugHelper.GetDungeonFlowsLog(dynamicManagerDungeonFlows));
        DebugHelper.Log("CurrentLevel DungeonFlows Count Post Patch: " + "\n" + dynamicLevelDungeonFlows.Count);

        LethalLib.Modules.Dungeon.PrePatchNewFloor(roundManager);

        //Fix Fire Escapes
        if (Levels.TryGetExtendedLevel(StartOfRound.Instance.currentLevel, out ExtendedLevel extendedLevel))
            if (extendedLevel.levelPrefab != null && runtimeDungeon != null && roundManager != null)
                    foreach (DungeonFlow dungeonFlow in roundManager.dungeonFlowTypes)
                        foreach (GlobalPropSettings globalProp in dungeonFlow.GlobalProps)
                            if (globalProp.ID == 1231)
                            {
                                cachedGlobalPropList.Add((globalProp, new IntRange(globalProp.Count.Min, globalProp.Count.Max)));
                                //globalProp.Count = new IntRange(extendedLevel.fireExitsAmount, extendedLevel.fireExitsAmount);
                            }
    }

    [HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
    [HarmonyPostfix]
    public static void GenerateNewFloor_PostFix()
    {
        RoundManager.Instance.dungeonFlowTypes = cachedManagerDungeonFlowTypes.ToArray();
        RoundManager.Instance.currentLevel.dungeonFlowTypes = cachedLevelDungeonFlowTypes.ToArray();

        cachedLevelDungeonFlowTypes.Clear();
        cachedManagerDungeonFlowTypes.Clear();



        foreach ((GlobalPropSettings, IntRange) cachedGlobalProp in cachedGlobalPropList)
            cachedGlobalProp.Item1.Count = cachedGlobalProp.Item2;

        cachedGlobalPropList.Clear();

        Scene scene = SceneManager.GetSceneByName("Level4March");

        List<NetworkAnimator> networkAnimatorList = new List<NetworkAnimator>();

        foreach (GameObject rootObject in scene.GetRootGameObjects())
            foreach (NetworkAnimator networkAnimator in rootObject.GetComponentsInChildren<NetworkAnimator>())
            {
                DebugHelper.Log("Found Illegal NetworkAnimator On GameObject: " + networkAnimator.gameObject.name);
                networkAnimatorList.Add(networkAnimator);
            }

        for (int i = networkAnimatorList.Count; i > 0; i++)
        {
            GameObject gameObject = networkAnimatorList[i].gameObject;
            UnityEngine.Object.DestroyImmediate(networkAnimatorList[i]);
            gameObject.AddComponent<NetworkObject>().Spawn();
        }

        RoundManager.Instance.currentLevel.spawnableMapObjects = Array.Empty<SpawnableMapObject>();

        //TrySyncLoadedLevel(RoundManager.Instance.dungeonGenerator.ga);
    }

    public static void DisableTerrain() //Jank hotfix to load terrain later so Unity doesn't get overwhelmed.
    {
        DebugHelper.Log("Disabling Terrain!");
        Terrain terrain = GameObject.FindObjectOfType<Terrain>();
        TerrainCollider terrainCollider = GameObject.FindObjectOfType<TerrainCollider>();

        if (terrain != null && terrainCollider != null)
        {
            sceneTerrainInfo = new TerrainInfo();
            sceneTerrainInfo.terrainCollider = terrainCollider;
            sceneTerrainInfo.terrainData = terrainCollider.terrainData;
            sceneTerrainInfo.terrain = terrain;

            terrainCollider.terrainData = null;
            terrainCollider.enabled = false;
            terrainCollider.gameObject.SetActive(false);

            terrain.enabled = false;
            terrain.gameObject.SetActive(false);
        }
    }

    public static void EnableTerrain() //Jank hotfix to load terrain later so Unity doesn't get overwhelmed.
    {
        DebugHelper.Log("Enabling Terrain!");

        if (sceneTerrainInfo != null)
        {
            sceneTerrainInfo.terrainCollider.terrainData = sceneTerrainInfo.terrainData;
            sceneTerrainInfo.terrainCollider.enabled = true;
            sceneTerrainInfo.terrainCollider.gameObject.SetActive(true);

            sceneTerrainInfo.terrain.enabled = true;
            sceneTerrainInfo.terrain.gameObject.SetActive(true);
        }
    }

    public static void PreloadTerrainShader() //Tiny temp terrain generated to preload terrain shaders so Unity doesn't get overwhelmed (Credit: Holo)
    {
        DebugHelper.Log("Preloading Terrain Shaders!");
        terrainfixer = new GameObject();
        terrainfixer.name = "terrainfixer";
        terrainfixer.transform.position = new Vector3(0, -500, 0);
        Terrain terrain = terrainfixer.AddComponent<Terrain>();
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, depth, height);
        terrainData.SetHeights(0, 0, GenerateHeights());
        terrain.terrainData = terrainData;
    }
}
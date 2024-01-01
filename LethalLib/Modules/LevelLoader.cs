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

    [HarmonyPatch(typeof(StartOfRound), "StartGame")]
    [HarmonyPrefix]
    public static void StartGame()
    {
        DebugHelper.Log("Starting Game Prefix!");
        DebugHelper.Log("Current Level Is: " + StartOfRound.Instance.currentLevel.PlanetName + " (" + StartOfRound.Instance.currentLevelID + ") ");
        DebugHelper.Log("Current Level SceneName Is: " + StartOfRound.Instance.currentLevel.sceneName);
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
        if (SceneManager.GetSceneByName("InitSceneLaunchOptions") != null)
            if (isInGame == true)
                InjectCustomMoon(SceneManager.GetSceneByName("InitSceneLaunchOptions"), true);
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
                if (extendedLevel.selectableLevel.planetPrefab != null)
                {
                    GameObject mainPrefab = GameObject.Instantiate(extendedLevel.selectableLevel.planetPrefab);
                    if (mainPrefab != null)
                    {
                        SceneManager.MoveGameObjectToScene(mainPrefab, scene); //We move and detatch to replicate vanilla moon scene hierarchy.
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

    public static List<(GlobalPropSettings, IntRange)> cachedGlobalPropList = new List<(GlobalPropSettings, IntRange)>();

    [HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
    [HarmonyPrefix]
    public static void GenerateNewFloor_PreFix(RoundManager __instance)
    {
        RoundManager roundManager = __instance;
        RuntimeDungeon runtimeDungeon = UnityEngine.Object.FindObjectOfType<RuntimeDungeon>(false);
        if (Levels.TryGetExtendedLevel(StartOfRound.Instance.currentLevel, out ExtendedLevel extendedLevel))
            if (extendedLevel.selectableLevel.planetPrefab != null)
            {
                EntranceTeleport[] customMoonEntrances = extendedLevel.selectableLevel.planetPrefab.GetComponentsInChildren<EntranceTeleport>();
                if (runtimeDungeon != null && roundManager != null)
                    foreach (DungeonFlow dungeonFlow in roundManager.dungeonFlowTypes)
                        foreach (GlobalPropSettings globalProp in dungeonFlow.GlobalProps)
                            if (globalProp.ID == 1231)
                            {
                                cachedGlobalPropList.Add((globalProp, new IntRange(globalProp.Count.Min, globalProp.Count.Max)));
                                globalProp.Count = new IntRange(customMoonEntrances.Length - 1, customMoonEntrances.Length - 1); //-1 Because that Array contains the Main Entrance which is handled by a seperate Global Prop.
                            }
            }
    }

    [HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
    [HarmonyPostfix]
    public static void GenerateNewFloor_PostFix()
    {
        foreach ((GlobalPropSettings, IntRange) cachedGlobalProp in cachedGlobalPropList)
            cachedGlobalProp.Item1.Count = cachedGlobalProp.Item2;

        cachedGlobalPropList.Clear();
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
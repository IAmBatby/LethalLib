using BepInEx.Logging;
using HarmonyLib;
using System;
using Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using LethalLib.Modules;
using LethalLib.Extras;

public class TerrainInfo
{
    public TerrainCollider terrainCollider;
    public TerrainData terrainData;
    public Terrain terrain;
}

public class LoadCustomMoon
{
    //public static ManualLogSource Logger;
    public static bool isInGame;
    public static bool customMoonLoaded;
    public static Coroutine startCoroutine;
    public static TerrainInfo sceneTerrainInfo;

    public static int terrainFrameDelay = 0;
    public static int terrainFrameDelayMax = 500;

    /*[HarmonyPatch(typeof(StartOfRound), "OnEnable")]
    [HarmonyPrefix]
    public static void OnEnablePrefix()
    {
        Debug.Log("Enabling connection callbacks in StartOfRound");
        if (NetworkManager.Singleton != null)
        {
            StartOfRound startOfRound = StartOfRound.Instance;
            Debug.Log("Began listening to SceneManager_OnLoadComplete1 on this client");
            try
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += EnableTerrain2;
            }
            catch (Exception arg)
            {
                Debug.LogError(string.Format("Error returned when subscribing to scenemanager callbacks!: {0}", arg));
                GameNetworkManager.Instance.disconnectionReasonMessage = "An error occured when syncing the scene! The host might not have loaded in.";
                GameNetworkManager.Instance.Disconnect();
                return;
            }
            bool isServer = startOfRound.IsServer;
            return;
        }
    }*/

    [HarmonyPatch(typeof(RoundManager), "LoadNewLevel")]
    [HarmonyPrefix]
    public static bool LoadNewLevelPrefix(int randomSeed, SelectableLevel newLevel)
    {
        if (startCoroutine != null)
            return (false);

        if (customMoonLoaded == false)
        {
            Debug.Log("Custom Moon Not Loaded! Waiting!");
            startCoroutine = RoundManager.Instance.StartCoroutine(WaitForSceneToLoad(randomSeed, newLevel));
            return (false);
        }
        else
        {
            Debug.Log("Custom Moon Loaded! Lets Get It!");
            customMoonLoaded = false;
            //RoundManager.Instance.LoadNewLevel(randomSeed, newLevel);
            return (true);
        }
    }

    [HarmonyPatch(typeof(RoundManager), "Update")]
    [HarmonyPostfix]
    public static void Update()
    {
        //if (terrainFrameDelay != terrainFrameDelayMax)
        //Debug.Log("Hijacked Update Running!");
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

    /*[HarmonyPatch(typeof(StartOfRound), "openingDoorsSequence")]
    [HarmonyPrefix]
    public static void openingDoorsSequence()
    {
        if (sceneTerrainInfo != null)
                EnableTerrain();
    }*/

    public static IEnumerator WaitForSceneToLoad(int randomSeed, SelectableLevel newLevel)
    {
        yield return new WaitUntil(() => customMoonLoaded);
        startCoroutine = null;
        RoundManager.Instance.LoadNewLevel(randomSeed, newLevel);
        yield break;///
    }

    [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
    [HarmonyPrefix]
    public static void StartGame()
    {
        DebugHelper.Log("Starting Game Prefix!");
        DebugHelper.Log("Current Level Is: " + StartOfRound.Instance.currentLevel.PlanetName + " (" + StartOfRound.Instance.currentLevelID + ") ");
        DebugHelper.Log("Current Level SceneName Is: " + StartOfRound.Instance.currentLevel.sceneName);
    }

    [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
    [HarmonyPostfix]
    public static void CheckMoon()
    {
        Scene scene;
        if (isInGame == false)
        {
            scene = SceneManager.GetSceneByName("MainMenu");
            if (scene != null)
                isInGame = true;
        }
        else
        {
            //Logger.LogInfo("Checking Moon!");
            scene = SceneManager.GetSceneByName("InitSceneLaunchOptions");
            if (scene != null)
            {
                //Debug.Log("Attempting To Inject Custom Moon: " + Terminal_Patch.newMoons[StartOfRound.Instance.currentLevelID].MoonName);
                InjectCustomMoon(SceneManager.GetSceneByName("InitSceneLaunchOptions"), true);
                Debug.Log("Custom Moon Successfully Injected! Continuing Level Initialization.");
                customMoonLoaded = true;
            }
        }
    }

    public static void InjectCustomMoon(Scene scene, bool disableTerrainOnFirstFrame = false)
    {
        GameObject injectionPrefab;

        foreach (GameObject obj in scene.GetRootGameObjects())
            obj.SetActive(false);

        //GameObject injectionPrefab = StartOfRound.Instance.currentLevel

        if (Levels.TryGetExtendedLevel(StartOfRound.Instance.currentLevel, out ExtendedSelectableLevel extendedLevel))
        {
            injectionPrefab = extendedLevel.CustomLevel.levelPrefab;

            if (injectionPrefab != null)
            {
                GameObject mainPrefab = GameObject.Instantiate(injectionPrefab);
                if (mainPrefab != null)
                {
                    //Logger.LogInfo("Custom Moon Successfully Created! : " + scene.name);

                    SceneManager.MoveGameObjectToScene(mainPrefab, scene);
                    mainPrefab.transform.DetachChildren();
                    mainPrefab.SetActive(false);

                    if (disableTerrainOnFirstFrame == true)
                    {
                        DisableTerrain();
                    }
                }
            }
        }
    }

    public static void DisableTerrain()
    {
        Debug.Log("Attempting To Disable Terrain Related Components!");
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
            Debug.Log("Successfully Disabled Terrain!");
        }
        else
            Debug.Log("Failed To Disable Terrain!");
    }

    public static void EnableTerrain()
    {
        Debug.Log("Attempting To Enable Terrain Related Components!");

        if (sceneTerrainInfo != null)
        {
            sceneTerrainInfo.terrainCollider.terrainData = sceneTerrainInfo.terrainData;
            sceneTerrainInfo.terrainCollider.enabled = true;
            sceneTerrainInfo.terrainCollider.gameObject.SetActive(true);

            sceneTerrainInfo.terrain.enabled = true;
            sceneTerrainInfo.terrain.gameObject.SetActive(true);
            Debug.Log("Successfully Enabled Terrain!");
        }
        else
            Debug.Log("Failed To Enable Terrain!");
    }
}
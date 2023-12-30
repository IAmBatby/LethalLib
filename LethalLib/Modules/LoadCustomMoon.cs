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

    public static GameObject terrainfixer;

    public static bool isMoonInjected;

    public static int terrainFrameDelay = 0;
    public static int terrainFrameDelayMax = 1200;

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


    [HarmonyPatch(typeof(RoundManager), "Start")]
    [HarmonyPrefix]
    public static void RoundManagerStart(RoundManager __instance)
    {
        DebugHelper.Log("TimeOfDay Is: " + (__instance.timeScript != null));
        __instance.timeScript = TimeOfDay.Instance;
        DebugHelper.Log("TimeOfDay Is: " + (__instance.timeScript != null));
    }

    [HarmonyPatch(typeof(RoundManager), "Update")]
    [HarmonyPrefix]
    public static void RoundManagerUpdatePrefix(RoundManager __instance)
    {

        if (__instance.timeScript == null)
            __instance.timeScript = TimeOfDay.Instance;

    }

    [HarmonyPatch(typeof(RoundManager), "Update")]
    [HarmonyPostfix]
    public static void RoundManagerUpdatePostfix(RoundManager __instance)
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

    public static IEnumerator WaitForSceneToLoad(int randomSeed, SelectableLevel newLevel)
    {
        yield return new WaitUntil(() => customMoonLoaded);
        startCoroutine = null;
        RoundManager.Instance.LoadNewLevel(randomSeed, newLevel);
        yield break;///
    }

    [HarmonyPatch(typeof(StartOfRound), "StartGame")]
    [HarmonyPrefix]
    public static void StartGame()
    {
        DebugHelper.Log("Starting Game Prefix!");
        DebugHelper.Log("Current Level Is: " + StartOfRound.Instance.currentLevel.PlanetName + " (" + StartOfRound.Instance.currentLevelID + ") ");
        DebugHelper.Log("Current Level SceneName Is: " + StartOfRound.Instance.currentLevel.sceneName);

        //CheckMoon();
    }

    [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
    [HarmonyPostfix]
    public static void CheckMoon()
    {
        Scene scene;

        scene = SceneManager.GetSceneByName("SampleSceneRelay");
        if (scene != null)
        {
            DebugHelper.Log("Attempting To Load TerrainShader");
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
                if (terrainfixer != null)
                {
                    terrainfixer.SetActive(false);
                    //GameObject.Destroy(terrainfixer);
                }
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

        if (isMoonInjected == false)
        {
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

            isMoonInjected = true;
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
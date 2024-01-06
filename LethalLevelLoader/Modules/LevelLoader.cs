using BepInEx.Logging;
using HarmonyLib;
using System;
using Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using LethalLevelLoader.Modules;
using LethalLevelLoader.Extras;
using DunGen;
using DunGen.Graph;
using static DunGen.Graph.DungeonFlow;
using System.Collections.Generic;
using Unity.Netcode;
using static LethalLevelLoader.Modules.Dungeon;
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

    [HarmonyPatch(typeof(RoundManager), "Update")]
    [HarmonyPrefix]
    public static void Update_Prefix(RoundManager __instance)
    {
        if (__instance.timeScript == null) //I don't know why but RoundManager loses it's TimeOfDay reference.
            __instance.timeScript = TimeOfDay.Instance;
    }

    [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
    [HarmonyPostfix]
    public static void SceneManager_OnLoadComplete1_PostFix()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            foreach (GameObject rootObject in SceneManager.GetSceneAt(i).GetRootGameObjects())
                ContentExtractor.TryExtractAudioMixerGroups(rootObject.GetComponentsInChildren<AudioSource>());

        DebugHelper.Log("OnLoadComplete. CurrentLevelSceneName: " + RoundManager.Instance.currentLevel.sceneName + " | " + "InjectionSceneName: " + Levels.injectionSceneName);

        if (SceneManager.GetSceneByName("MainMenu") != null) //IsInGame check to stop us from trying to inject before the intended InitSceneLaunchOptions usage.
            isInGame = true;
        if (SceneManager.GetSceneByName(Levels.injectionSceneName) != null)
            if (Levels.TryGetExtendedLevel(StartOfRound.Instance.currentLevel, out ExtendedLevel extendedLevel, ContentType.Custom))
                InjectCustomMoon(SceneManager.GetSceneByName(Levels.injectionSceneName), extendedLevel, false);
    }
    public static void InjectCustomMoon(Scene scene, ExtendedLevel extendedLevel, bool disableTerrainOnFirstFrame = false)
    {
        StartGame_Prefix();
        if (isMoonInjected == false)
        {
            foreach (GameObject obj in scene.GetRootGameObjects()) //Disable everything in the Scene were injecting into
                obj.SetActive(false);

                if (extendedLevel.levelPrefab != null)
                {
                    GameObject mainPrefab = GameObject.Instantiate(extendedLevel.levelPrefab);
                    if (mainPrefab != null)
                    {
                        SceneManager.MoveGameObjectToScene(mainPrefab, scene); //We move and detatch to replicate vanilla moon scene hierarchy.
                        if (RoundManager.Instance.IsServer)
                            SpawnNetworkObjects(mainPrefab.scene);
                    }
                }
            isMoonInjected = true;
            DebugHelper.DebugSelectableLevelReferences(extendedLevel);
        }
    }

    public static void SpawnNetworkObjects(Scene scene)
    {
        int debugCounter = 0;
        foreach (GameObject rootObject in scene.GetRootGameObjects())
            foreach (NetworkObject networkObject in rootObject.GetComponentsInChildren<NetworkObject>())
                if (networkObject.IsSpawned == false)
                {
                    networkObject.Spawn();
                    debugCounter++;
                }

        DebugHelper.Log("Spawned " + debugCounter + " NetworkObject's Found In Injected Moon Prefab");
    }

    [HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
    [HarmonyPrefix]
    public static void ShipHasLeft_Prefix(StartOfRound __instance)
    {
        DebugHelper.Log("ShipHasLeft Prefix.");
        DebugHelper.Log("Scene #1 Is: " + SceneManager.GetSceneAt(1).name);
        DebugHelper.Log("Connected Players Count Is: " + GameNetworkManager.Instance.connectedPlayers);
    }

    [HarmonyPatch(typeof(StartOfRound), "EndOfGameClientRpc")]
    [HarmonyPrefix]
    public static void EndOfGameClientRpc()
    {
        DebugHelper.Log("EndOfGameClientRpc Prefix.");
    }

    public static void StartGame_Prefix()
    {
        NetworkManager networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();

        int counter = 0;
        foreach (NetworkPrefab networkPrefab in networkManager.NetworkConfig.Prefabs.m_Prefabs)
        {
            if (networkPrefab != null)
            {
                if (networkPrefab.Prefab != null)
                {
                    NetworkObject networkObject = networkPrefab.Prefab.GetComponentInChildren<NetworkObject>();
                    if (networkObject != null)
                    {
                        DebugHelper.Log("Registered NetworkPrefab #" + counter + ": " + networkPrefab.Prefab.name + " (ID: " + networkObject.NetworkObjectId + ")");
                        counter++;
                    }
                    else
                    {
                        DebugHelper.Log("Registered NetworkPrefab #" + counter + ": " + networkPrefab.Prefab.name + " (Missing ID!)");
                        counter++;
                    }
                }
                else
                {
                    DebugHelper.Log("Registered NetworkPrefab #" + counter + " Is Missing A Prefab Reference!");
                    counter++;
                }
            }
            else
            {
                DebugHelper.Log("Registered NetworkPrefab Was Null!");
                counter++;
            }
        }
    }

}
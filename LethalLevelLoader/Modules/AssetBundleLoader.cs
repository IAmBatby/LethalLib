using DunGen;
using DunGen.Graph;
using HarmonyLib;
using LethalLevelLoader.Extras;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace LethalLevelLoader.Modules
{
    //A way to automatically pull custom content from AssetBundles if they fit the requirements.
    //Assets pulled from bundles will be sent to the pre-existing functions
    // This is only a helper and won't do anything that can't be done manually
    public static class AssetBundleLoader
    {
        public static string specifiedFileExtension = string.Empty;

        public static DirectoryInfo lethalLibFile = new DirectoryInfo(Assembly.GetExecutingAssembly().Location);
        public static DirectoryInfo lethalLibFolder;
        public static DirectoryInfo pluginsFolder;

        public static List<(SelectableLevel, GameObject)> obtainedSelectableLevelsList = new List<(SelectableLevel, GameObject)>();
        public static List<ExtendedLevel> obtainedExtendedLevelsList = new List<ExtendedLevel>();

        //RoundManager Awake is pretty much the earliest we can safely mess with stuff.
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        public static void StartOfRoundAwake_Postfix(StartOfRound __instance)
        {

            CreateVanillaExtendedLevels(__instance);
            CreateVanillaExtendedDungeonFlows();
            ProcessBundleContent();
        }

        public static void FindBundles()
        {
            lethalLibFolder = lethalLibFile.Parent;
            pluginsFolder = lethalLibFile.Parent.Parent;
            specifiedFileExtension = "*.lethalbundle";

            foreach (string file in Directory.GetFiles(pluginsFolder.FullName, specifiedFileExtension, SearchOption.AllDirectories))
                LoadBundle(file);
        }

        public static void LoadBundle(string bundleFile)
        {
            AssetBundle newBundle = AssetBundle.LoadFromFile(bundleFile);

            if (newBundle != null)
            {
                DebugHelper.Log("Loading Custom Content From Bundle: " + newBundle.name);

                foreach (ExtendedLevel extendedLevel in newBundle.LoadAllAssets<ExtendedLevel>())
                    obtainedExtendedLevelsList.Add(extendedLevel);

                foreach (SelectableLevel selectableLevel in newBundle.LoadAllAssets<SelectableLevel>())
                    foreach (GameObject gameObject in newBundle.LoadAllAssets<GameObject>())
                        if (!Levels.AllSelectableLevelsList.Contains(selectableLevel))
                            if (gameObject.name == ExtendedLevel.GetNumberlessPlanetName(selectableLevel))
                                obtainedSelectableLevelsList.Add((selectableLevel, gameObject));
            }
        }

        public static Tile[] GetAllTilesInDungeonFlow(DungeonFlow dungeonFlow)
        {
            List<Tile> tilesList = new List<Tile>();

            foreach (GraphNode dungeonNode in dungeonFlow.Nodes)
                foreach (TileSet dungeonTileSet in dungeonNode.TileSets)
                    foreach (GameObjectChance dungeonTileWeight in dungeonTileSet.TileWeights.Weights)
                        foreach (Tile dungeonTile in dungeonTileWeight.Value.GetComponentsInChildren<Tile>())
                            tilesList.Add(dungeonTile);

            foreach (GraphLine dungeonLine in dungeonFlow.Lines)
                foreach (DungeonArchetype dungeonArchetype in dungeonLine.DungeonArchetypes)
                {
                    foreach (TileSet dungeonTileSet in dungeonArchetype.BranchCapTileSets)
                        foreach (GameObjectChance dungeonTileWeight in dungeonTileSet.TileWeights.Weights)
                            foreach (Tile dungeonTile in dungeonTileWeight.Value.GetComponentsInChildren<Tile>())
                                tilesList.Add(dungeonTile);

                    foreach (TileSet dungeonTileSet in dungeonArchetype.TileSets)
                        foreach (GameObjectChance dungeonTileWeight in dungeonTileSet.TileWeights.Weights)
                            foreach (Tile dungeonTile in dungeonTileWeight.Value.GetComponentsInChildren<Tile>())
                                tilesList.Add(dungeonTile);
                }

            return (tilesList.ToArray());
        }

        public static RandomMapObject[] GetAllMapObjectsInTiles(Tile[] tiles)
        {
            List<RandomMapObject> returnList = new List<RandomMapObject>();

            foreach (Tile dungeonTile in tiles)
                foreach (RandomMapObject randomMapObject in dungeonTile.gameObject.GetComponentsInChildren<RandomMapObject>())
                {
                    DebugHelper.Log("Found RandomMapObject: " + randomMapObject.name);
                    returnList.Add(randomMapObject);
                }

            return (returnList.ToArray());
        }

        public static SpawnSyncedObject[] GetAllSpawnSyncedObjectsInTiles(Tile[] tiles)
        {
            List<SpawnSyncedObject> returnList = new List<SpawnSyncedObject>();

            foreach (Tile dungeonTile in tiles)
            {
                foreach (Doorway dungeonDoorway in dungeonTile.gameObject.GetComponentsInChildren<Doorway>())
                {
                    foreach (GameObjectWeight doorwayTileWeight in dungeonDoorway.ConnectorPrefabWeights)
                        foreach (SpawnSyncedObject spawnSyncedObject in doorwayTileWeight.GameObject.GetComponentsInChildren<SpawnSyncedObject>())
                            returnList.Add(spawnSyncedObject);

                    foreach (GameObjectWeight doorwayTileWeight in dungeonDoorway.BlockerPrefabWeights)
                        foreach (SpawnSyncedObject spawnSyncedObject in doorwayTileWeight.GameObject.GetComponentsInChildren<SpawnSyncedObject>())
                            returnList.Add(spawnSyncedObject);
                }

                foreach (SpawnSyncedObject spawnSyncedObject in dungeonTile.gameObject.GetComponentsInChildren<SpawnSyncedObject>())
                    returnList.Add(spawnSyncedObject);
            }


            return (returnList.ToArray());
        }

        public static void RegisterDungeonContent(DungeonFlow dungeonFlow)
        {
            Tile[] allTiles = GetAllTilesInDungeonFlow(dungeonFlow);

            foreach (SpawnSyncedObject spawnSyncedObject in GetAllSpawnSyncedObjectsInTiles(allTiles))
                RegisterSpawnSyncedObject(spawnSyncedObject);
        }

        public static void RegisterSpawnSyncedObject(SpawnSyncedObject spawnSyncedObject)
        {
            if (spawnSyncedObject.spawnPrefab.GetComponent<NetworkObject>() == null)
                spawnSyncedObject.spawnPrefab.AddComponent<NetworkObject>();
            NetworkPrefabs.RegisterNetworkPrefab(spawnSyncedObject.spawnPrefab);
        }

        public static void ProcessBundleContent()
        {
            foreach (ExtendedLevel extendedLevel in obtainedExtendedLevelsList)
            {
                ExtendedLevel.ProcessCustomLevel(extendedLevel);
                Levels.AddSelectableLevel(extendedLevel);
            }

            foreach ((SelectableLevel, GameObject) selectableLevel in obtainedSelectableLevelsList)
            {
                ExtendedLevel extendedLevel = ScriptableObject.CreateInstance<ExtendedLevel>();
                extendedLevel.Initialize(selectableLevel.Item1, ContentType.Custom, generateTerminalAssets: true, newLevelPrefab: selectableLevel.Item2, newSourceName: "fixlater");
                ExtendedLevel.ProcessCustomLevel(extendedLevel);
                Levels.AddSelectableLevel(extendedLevel);
            }

            DebugHelper.DebugAllLevels();
        }

        public static void RestoreVanillaDungeonAssetReferences(ExtendedDungeonFlow extendedDungeonFlow)
        {
            Tile[] allTiles = GetAllTilesInDungeonFlow(extendedDungeonFlow.dungeonFlow);

            foreach (RandomMapObject randomMapObject in GetAllMapObjectsInTiles(allTiles))
            {
                foreach (GameObject spawnablePrefab in new List<GameObject>(randomMapObject.spawnablePrefabs))
                    foreach (GameObject vanillaPrefab in ContentExtractor.vanillaSpawnableInsideMapObjectsList)
                        if (spawnablePrefab.name == vanillaPrefab.name)
                        {
                            DebugHelper.Log("Replacing Dungeon RandomSpawnablePrefab " + spawnablePrefab.name + " With: " + vanillaPrefab.name);
                            int index = randomMapObject.spawnablePrefabs.IndexOf(spawnablePrefab);
                            randomMapObject.spawnablePrefabs[index] = vanillaPrefab;
                        }
            }
        }

        public static void RestoreVanillaLevelAssetReferences(ExtendedLevel extendedLevel)
        {

            AudioSource[] moonAudioSources = extendedLevel.levelPrefab.GetComponentsInChildren<AudioSource>();

            DebugHelper.Log("Found " + moonAudioSources.Length + " AudioSources In Custom Moon: " + extendedLevel.NumberlessPlanetName);

            foreach (AudioSource audioSource in moonAudioSources)
            {
                if (audioSource.outputAudioMixerGroup == null)
                {
                    audioSource.outputAudioMixerGroup = ContentExtractor.vanillaAudioMixerGroupsList[0];
                    DebugHelper.Log("AudioGroupMixer Reference Inside " + audioSource.name + " Was Null, Assigning Master SFX Mixer For Safety!");
                }
            }

            foreach (SpawnableItemWithRarity spawnableItem in extendedLevel.selectableLevel.spawnableScrap)
                foreach (Item vanillaItem in ContentExtractor.vanillaItemsList)
                    if (spawnableItem.spawnableItem.itemName == vanillaItem.itemName)
                        spawnableItem.spawnableItem = vanillaItem;

            foreach (SpawnableEnemyWithRarity spawnableEnemy in extendedLevel.selectableLevel.Enemies)
                foreach (EnemyType vanillaEnemy in ContentExtractor.vanillaEnemiesList)
                    if (spawnableEnemy.enemyType != null && spawnableEnemy.enemyType.enemyName == vanillaEnemy.enemyName)
                        spawnableEnemy.enemyType = vanillaEnemy;

            foreach (SpawnableEnemyWithRarity enemyType in extendedLevel.selectableLevel.OutsideEnemies)
                foreach (EnemyType vanillaEnemyType in ContentExtractor.vanillaEnemiesList)
                    if (enemyType.enemyType != null && enemyType.enemyType.enemyName == vanillaEnemyType.enemyName)
                        enemyType.enemyType = vanillaEnemyType;

            foreach (SpawnableEnemyWithRarity enemyType in extendedLevel.selectableLevel.DaytimeEnemies)
                foreach (EnemyType vanillaEnemyType in ContentExtractor.vanillaEnemiesList)
                    if (enemyType.enemyType != null && enemyType.enemyType.enemyName == vanillaEnemyType.enemyName)
                        enemyType.enemyType = vanillaEnemyType;

            foreach (SpawnableMapObject spawnableMapObject in extendedLevel.selectableLevel.spawnableMapObjects)
                foreach (GameObject vanillaSpawnableMapObject in ContentExtractor.vanillaSpawnableInsideMapObjectsList)
                    if (spawnableMapObject.prefabToSpawn != null && spawnableMapObject.prefabToSpawn.name == vanillaSpawnableMapObject.name)
                        spawnableMapObject.prefabToSpawn = vanillaSpawnableMapObject;

            foreach (SpawnableOutsideObjectWithRarity spawnableOutsideObject in extendedLevel.selectableLevel.spawnableOutsideObjects)
                foreach (SpawnableOutsideObject vanillaSpawnableOutsideObject in ContentExtractor.vanillaSpawnableOutsideMapObjectsList)
                    if (spawnableOutsideObject.spawnableObject != null && spawnableOutsideObject.spawnableObject.name == vanillaSpawnableOutsideObject.name)
                        spawnableOutsideObject.spawnableObject = vanillaSpawnableOutsideObject;

            foreach (LevelAmbienceLibrary vanillaAmbienceLibrary in ContentExtractor.vanillaAmbienceLibrariesList)
                if (extendedLevel.selectableLevel.levelAmbienceClips != null && extendedLevel.selectableLevel.levelAmbienceClips.name == vanillaAmbienceLibrary.name)
                    extendedLevel.selectableLevel.levelAmbienceClips = vanillaAmbienceLibrary;
            
        }

        public static void CreateVanillaExtendedLevels(StartOfRound startOfRound)
        {
            DebugHelper.Log("Creating ExtendedLevels For Vanilla SelectableLevels");

            foreach (SelectableLevel selectableLevel in startOfRound.levels)
            {
                DebugHelper.Log("Moons SelectableLevel Is: " + (selectableLevel != null));
                ExtendedLevel extendedLevel = ScriptableObject.CreateInstance<ExtendedLevel>();
                extendedLevel.Initialize(selectableLevel, ContentType.Vanilla, generateTerminalAssets: false);
                Levels.AddSelectableLevel(extendedLevel);
            }
        }

        public static void CreateVanillaExtendedDungeonFlows()
        {
            DebugHelper.Log("Creating ExtendedDungeonFlows For Vanilla DungeonFlows");

            foreach (DungeonFlow dungeonFlow in RoundManager.Instance.dungeonFlowTypes)
            {
                ExtendedDungeonFlow extendedDungeonFlow = ScriptableObject.CreateInstance<ExtendedDungeonFlow>();
                extendedDungeonFlow.Initialize(dungeonFlow, null, ContentType.Vanilla, "Lethal Company");
                Dungeon.AddExtendedDungeonFlow(extendedDungeonFlow);
                //Gotta assign the right audio later.
            }
        }
    }
}

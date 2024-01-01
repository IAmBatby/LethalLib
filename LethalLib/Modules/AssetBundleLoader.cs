using HarmonyLib;
using LethalLib.Extras;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LethalLib.Modules
{
    //A way to automatically pull custom content from AssetBundles if they fit the requirements.
    //Assets pulled from bundles will be sent to the pre-existing functions
    // This is only a helper and won't do anything that can't be done manually
    class AssetBundleLoader
    {
        public static string specifiedFileExtension = string.Empty;

        public static DirectoryInfo lethalLibFile = new DirectoryInfo(Assembly.GetExecutingAssembly().Location);
        public static DirectoryInfo lethalLibFolder;
        public static DirectoryInfo pluginsFolder;

        //RoundManager Awake is pretty much the earliest we can safely mess with stuff.
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.Awake))]
        [HarmonyPostfix]
        public static void RoundManagerAwake_Postfix()
        {
            FindBundles();
        }

        //StartOfRound stores a levels list assigned in Unity Editor, So we need to update that as soon as possible.
        //This is also the earliest place we can access the Vanilla SelectableLevels. So we scrape vanilla and restore references here.
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        [HarmonyPostfix]
        public static void StartOfRoundAwake_Postfix(StartOfRound __instance)
        {
            foreach (ExtendedLevel customLevel in Levels.customLevelsList)
                RestoreVanillaAssetReferences(customLevel.selectableLevel);

            __instance.levels = Levels.AllSelectableLevelsList.ToArray();
        }

        //Terminal stores a levels list assigned in Unity Editor, So we need to update that as soon as possible.
        [HarmonyPatch(typeof(Terminal), nameof(Terminal.Awake))]
        [HarmonyPostfix]
        public static void TerminalAwake_Postfix(Terminal __instance)
        {
            __instance.moonsCatalogueList = Levels.AllSelectableLevelsList.ToArray();
        }

        public static void FindBundles()
        {
            lethalLibFolder = lethalLibFile.Parent;
            pluginsFolder = lethalLibFile.Parent.Parent;
            specifiedFileExtension = "*.lem";

            foreach (string file in Directory.GetFiles(pluginsFolder.FullName, specifiedFileExtension, SearchOption.AllDirectories))
                LoadBundle(file);
        }

        public static void LoadBundle(string bundleFile)
        {
            AssetBundle newBundle = AssetBundle.LoadFromFile(bundleFile);

            if (newBundle != null)
                foreach (SelectableLevel selectableLevel in newBundle.LoadAllAssets<SelectableLevel>())
                    new ExtendedLevel(selectableLevel, LevelType.Custom, generateTerminalAssets: true, newBundle.name);
        }

        public static void RestoreVanillaAssetReferences(SelectableLevel selectableLevel)
        {

            AudioSource[] moonAudioSources = selectableLevel.planetPrefab.GetComponentsInChildren<AudioSource>();

            DebugHelper.Log("Found " + moonAudioSources.Length + " AudioSources In Custom Moon: " + selectableLevel.PlanetName);

            foreach (AudioSource audioSource in moonAudioSources)
            {
                if (audioSource.outputAudioMixerGroup == null)
                {
                    audioSource.outputAudioMixerGroup = ContentExtractor.vanillaAudioMixerGroupsList[0];
                    DebugHelper.Log("AudioGroupMixer Reference Inside " + audioSource.name + " Was Null, Assigning Master SFX Mixer For Safety!");
                }
            }

            foreach (SpawnableItemWithRarity spawnableItem in selectableLevel.spawnableScrap)
                foreach (Item vanillaItem in ContentExtractor.vanillaItemsList)
                    if (spawnableItem.spawnableItem.itemName == vanillaItem.itemName)
                        spawnableItem.spawnableItem = vanillaItem;

            foreach (SpawnableEnemyWithRarity spawnableEnemy in selectableLevel.Enemies)
                foreach (EnemyType vanillaEnemy in ContentExtractor.vanillaEnemiesList)
                    if (spawnableEnemy.enemyType != null && spawnableEnemy.enemyType.enemyName == vanillaEnemy.enemyName)
                        spawnableEnemy.enemyType = vanillaEnemy;

            foreach (SpawnableEnemyWithRarity enemyType in selectableLevel.OutsideEnemies)
                foreach (EnemyType vanillaEnemyType in ContentExtractor.vanillaEnemiesList)
                    if (enemyType.enemyType != null && enemyType.enemyType.enemyName == vanillaEnemyType.enemyName)
                        enemyType.enemyType = vanillaEnemyType;

            foreach (SpawnableEnemyWithRarity enemyType in selectableLevel.DaytimeEnemies)
                foreach (EnemyType vanillaEnemyType in ContentExtractor.vanillaEnemiesList)
                    if (enemyType.enemyType != null && enemyType.enemyType.enemyName == vanillaEnemyType.enemyName)
                        enemyType.enemyType = vanillaEnemyType;

            foreach (SpawnableMapObject spawnableMapObject in selectableLevel.spawnableMapObjects)
                foreach (GameObject vanillaSpawnableMapObject in ContentExtractor.vanillaSpawnableInsideMapObjectsList)
                    if (spawnableMapObject.prefabToSpawn != null && spawnableMapObject.prefabToSpawn.name == vanillaSpawnableMapObject.name)
                        spawnableMapObject.prefabToSpawn = vanillaSpawnableMapObject;

            foreach (SpawnableOutsideObjectWithRarity spawnableOutsideObject in selectableLevel.spawnableOutsideObjects)
                foreach (SpawnableOutsideObject vanillaSpawnableOutsideObject in ContentExtractor.vanillaSpawnableOutsideMapObjectsList)
                    if (spawnableOutsideObject.spawnableObject != null && spawnableOutsideObject.spawnableObject.name == vanillaSpawnableOutsideObject.name)
                        spawnableOutsideObject.spawnableObject = vanillaSpawnableOutsideObject;

            foreach (LevelAmbienceLibrary vanillaAmbienceLibrary in ContentExtractor.vanillaAmbienceLibrariesList)
                if (selectableLevel.levelAmbienceClips != null && selectableLevel.levelAmbienceClips.name == vanillaAmbienceLibrary.name)
                    selectableLevel.levelAmbienceClips = vanillaAmbienceLibrary;
            
        }
    }
}

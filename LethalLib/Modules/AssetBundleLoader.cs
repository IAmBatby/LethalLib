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

        public static void Init()
        {
            //On.RoundManager.Start += RoundManager_Start;
        }

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
            {
                RestoreVanillaAssetReferences(customLevel.selectableLevel);
            }

            __instance.levels = Levels.AllSelectableLevelsList.ToArray();
        }

        //Terminal stores a levels list assigned in Unity Editor, So we need to update that as soon as possible.
        [HarmonyPatch(typeof(Terminal), nameof(Terminal.Awake))]
        [HarmonyPostfix]
        public static void TerminalAwake_Postfix(Terminal __instance)
        {
            __instance.moonsCatalogueList = Levels.AllSelectableLevelsList.ToArray();
        }

        //Terminal stores a levels list assigned in Unity Editor, So we need to update that as soon as possible.
        [HarmonyPatch(typeof(Terminal), nameof(Terminal.Start))]
        [HarmonyPostfix]
        public static void TerminalStart_Postfix(Terminal __instance)
        {
            foreach (ExtendedLevel customLevel in Levels.customLevelsList)
                TerminalUtils.CreateLevelTerminalData(customLevel);
        }

        public static void FindBundles()
        {
            lethalLibFolder = lethalLibFile.Parent;
            pluginsFolder = lethalLibFile.Parent.Parent;
            specifiedFileExtension = "*.lem";

            DebugHelper.Log("LethaLib Folder Location: " + lethalLibFolder);
            DebugHelper.Log("Plugins Folder Location: " + pluginsFolder);

            if (specifiedFileExtension == string.Empty)
                foreach (string file in Directory.GetFiles(pluginsFolder.FullName))
                    DebugHelper.Log("Found File In Plugins Folder (Unspecified): " + file);
            else
                foreach (string file in Directory.GetFiles(pluginsFolder.FullName, specifiedFileExtension, SearchOption.AllDirectories))
                    DebugHelper.Log("Found File In Plugins Folder (Specified): " + file);

            foreach (string file in Directory.GetFiles(pluginsFolder.FullName, specifiedFileExtension, SearchOption.AllDirectories))
                LoadBundle(file);
        }

        public static void LoadBundle(string bundleFile)
        {
            AssetBundle newBundle;
            List<SelectableLevel> selectableLevels = new List<SelectableLevel>();

            SelectableLevel selectableLevel = null;
            TerminalNode levelRoute = null;
            TerminalNode levelConfirmRoute = null;
            TerminalNode levelInfo = null;
            TerminalKeyword levelKeyword = null;
            GameObject levelPrefab = null;

            newBundle = AssetBundle.LoadFromFile(bundleFile);

            DebugHelper.Log("Trying To Load AssetBundle, Loaded Bundle Is: " + newBundle);

            if (newBundle != null)
            {




                string debugString = "AssetBundle Found, Logging Files Inside Below: " + "\n";
                string sourceName = newBundle.name;

                foreach (string name in newBundle.GetAllAssetNames())
                    debugString += name + "\n";

                DebugHelper.Log(debugString);

                selectableLevels = new List<SelectableLevel>(newBundle.LoadAllAssets<SelectableLevel>());

                foreach (SelectableLevel newSelectableLevel in selectableLevels)
                {
                    DebugHelper.Log("Found SelectableLevel - " + newSelectableLevel.PlanetName);
                    if (selectableLevel == null)
                        selectableLevel = newSelectableLevel;
                }

                if (selectableLevel != null)
                {

                    foreach (TerminalNode newTerminalNode in newBundle.LoadAllAssets<TerminalNode>())
                    {
                        if (newTerminalNode.name.Contains("route"))
                        {
                            if (newTerminalNode.name.Contains("Confirm"))
                                levelConfirmRoute = newTerminalNode;
                            else
                                levelRoute = newTerminalNode;
                        }
                        else if (newTerminalNode.name.Contains("Info"))
                            levelInfo = newTerminalNode;
                    }
                    DebugHelper.Log("Route For " + selectableLevel.PlanetName + " Is " + levelRoute);
                    DebugHelper.Log("Info For " + selectableLevel.PlanetName + " Is " + levelInfo);

                    foreach (TerminalKeyword newTerminalKeyword in newBundle.LoadAllAssets<TerminalKeyword>())
                    {
                        levelKeyword = newTerminalKeyword;
                    }

                    DebugHelper.Log("Keyword For " + selectableLevel.PlanetName + " Is " + levelKeyword);

                    if (selectableLevel != null)
                        levelPrefab = selectableLevel.planetPrefab;

                    DebugHelper.Log("Prefab For " + selectableLevel.PlanetName + " Is " + levelPrefab);


                    if (selectableLevel != null && levelRoute != null && levelInfo != null && levelKeyword != null && levelPrefab != null)
                    {
                        DebugHelper.Log("Adding " + selectableLevel.PlanetName);
                        CustomLevelData newCustomLevelData = new CustomLevelData(levelPrefab, levelRoute, levelConfirmRoute, levelInfo, levelKeyword);
                        ExtendedLevel extendedLevel = new ExtendedLevel(selectableLevel, newCustomLevelData, sourceName);
                    }
                }
            }
        }

        //private static void RoundManager_Start(On.RoundManager.orig_Start orig, RoundManager self)
        //{
            //FindBundles();
        //}

        public static void RestoreVanillaAssetReferences(SelectableLevel selectableLevel)
        {
            DebugHelper.Log("Found " + selectableLevel.spawnableScrap.Count + " Item References In Custom Moon: " + selectableLevel.PlanetName);
            int debugCounter = 0;

            ////////// Items (Inside / Dungen) //////////

            foreach (SpawnableItemWithRarity spawnableItem in selectableLevel.spawnableScrap)
            {
                foreach (Item vanillaItem in ContentExtractor.vanillaItemsList)
                {
                    if (spawnableItem.spawnableItem.itemName == vanillaItem.itemName)
                    {
                        spawnableItem.spawnableItem = vanillaItem;
                        debugCounter++;
                    }
                }
            }

            DebugHelper.Log("Found " + debugCounter + " Matches To Vanilla Item References In Custom Moon: " + selectableLevel.PlanetName + ". Restoring!");

            ////////// Enemies (Inside / DunGen) //////////

            DebugHelper.Log("Found " + selectableLevel.Enemies.Count + " Enemy References In Custom Moon: " + selectableLevel.PlanetName);
            debugCounter = 0;

            foreach (SpawnableEnemyWithRarity spawnableEnemy in selectableLevel.Enemies)
            {
                foreach (EnemyType vanillaEnemy in ContentExtractor.vanillaEnemiesList)
                {
                    if (spawnableEnemy.enemyType.enemyName == vanillaEnemy.enemyName)
                    {
                        spawnableEnemy.enemyType = vanillaEnemy;
                        debugCounter++;
                    }
                }
            }

            DebugHelper.Log("Found " + debugCounter + " Matches To Vanilla Enemy References In Custom Moon: " + selectableLevel.PlanetName + ". Restoring!");

            ////////// Enemies (Outside / Moon) (Nighttime) //////////

            DebugHelper.Log("Found " + selectableLevel.OutsideEnemies.Count + " Outside Enemies In Custom Moon: " + selectableLevel.PlanetName);
            debugCounter = 0;

            foreach (SpawnableEnemyWithRarity enemyType in selectableLevel.OutsideEnemies)
                foreach (EnemyType vanillaEnemyType in ContentExtractor.vanillaEnemiesList)
                {
                    //DebugHelper.Log("Custom: " + enemyType.enemyType.enemyName + " | " + "Vanilla: " + vanillaEnemyType + "\n");
                    if (enemyType.enemyType.enemyName == vanillaEnemyType.enemyName)
                    {
                        enemyType.enemyType = vanillaEnemyType;
                        debugCounter++;
                    }
                }

            DebugHelper.Log("Found " + debugCounter + " Matches To Vanilla Outside Enemies In Custom Moon: " + selectableLevel.PlanetName + ". Restoring!");

            ////////// Enemies (Outside / Moon) (Daytime) //////////

            DebugHelper.Log("Found " + selectableLevel.DaytimeEnemies.Count + " Daytime Enemies In Custom Moon: " + selectableLevel.PlanetName);
            debugCounter = 0;

            foreach (SpawnableEnemyWithRarity enemyType in selectableLevel.DaytimeEnemies)
                foreach (EnemyType vanillaEnemyType in ContentExtractor.vanillaEnemiesList)
                {
                    //DebugHelper.Log("Custom: " + enemyType.enemyType.enemyName + " | " + "Vanilla: " + vanillaEnemyType + "\n");
                    if (enemyType.enemyType.enemyName == vanillaEnemyType.enemyName)
                    {
                        enemyType.enemyType = vanillaEnemyType;
                        debugCounter++;
                    }
                }

            DebugHelper.Log("Found " + debugCounter + " Matches To Vanilla Daytime Enemies In Custom Moon: " + selectableLevel.PlanetName + ". Restoring!");

            ////////// Spawnable Inside Objects (DunGen) //////////

            DebugHelper.Log("Found " + selectableLevel.spawnableMapObjects.Length + " Inside Map Objects In Custom Moon: " + selectableLevel.PlanetName);
            debugCounter = 0;

            foreach (SpawnableMapObject spawnableMapObject in selectableLevel.spawnableMapObjects)
                foreach (GameObject vanillaSpawnableMapObject in ContentExtractor.vanillaSpawnableInsideMapObjectsList)
                    if (spawnableMapObject.prefabToSpawn.name == vanillaSpawnableMapObject.name)
                    {
                        spawnableMapObject.prefabToSpawn = vanillaSpawnableMapObject;
                        debugCounter++;
                    }

            DebugHelper.Log("Found " + debugCounter + " Matches To Vanilla Inside Map Objects In Custom Moon: " + selectableLevel.PlanetName + ". Restoring!");

            ////////// Spawnable Outside Objects (Moon) //////////

            DebugHelper.Log("Found " + selectableLevel.spawnableOutsideObjects.Length + " Outside Map Objects In Custom Moon: " + selectableLevel.PlanetName);
            debugCounter = 0;

            foreach (SpawnableOutsideObjectWithRarity spawnableOutsideObject in selectableLevel.spawnableOutsideObjects)
                foreach (SpawnableOutsideObject vanillaSpawnableOutsideObject in ContentExtractor.vanillaSpawnableOutsideMapObjectsList)
                    if (spawnableOutsideObject.spawnableObject.name == vanillaSpawnableOutsideObject.name)
                    {
                        spawnableOutsideObject.spawnableObject = vanillaSpawnableOutsideObject;
                        debugCounter++;
                    }

            DebugHelper.Log("Found " + debugCounter + " Matches To Vanilla Outside Map Objects In Custom Moon: " + selectableLevel.PlanetName + ". Restoring!");

            ////////// Level Ambience Libraries //////////

            bool restoredLevelAmbienceLibrary = false;
            foreach (LevelAmbienceLibrary vanillaAmbienceLibrary in ContentExtractor.vanillaAmbienceLibrariesList)
                if (vanillaAmbienceLibrary.name == selectableLevel.levelAmbienceClips.name)
                {
                    selectableLevel.levelAmbienceClips = vanillaAmbienceLibrary;
                    restoredLevelAmbienceLibrary = true;
                }

            if (restoredLevelAmbienceLibrary == true)
                DebugHelper.Log("Succesfully Found Matching Vanilla Level Ambience Library In Custom Moon: " + selectableLevel.PlanetName + ". Restoring!");

        }
    }
}

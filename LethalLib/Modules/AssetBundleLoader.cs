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
            On.RoundManager.Start += RoundManager_Start;
            On.StartOfRound.Start += StartOfRound_Start;
        }


        public static void FindBundles()
        {
            lethalLibFolder = lethalLibFile.Parent;
            pluginsFolder = lethalLibFile.Parent.Parent;
            specifiedFileExtension = "*.lem";

            Debug.Log("LethalLib (Batby): LethaLib Folder Location: " + lethalLibFolder);
            Debug.Log("LethalLib (Batby): Plugins Folder Location: " + pluginsFolder);

            if (specifiedFileExtension == string.Empty)
                foreach (string file in Directory.GetFiles(pluginsFolder.FullName))
                    Debug.Log("LethalLib (Batby): Found File In Plugins Folder (Unspecified): " + file);
            else
                foreach (string file in Directory.GetFiles(pluginsFolder.FullName, specifiedFileExtension, SearchOption.AllDirectories))
                    Debug.Log("LethalLib (Batby): Found File In Plugins Folder (Specified): " + file);

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

            Debug.Log("LethalLib (Batby): Trying To Load AssetBundle, Loaded Bundle Is: " + newBundle);

            if (newBundle != null)
            {
                string debugString = "LethalLib (Batby): AssetBundle Found, Logging Files Inside Below: " + "\n";
                string sourceName = newBundle.name;

                foreach (string name in newBundle.GetAllAssetNames())
                    debugString += name + "\n";

                Debug.Log(debugString);

                selectableLevels = new List<SelectableLevel>(newBundle.LoadAllAssets<SelectableLevel>());

                foreach (SelectableLevel newSelectableLevel in selectableLevels)
                {
                    Debug.Log("LethalLib (Batby): Found SelectableLevel - " + newSelectableLevel.PlanetName);
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
                    Debug.Log("LethalLib (Batby): Route For " + selectableLevel.PlanetName + " Is " + levelRoute);
                    Debug.Log("LethalLib (Batby): Info For " + selectableLevel.PlanetName + " Is " + levelInfo);

                    foreach (TerminalKeyword newTerminalKeyword in newBundle.LoadAllAssets<TerminalKeyword>())
                    {
                        levelKeyword = newTerminalKeyword;
                    }

                    Debug.Log("LethalLib (Batby): Keyword For " + selectableLevel.PlanetName + " Is " + levelKeyword);

                    if (selectableLevel != null)
                        levelPrefab = selectableLevel.planetPrefab;

                    Debug.Log("LethalLib (Batby): Prefab For " + selectableLevel.PlanetName + " Is " + levelPrefab);


                    if (selectableLevel != null && levelRoute != null && levelInfo != null && levelKeyword != null && levelPrefab != null)
                    {
                        Debug.Log("LethalLib (Batby): Adding " + selectableLevel.PlanetName);
                        CustomLevelData newCustomLevelData = new CustomLevelData(levelPrefab, levelRoute, levelConfirmRoute, levelInfo, levelKeyword);
                        ExtendedLevel extendedLevel = new ExtendedLevel(selectableLevel, sourceName, newCustomLevelData);
                    }
                }
            }
        }

        private static void RoundManager_Start(On.RoundManager.orig_Start orig, RoundManager self)
        {
            FindBundles();
        }

        private static void StartOfRound_Start(On.StartOfRound.orig_Start orig, StartOfRound self)
        {
            //FindBundles();
        }
    }
}

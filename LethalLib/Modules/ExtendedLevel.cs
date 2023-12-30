using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LethalLib.Modules
{
    public class CustomLevelData //This could be better as a struct, Not sure let me know.
    {
        public GameObject levelPrefab;
        public TerminalNode levelRouteNode;
        public TerminalNode levelRouteConfirmNode;
        public TerminalNode levelInfoNode;
        public TerminalKeyword levelKeyword;

        private static List<string> ObjectsToDestroy = new List<string> {
                "CompletedVowTerrain",
                "tree",
                "Tree",
                "Rock",
                "StaticLightingSky",
                "ForestAmbience",
                "Local Volumetric Fog",
                "GroundFog",
                "Sky and Fog Global Volume",
                "SunTexture"
            };

        public CustomLevelData(GameObject newLevelPrefab, TerminalNode newRouteNode, TerminalNode newRouteConfirmNode, TerminalNode newInfoNode, TerminalKeyword newTerminalKeyword)
        {
            levelPrefab = newLevelPrefab;
            levelRouteNode = newRouteNode;
            levelRouteConfirmNode = newRouteConfirmNode;
            levelInfoNode = newInfoNode;
            levelKeyword = newTerminalKeyword;
            levelKeyword.defaultVerb = TerminalUtils.RouteKeyword;

    
            //TerminalUtils.AddRouteNode(levelRouteNode);
        }

        public static void AddObjectToDestroyList(string NewObjectName)
        {
            ObjectsToDestroy.Add(NewObjectName);
        }

        public static void ClearObjectToDestroyList()
        {
            ObjectsToDestroy.Clear();
        }

        public List<string> GetDestroyList() { return ObjectsToDestroy; }
    }

    public class ExtendedLevel
    {
        public SelectableLevel SelectableLevel;

        public string sourceName = "Lethal Company"; //Levels from AssetBundles will have this as their Assembly Name.
        public string NumberlessPlanetName
        {
            get
            {
                if (SelectableLevel != null)
                    return new string(SelectableLevel.PlanetName.SkipWhile(c => !char.IsLetter(c)).ToArray());
                else
                    return null;
            }
        } //ChatGPT did this one i'll be honest.

        public enum LevelType { Vanilla, Custom, Any } //Any included for built in checks.
        public LevelType levelType;

        //Type & Built-In NullCheck.
        private CustomLevelData customLevelData;
        public CustomLevelData CustomLevel
        {
            get
            {
                if (levelType == LevelType.Custom && customLevelData != null)
                    return (customLevelData);
                else
                    return (null);
            }
        }

        public ExtendedLevel(SelectableLevel newSelectableLevel, string newSourceName = default, CustomLevelData newCustomLevelData = null)
        {
            if (newCustomLevelData == null)
                levelType = LevelType.Vanilla;
            else
            {
                levelType = LevelType.Custom;

                if (newSourceName != string.Empty)
                    sourceName = newSourceName;

                newSelectableLevel.levelID = 9 + Levels.customLevelsList.Count; //Hardcoded Vanilla level length + how many custom moons are already loaded. If I can refine order of execution we can remove the hardcoded value here
                newSelectableLevel.sceneName = "InitSceneLaunchOptions"; //This is the scene we inject our custom moon into.

                customLevelData = newCustomLevelData;
                customLevelData.levelRouteNode.displayPlanetInfo = newSelectableLevel.levelID;
                customLevelData.levelRouteConfirmNode.buyRerouteToMoon = newSelectableLevel.levelID;

                TerminalUtils.AddTerminalKeyword(customLevelData.levelKeyword);
                TerminalUtils.AddRouteNode(customLevelData.levelKeyword, customLevelData.levelRouteNode);


                ///These Will Not Stay
                ///
            }

            SelectableLevel = newSelectableLevel;


            Debug.Log("LethalLib (Batby): New CustomLevel Created Is " + SelectableLevel.PlanetName + " (" + SelectableLevel.levelID + ") " + " , Adding To List!");

            Levels.AddSelectableLevel(this);
        }

        public void CreateCustomLevelData(GameObject newLevelPrefab, TerminalNode newRouteNode, TerminalNode newRouteInfoNode, TerminalNode newInfoNode, TerminalKeyword newTerminalKeyword)
        {
            customLevelData = new CustomLevelData(newLevelPrefab, newRouteNode, newRouteInfoNode, newInfoNode, newTerminalKeyword);
        }
    }
}
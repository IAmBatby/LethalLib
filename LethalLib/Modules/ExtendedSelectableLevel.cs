using HarmonyLib;
using System;
using System.Collections.Generic;
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

    public class ExtendedSelectableLevel
    {
        public SelectableLevel SelectableLevel;

        //public static string LevelSource = "Lethal Company"; //Levels from AssetBundles will have this as their Assembly Name.

        public enum LevelType { Vanilla, Custom }
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

        public ExtendedSelectableLevel(SelectableLevel newSelectableLevel, CustomLevelData newCustomLevelData = null)
        {
            if (newCustomLevelData == null)
                levelType = LevelType.Vanilla;
            else
            {
                levelType = LevelType.Custom;

                newSelectableLevel.levelID = 9 + Levels.customLevelsList.Count;
                //newSelectableLevel.levelID = 2;
                newSelectableLevel.sceneName = "InitSceneLaunchOptions";

                customLevelData = newCustomLevelData;
                //customLevelData.levelRouteNode.buyRerouteToMoon = newSelectableLevel.levelID;
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
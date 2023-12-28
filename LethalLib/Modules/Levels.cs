using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalLib.Modules
{
    public class Levels {
        [Flags]
        public enum LevelTypes {
            None = 1 << 0,
            ExperimentationLevel = 1 << 1,
            AssuranceLevel = 1 << 2,
            VowLevel = 1 << 3,
            OffenseLevel = 1 << 4,
            MarchLevel = 1 << 5,
            RendLevel = 1 << 6,
            DineLevel = 1 << 7,
            TitanLevel = 1 << 8,
            OoblterraLevel = 1 << 9,
            All = ExperimentationLevel | AssuranceLevel | VowLevel | OffenseLevel | MarchLevel | RendLevel | DineLevel | TitanLevel | OoblterraLevel
        }

        /* This class is called levels so I'm putting all the custom level code here.
         * If I need to move it to a seperate class let me know -Skull
         */

        public static Dictionary<string, CustomLevel> CustomLevelList;

        public class CustomLevel {
            public TerminalKeyword LevelKeyword;
            public TerminalNode TerminalRoute;
            public SelectableLevel NewLevel;
            public TerminalNode LevelTerminalInfo;         
            public GameObject LevelPrefab;
            public static int MoonID = 9;
            public string MoonFriendlyName;

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

            public static void AddObjectToDestroyList(string NewObjectName) {
                ObjectsToDestroy.Add(NewObjectName);
            }
            public static void ClearObjectToDestroyList() {
                ObjectsToDestroy.Clear();
            }

            public List<string> GetDestroyList() { return ObjectsToDestroy; }

            public CustomLevel(SelectableLevel newSelectableLevel, TerminalKeyword newTerminalAsset,
                    TerminalNode NewRoute, TerminalNode newTerminalInfo, GameObject newLevelPrefab) {
                MoonID = MoonID++;
                NewLevel = newSelectableLevel;
                NewLevel.levelID = MoonID;
                LevelKeyword = newTerminalAsset;
                TerminalRoute = NewRoute;
                NewRoute.buyRerouteToMoon = MoonID;
                NewRoute.terminalOptions[1].result.buyRerouteToMoon = MoonID;
                LevelTerminalInfo = newTerminalInfo;
                LevelPrefab = newLevelPrefab;
                MoonFriendlyName = NewLevel.PlanetName;
                CustomLevelList.AddItem(new KeyValuePair<string, CustomLevel>(MoonFriendlyName, this));
                
            }
        }
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        [HarmonyPriority(0)]
        public static void AddMoonsToMoonList(StartOfRound __instance) {
            
            //Resize the levels array to include all of our new ones
            SelectableLevel[] newLevelArray = new SelectableLevel[__instance.levels.Length + CustomLevelList.Count];
            __instance.levels.CopyTo(newLevelArray, 0);
            __instance.levels = newLevelArray;

            foreach (CustomLevel moon in CustomLevelList.Values) {
                SelectableLevel MyNewMoon = moon.NewLevel;
                /* TODO: these assignments should only be made if there's no entry in any of these arrays already.
                 * Hopefully, the end user would be able to put their custom monsters/scrap/whatever into the 
                 * SelectableLevel class they made in unity or set it before this point or something.
                 * I want to make these default values, but the problem with that is we can't retrieve the instance without it existing.
                 */
                MyNewMoon.planetPrefab = __instance.levels[2].planetPrefab;
                MyNewMoon.spawnableMapObjects = __instance.levels[2].spawnableMapObjects;
                MyNewMoon.spawnableOutsideObjects = __instance.levels[2].spawnableOutsideObjects;
                MyNewMoon.spawnableScrap = __instance.levels[2].spawnableScrap;
                MyNewMoon.Enemies = __instance.levels[5].Enemies;
                MyNewMoon.levelAmbienceClips = __instance.levels[2].levelAmbienceClips;
                MyNewMoon.OutsideEnemies = __instance.levels[0].OutsideEnemies;
                MyNewMoon.DaytimeEnemies = __instance.levels[0].DaytimeEnemies;
                
                int num = -1;
                for (int i = 0; i < __instance.levels.Length; i++) {
                    if (__instance.levels[i] == null) {
                        num = i;
                        break;
                    }
                }
                if (num == -1) {
                    throw new NullReferenceException("No slot in level list to put new level in!");
                }
                __instance.levels[num] = MyNewMoon;
            }
            TerminalUtils.AddMoonsToCatalogue();
        }

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix]
        public static void CreateMoonOnNav(StartOfRound __instance) {
            if (!CustomLevelList.ContainsKey(__instance.currentLevel.PlanetName)) {
                return;
            }
            CustomLevel LevelToLoad = CustomLevelList[__instance.currentLevel.PlanetName];
            Debug.Log(" LethalLib Moon Tools: Loading into level " + __instance.currentLevel.PlanetName);

            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject ObjToDestroy in allObjects) {
                if (ObjToDestroy.name.Contains("Models2VowFactory")) {
                    ObjToDestroy.SetActive(false);
                }

                //If the object's named Plane and its parent is Foliage, it's also gotta go. This gets rid of the grass
                if (ObjToDestroy.name.Contains("Plane") && (ObjToDestroy.transform.parent.gameObject.name.Contains("Foliage") || ObjToDestroy.transform.parent.gameObject.name.Contains("Mounds"))) {
                    GameObject.Destroy(ObjToDestroy);
                }
                foreach (string UnwantedObjString in LevelToLoad.GetDestroyList()) {
                    //If the object has any of the names in the list, it's gotta go
                    if (ObjToDestroy.name.Contains(UnwantedObjString)) {
                        GameObject.Destroy(ObjToDestroy);
                        continue;
                    }
                }
            }
            //Load our custom prefab
            GameObject MyLevelAsset = LevelToLoad.LevelPrefab as GameObject;
            GameObject.Instantiate(MyLevelAsset);

        }
    }
}

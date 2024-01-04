using DunGen.Graph;
using HarmonyLib;
using LethalLevelLoader.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;

namespace LethalLevelLoader.Extras
{
    public static class DebugHelper
    {
        public static string logAuthor = "Batby";

        public static void Log(string log)
        {
            string logString = "LethalLib (" + logAuthor + "): ";
            logString += log;
            Debug.Log(logString);
        }

        public static void DebugTerminalKeyword(TerminalKeyword terminalKeyword)
        {
            string logString = "Info For " + terminalKeyword.word + ") TerminalKeyword!" + "\n" + "\n";
            logString += "Word: " + terminalKeyword.word + "\n";
            logString += "isVerb?: " + terminalKeyword.isVerb + "\n";
            logString += "CompatibleNouns :" + "\n";

            foreach (CompatibleNoun compatibleNoun in terminalKeyword.compatibleNouns)
                logString += compatibleNoun.noun + " | " + compatibleNoun.result + "\n";

            logString += "SpecialKeywordResult: " + terminalKeyword.specialKeywordResult + "\n";
            logString += "AccessTerminalObjects?: " + terminalKeyword.accessTerminalObjects + "\n";
            logString += "DefaultVerb: " + terminalKeyword.defaultVerb.name + "\n";

            Log(logString + "\n" + "\n");
        }

        public static void DebugTerminalNode(TerminalNode terminalNode)
        {
            string logString = "Info For " + terminalNode.name + ") TerminalNode!" + "\n" + "\n";
            logString += "Display Text: " + terminalNode.displayText + "\n";
            logString += "Accept Anything?: " + terminalNode.acceptAnything + "\n";
            logString += "Override Options?: " + terminalNode.overrideOptions + "\n";
            logString += "Display Planet Info (LevelID): " + terminalNode.displayPlanetInfo + "\n";
            logString += "Buy Reroute To Moon (LevelID): " + terminalNode.buyRerouteToMoon + "\n";
            logString += "Is Confirmation Node?: " + terminalNode.isConfirmationNode + "\n";
            logString += "Terminal Options (CompatibleNouns) :" + "\n";

            foreach (CompatibleNoun compatibleNoun in terminalNode.terminalOptions)
                logString += compatibleNoun.noun + " | " + compatibleNoun.result + "\n";

            Log(logString + "\n" + "\n");
        }

        public static void DebugInjectedLevels()
        {
            string logString = "Injected Levels List: " + "\n" + "\n";

            int counter = 0;
            if (StartOfRound.Instance != null)
            {
                foreach (SelectableLevel level in StartOfRound.Instance.levels)
                {
                    logString += counter + ". " + level.PlanetName + " (" + level.levelID + ") " + "\n";
                    counter++;
                }

                logString += "Current Level Is: " + StartOfRound.Instance.currentLevel.PlanetName + " (" + StartOfRound.Instance.currentLevel.levelID + ") " + "\n";
            }

            Log(logString + "\n" + "\n");
        }



        public static void DebugAllLevels()
        {
            string logString = "All Levels List: " + "\n" + "\n";

            foreach (ExtendedLevel extendedLevel in Levels.allLevelsList)
                logString += extendedLevel.selectableLevel.PlanetName + " (" + extendedLevel.selectableLevel.levelID + ") " + "\n";

            Log(logString + "\n");
        }

        public static void DebugVanillaLevels()
        {
            string logString = "Vanilla Levels List: " + "\n" + "\n";

            foreach (ExtendedLevel extendedLevel in Levels.vanillaLevelsList)
                logString += extendedLevel.selectableLevel.PlanetName + " (" + extendedLevel.selectableLevel.levelID + ") " + "\n";

            Log(logString + "\n");
        }

        public static void DebugCustomLevels()
        {
            string logString = "Custom Levels List: " + "\n" + "\n";

            foreach (ExtendedLevel extendedLevel in Levels.customLevelsList)
                logString += extendedLevel.selectableLevel.PlanetName + " (" + extendedLevel.selectableLevel.levelID + ") " + "\n";

            Log(logString + "\n");
        }

        public static void DebugScrapedVanillaContent()
        {
            Log("Obtained (" + ContentExtractor.vanillaItemsList.Count + " / 68) Vanilla Item References");

            Log("Obtained (" + ContentExtractor.vanillaEnemiesList.Count + " / 20) Vanilla Enemy References");

            Log("Obtained (" + ContentExtractor.vanillaSpawnableOutsideMapObjectsList.Count + " / 11) Vanilla Outside Object References");

            Log("Obtained (" + ContentExtractor.vanillaSpawnableInsideMapObjectsList.Count + " / 2) Vanilla Inside Object References");


            Log("Obtained (" + ContentExtractor.vanillaAmbienceLibrariesList.Count + " / 3) Vanilla Ambience Library References");

            Log("Obtained (" + ContentExtractor.vanillaAudioMixerGroupsList.Count + " / 00) Vanilla Audio Mixing Group References");

            foreach (AudioMixerGroup audioMix in ContentExtractor.vanillaAudioMixerGroupsList)
                Log("AudioMixerGroup Name: " + audioMix.name);
        }

        public static void DebugAudioMixerGroups()
        {

        }


        public static void DebugSelectableLevelReferences(ExtendedLevel extendedLevel)
        {
            string logString = "Logging SelectableLevel References For Moon: " + extendedLevel.NumberlessPlanetName + " (" + extendedLevel.levelType.ToString() + ")." + "\n";

            logString += "Inside Enemies" + "\n" + "\n";

            foreach (SpawnableEnemyWithRarity spawnableEnemy in extendedLevel.selectableLevel.Enemies)
                logString += "Enemy Type: " + spawnableEnemy.enemyType.enemyName + " , Rarity: " + spawnableEnemy.rarity + " , Prefab Status: " + (spawnableEnemy.enemyType.enemyPrefab != null) + "\n";

            logString += "Outside Enemies (Nighttime)" + "\n" + "\n";

            foreach (SpawnableEnemyWithRarity spawnableEnemy in extendedLevel.selectableLevel.OutsideEnemies)
                logString += "Enemy Type: " + spawnableEnemy.enemyType.enemyName + " , Rarity: " + spawnableEnemy.rarity + " , Prefab Status: " + (spawnableEnemy.enemyType.enemyPrefab != null) + "\n";

            logString += "Outside Enemies (daytime)" + "\n" + "\n";

            foreach (SpawnableEnemyWithRarity spawnableEnemy in extendedLevel.selectableLevel.DaytimeEnemies)
                logString += "Enemy Type: " + spawnableEnemy.enemyType.enemyName + " , Rarity: " + spawnableEnemy.rarity + " , Prefab Status: " + (spawnableEnemy.enemyType.enemyPrefab != null) + "\n";


            Log(logString + "\n");
        }

        public static void DebugDungeonFlows(List<DungeonFlow> dungeonFlowList)
        {
            string debugString = "Dungen Flow Report: " + "\n" + "\n";

            foreach (DungeonFlow dungeonFlow in dungeonFlowList)
                debugString += dungeonFlow.name + "\n";
        }

        public static string GetDungeonFlowsLog(List<DungeonFlow> dungeonFlowList)
        {
            string returnString = string.Empty;

            foreach (DungeonFlow dungeonFlow in dungeonFlowList)
                returnString += dungeonFlow.name + "\n";

            return (returnString);
        }

        public static void DebugAllExtendedDungeons()
        {
            string debugString = "All ExtendedDungeons: " + "\n" + "\n";

            foreach (ExtendedDungeonFlow dungeonFlow in Dungeon.allExtendedDungeonsList)
                debugString += dungeonFlow.dungeonFlow.name;

            Log(debugString);

            debugString = "Vanilla ExtendedDungeons: " + "\n" + "\n";

            foreach (ExtendedDungeonFlow dungeonFlow in Dungeon.vanillaDungeonFlowsList)
                debugString += dungeonFlow.dungeonFlow.name;

            Log(debugString);

            debugString = "Custom ExtendedDungeons: " + "\n" + "\n";

            foreach (ExtendedDungeonFlow dungeonFlow in Dungeon.customDungeonFlowsList)
                debugString += dungeonFlow.dungeonFlow.name;

            Log(debugString);
        }
    }

}

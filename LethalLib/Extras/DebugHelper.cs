using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalLib.Extras
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

            foreach (ExtendedSelectableLevel extendedLevel in Levels.allLevelsList)
                logString += extendedLevel.SelectableLevel.PlanetName + " (" + extendedLevel.SelectableLevel.levelID + ") " + "\n";

            Log(logString + "\n");
        }

        public static void DebugVanillaLevels()
        {
            string logString = "Vanilla Levels List: " + "\n" + "\n";

            foreach (ExtendedSelectableLevel extendedLevel in Levels.vanillaLevelsList)
                logString += extendedLevel.SelectableLevel.PlanetName + " (" + extendedLevel.SelectableLevel.levelID + ") " + "\n";

            Log(logString + "\n");
        }

        public static void DebugCustomLevels()
        {
            string logString = "Custom Levels List: " + "\n" + "\n";

            foreach (ExtendedSelectableLevel extendedLevel in Levels.customLevelsList)
                logString += extendedLevel.SelectableLevel.PlanetName + " (" + extendedLevel.SelectableLevel.levelID + ") " + "\n";

            Log(logString + "\n");
        }
    }

}

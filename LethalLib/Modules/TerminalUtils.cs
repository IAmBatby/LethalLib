using HarmonyLib;
using LethalLib.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static LethalLib.Modules.Levels;

namespace LethalLib.Modules
{
    public class TerminalUtils
    {
        private static Terminal _terminal;
        public static Terminal Terminal
        {
            get
            {
                if (_terminal != null)
                    return (_terminal);
                else
                {
                    _terminal = GameObject.Find("TerminalScript").GetComponent<Terminal>();
                    if (_terminal != null)
                        return (_terminal);
                    else
                    {
                        Debug.LogError("LethaLib: Failed To Grab Terminal Reference!");
                        return (null);
                    }
                }

            }
        }

        //Hardcoded References To Important Base-Game TerminalKeywords;
        public static TerminalKeyword RouteKeyword => Terminal.terminalNodes.allKeywords[26];
        public static TerminalKeyword InfoKeyword => Terminal.terminalNodes.allKeywords[6];

        public static TerminalKeyword CreateTerminalKeyword(string word, bool isVerb = false, CompatibleNoun[] compatibleNouns = null, TerminalNode specialKeywordResult = null, TerminalKeyword defaultVerb = null, bool accessTerminalObjects = false)
        {

            TerminalKeyword keyword = ScriptableObject.CreateInstance<TerminalKeyword>();
            keyword.name = word;
            keyword.word = word;
            keyword.isVerb = isVerb;
            keyword.compatibleNouns = compatibleNouns;
            keyword.specialKeywordResult = specialKeywordResult;
            keyword.defaultVerb = defaultVerb;
            keyword.accessTerminalObjects = accessTerminalObjects;
            return keyword;
        }

        public static void AddTerminalKeyword(TerminalKeyword newTerminalKeyword)
        {
            Terminal.terminalNodes.allKeywords = Terminal.terminalNodes.allKeywords.AddItem(newTerminalKeyword).ToArray();
        }

        public static void AddRouteNode(TerminalKeyword terminalKeyword, TerminalNode newTerminalNode)
        {
            CompatibleNoun newNoun = new CompatibleNoun();
            newNoun.noun = terminalKeyword;
            newNoun.result = newTerminalNode;

            RouteKeyword.compatibleNouns = RouteKeyword.compatibleNouns.AddItem(newNoun).ToArray();
        }

        public static void AddRouteInfo(TerminalKeyword terminalKeyword, TerminalNode newTerminalNode)
        {
            //TODO
        }

        [HarmonyPatch(typeof(Terminal), "TextPostProcess")]
        [HarmonyPrefix]
        public static void TextPostProcess_PreFix(ref string modifiedDisplayText)
        {
            if (modifiedDisplayText.Contains("Welcome to the exomoons catalogue"))
            {
                modifiedDisplayText = "Welcome to the exomoons catalogue.\r\nTo route the autopilot to a moon, use the word ROUTE.\r\nTo learn about any moon, use the word INFO.\r\n____________________________\r\n\r\n* The Company building   //   Buying at [companyBuyingPercent].\r\n\r\n";

                List<ExtendedLevel> tweakedVanillaLevelsList = new List<ExtendedLevel>(vanillaLevelsList);

                tweakedVanillaLevelsList.RemoveAt(3);
                tweakedVanillaLevelsList.Insert(3, tweakedVanillaLevelsList[6]);
                tweakedVanillaLevelsList.RemoveAt(7);
                tweakedVanillaLevelsList.Insert(5, null);

                modifiedDisplayText += GetMoonCatalogDisplayListings(tweakedVanillaLevelsList);
                modifiedDisplayText += GetMoonCatalogDisplayListings(customLevelsList);
                modifiedDisplayText += "\r\n";
            }
        }

        public static string GetMoonCatalogDisplayListings(List<ExtendedLevel> extendedLevels)
        {
            string returnString = string.Empty;

            int seperationCountMax = 3;
            int seperationCount = 0;

            foreach (ExtendedLevel extendedLevel in extendedLevels)
            {
                if (extendedLevel != null)
                    returnString += "* " + extendedLevel.NumberlessPlanetName + " " + GetMoonConditions(extendedLevel.SelectableLevel) + "\n";

                seperationCount++;
                if (seperationCount == seperationCountMax)
                {
                    returnString += "\n";
                    seperationCount = 0;
                }
            }

            return (returnString);
        }

        public static string GetMoonConditions(SelectableLevel selectableLevel)
        {
            string returnString = string.Empty;

            if (selectableLevel != null)
                if (selectableLevel.currentWeather != LevelWeatherType.None)
                    returnString = "(" + selectableLevel.currentWeather.ToString() + ")";

            return (returnString);
        }
    }
}
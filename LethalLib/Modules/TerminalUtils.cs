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
        public static TerminalKeyword RouteKeyword => GetTerminalKeywordFromIndex(26);
        public static TerminalKeyword InfoKeyword => GetTerminalKeywordFromIndex(6);
        public static TerminalKeyword ConfirmKeyword => GetTerminalKeywordFromIndex(3);
        public static TerminalKeyword DenyKeyword => GetTerminalKeywordFromIndex(4);
        //This isn't anywhere easy to grab so we grab it from Vow's Route.
        public static TerminalNode CancelRouteNode
        {
            get
            {
                if (RouteKeyword != null)
                {
                    //DebugHelper.Log(RouteKeyword.compatibleNouns[0].result.ToString());
                    //DebugHelper.Log(RouteKeyword.compatibleNouns[0].result.terminalOptions.Length.ToString());
                    return (RouteKeyword.compatibleNouns[0].result.terminalOptions[0].result);
                }
                else
                    return (null);
            }
        }
        
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
            string previousLevelSource = string.Empty;

            int seperationCountMax = 3;
            int seperationCount = 0;

            foreach (ExtendedLevel extendedLevel in extendedLevels)
            {
                if (extendedLevel != null)
                {
                    if (previousLevelSource != string.Empty && previousLevelSource != extendedLevel.sourceName)
                    {
                        returnString += "\n";
                        seperationCount = 0;
                    }

                    returnString += "* " + extendedLevel.NumberlessPlanetName + " " + GetMoonConditions(extendedLevel.selectableLevel) + "\n";
                    previousLevelSource = extendedLevel.sourceName;
                }

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

        public static void CreateLevelTerminalData(ExtendedLevel extendedLevel)
        {
            TerminalKeyword tempRouteKeyword = GetTerminalKeywordFromIndex(26);
            DebugHelper.Log("Temp Route Keyword Is: " + (tempRouteKeyword != null).ToString());


            TerminalKeyword terminalKeyword = ScriptableObject.CreateInstance<TerminalKeyword>();
            TerminalNode terminalNodeRoute = ScriptableObject.CreateInstance<TerminalNode>();
            TerminalNode terminalNodeRouteConfirm = ScriptableObject.CreateInstance<TerminalNode>();
            TerminalNode terminalNodeInfo = ScriptableObject.CreateInstance<TerminalNode>();

            terminalKeyword.compatibleNouns = new CompatibleNoun[0];
            terminalKeyword.name = extendedLevel.NumberlessPlanetName;
            terminalKeyword.word = extendedLevel.NumberlessPlanetName.ToLower();
            terminalKeyword.defaultVerb = tempRouteKeyword;


            terminalNodeRoute.name = extendedLevel.NumberlessPlanetName.ToLower() + "Route";
            terminalNodeRoute.displayText = "The cost to route to " + extendedLevel.selectableLevel.PlanetName + " is [totalCost]. It is currently [currentPlanetTime] on this moon.";
            terminalNodeRoute.displayText += "\n" + "\n" + "Please CONFIRM or DENY." + "\n" + "\n";
            terminalNodeRoute.clearPreviousText = true;
            terminalNodeRoute.maxCharactersToType = 25;
            terminalNodeRoute.buyItemIndex = -1;
            terminalNodeRoute.buyRerouteToMoon = -2;
            terminalNodeRoute.displayPlanetInfo = extendedLevel.selectableLevel.levelID;
            terminalNodeRoute.shipUnlockableID = -1;
            terminalNodeRoute.itemCost = 0;
            terminalNodeRoute.creatureFileID = -1;
            terminalNodeRoute.storyLogFileID = -1;
            terminalNodeRoute.overrideOptions = true;
            terminalNodeRoute.playSyncedClip = -1;

            terminalNodeRouteConfirm.terminalOptions = new CompatibleNoun[0];
            terminalNodeRouteConfirm.name = extendedLevel.NumberlessPlanetName.ToLower() + "RouteConfirm";
            terminalNodeRouteConfirm.displayText = "Routing autopilot to " + extendedLevel.selectableLevel.PlanetName + " Your new balance is [playerCredits].";
            terminalNodeRouteConfirm.clearPreviousText = true;
            terminalNodeRouteConfirm.maxCharactersToType = 25;
            terminalNodeRouteConfirm.buyItemIndex = -1;
            terminalNodeRouteConfirm.buyRerouteToMoon = extendedLevel.selectableLevel.levelID;
            terminalNodeRouteConfirm.displayPlanetInfo = 1;
            terminalNodeRouteConfirm.shipUnlockableID = -1;
            terminalNodeRouteConfirm.itemCost = 0;
            terminalNodeRouteConfirm.creatureFileID = -1;
            terminalNodeRouteConfirm.storyLogFileID = -1;
            terminalNodeRouteConfirm.overrideOptions = true;
            terminalNodeRouteConfirm.playSyncedClip = -1;

            CompatibleNoun routeDeny = new CompatibleNoun();
            CompatibleNoun routeConfirm = new CompatibleNoun();

            routeDeny.noun = DenyKeyword;
            routeDeny.result = CancelRouteNode;

            routeConfirm.noun = ConfirmKeyword;
            routeConfirm.result = terminalNodeRouteConfirm;

            terminalNodeRoute.terminalOptions = terminalNodeRoute.terminalOptions.AddItem(routeDeny).ToArray();
            terminalNodeRoute.terminalOptions = terminalNodeRoute.terminalOptions.AddItem(routeConfirm).ToArray();

            DebugHelper.DebugTerminalNode(extendedLevel.CustomLevel.levelRouteNode);

            //terminalNodeRoute.displayText = extendedLevel.CustomLevel.levelRouteNode.displayText;

            //terminalNodeRoute.terminalOptions = extendedLevel.CustomLevel.levelRouteNode.terminalOptions;

            DebugHelper.DebugTerminalNode(terminalNodeRoute);

            //AddTerminalKeyword(terminalKeyword);
            //AddRouteNode(terminalKeyword, terminalNodeRoute);

            CompatibleNoun routeLevel = new CompatibleNoun();

            routeLevel.noun = terminalKeyword;
            routeLevel.result = terminalNodeRoute;

            Terminal.terminalNodes.allKeywords = Terminal.terminalNodes.allKeywords.AddItem(terminalKeyword).ToArray();
            tempRouteKeyword.compatibleNouns = tempRouteKeyword.compatibleNouns.AddItem(routeLevel).ToArray();

            extendedLevel.CustomLevel.levelKeyword = terminalKeyword;
            extendedLevel.CustomLevel.levelInfoNode = terminalNodeInfo;
            extendedLevel.CustomLevel.levelRouteNode = terminalNodeRoute;
            extendedLevel.CustomLevel.levelRouteConfirmNode = terminalNodeRouteConfirm;
        }

        public static TerminalKeyword GetTerminalKeywordFromIndex(int index)
        {
            if (Terminal != null)
                return (Terminal.terminalNodes.allKeywords[index]);
            else
                return (null);
        }
    }
}
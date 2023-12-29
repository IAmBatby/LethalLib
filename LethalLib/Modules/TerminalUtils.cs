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
        /*     
        public string word;

        public bool isVerb;

        public CompatibleNoun[] compatibleNouns;

        public TerminalNode specialKeywordResult;

        [Space(5f)]
        public TerminalKeyword defaultVerb;

        [Space(3f)]
        public bool accessTerminalObjects;
        */
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

        //Terminal commands for custom moon stuff
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

        public static TerminalKeyword RouteKeyword => Terminal.terminalNodes.allKeywords[26];
        public static TerminalKeyword InfoKeyword => Terminal.terminalNodes.allKeywords[6];

        private static bool isTerminalPatched = false;

        public static void AddTerminalKeyword(TerminalKeyword newTerminalKeyword)
        {
            string debugLog = "\n";
            DebugHelper.Log("Old TerminalKeywords Array Size Is: " + Terminal.terminalNodes.allKeywords.Length);
            Terminal.terminalNodes.allKeywords = Terminal.terminalNodes.allKeywords.AddItem(newTerminalKeyword).ToArray();
            DebugHelper.Log("New TerminalKeywords Array Size Is: " + Terminal.terminalNodes.allKeywords.Length);

            foreach (TerminalKeyword terminalKeyword in Terminal.terminalNodes.allKeywords)
                debugLog += terminalKeyword.word + " | " + terminalKeyword.defaultVerb + " \n";

            DebugHelper.Log(debugLog + "\n");
        }

        public static void AddRouteNode(TerminalKeyword terminalKeyword, TerminalNode newTerminalNode)
        {
            string debugString = string.Empty;
            debugString = "RouteNode Old CompatibleNoun Log Dump: " + "\n" + "\n";

            foreach (CompatibleNoun compatibleNoun in RouteKeyword.compatibleNouns)
                debugString += compatibleNoun.noun.word + " | " + compatibleNoun.result + " \n";

            DebugHelper.Log(debugString + "\n");
            CompatibleNoun newNoun = new CompatibleNoun();
            newNoun.noun = terminalKeyword;
            newNoun.result = newTerminalNode;

            RouteKeyword.compatibleNouns = RouteKeyword.compatibleNouns.AddItem(newNoun).ToArray();

            debugString = "RouteNode New CompatibleNoun Log Dump: " + "\n";

            foreach (CompatibleNoun compatibleNoun in RouteKeyword.compatibleNouns)
                debugString += compatibleNoun.noun.word + " | " + compatibleNoun.result + " \n";

            DebugHelper.Log(debugString);

            DebugHelper.DebugTerminalKeyword(terminalKeyword);
            DebugHelper.DebugTerminalKeyword(RouteKeyword);
            DebugHelper.DebugTerminalNode(newTerminalNode);
            DebugHelper.DebugTerminalKeyword(RouteKeyword.compatibleNouns[0].noun);
            DebugHelper.DebugTerminalNode(RouteKeyword.compatibleNouns[0].result);
        }

        public static void AddRouteInfo(TerminalKeyword terminalKeyword, TerminalNode newTerminalNode)
        {

        }

        //[HarmonyPatch(typeof(StartOfRound), "Awake")]
        //[HarmonyPostfix]
        /*public static void AddMoonInfo(TerminalKeyword MoonEntryName, TerminalNode MoonInfo)
        {
            //Resize our RouteKeyword array and put our new info into it
            Array.Resize<CompatibleNoun>(ref InfoKeyword.compatibleNouns, InfoKeyword.compatibleNouns.Length + 1);
            InfoKeyword.compatibleNouns[InfoKeyword.compatibleNouns.Length - 1] = new CompatibleNoun
            {
                noun = MoonEntryName,
                result = MoonInfo
            };
        }*/

        [HarmonyPatch(typeof(Terminal), "OnSubmit")]
        [HarmonyPrefix]
        public static void OnSubmit(Terminal __instance)
        {
            Debug.Log("LethalLib (Batby): OnSubmit Prefix");
            Debug.Log("LethalLib (Batby): OnSubmit Current Node: " + __instance.currentNode);
        }

        [HarmonyPatch(typeof(Terminal), "LoadNewNode")]
        [HarmonyPrefix]
        public static void LoadNewNode(Terminal __instance)
        {
            Debug.Log("LethalLib (Batby): LoadNewNode Prefix");
            Debug.Log("LethalLib (Batby): LoadNewNode Current Node: " + __instance.currentNode);
        }

        [HarmonyPatch(typeof(Terminal), "TextPostProcess")]
        [HarmonyPrefix]
        public static void TerminalTextProcess(Terminal __instance, ref string modifiedDisplayText, TerminalNode node)
        {
            Terminal terminal = __instance; //Redeclaring for clearer name.
            TerminalNode newNode = node; //Redeclaring for clearer name.

            int seperationCountMax = 3;
            int seperationCount;

            string unmodifiedDisplayText = "Welcome to the exomoons catalogue.\r\nTo route the autopilot to a moon, use the word ROUTE.\r\nTo learn about any moon, use the word INFO.\r\n____________________________\r\n\r\n* The Company building   //   Buying at [companyBuyingPercent].\r\n\r\n";
            string moonName;

            //DebugHelper.DebugAllLevels();
            //DebugHelper.DebugVanillaLevels();
            //DebugHelper.DebugCustomLevels();

            terminal.moonsCatalogueList = StartOfRound.Instance.levels;

            Debug.Log(StartOfRound.Instance.currentLevel.ToString() + StartOfRound.Instance.currentLevelID);

            Debug.Log("LethalLib (Batby): Logging All Levels List");

            Debug.Log("LethalLib (Batby): Attempting To Patch Terminal");
            //Debug.Log("LethalLib (Batby): ModifiedDisplayText Is: " + modifiedDisplayText);

            if (modifiedDisplayText != null && modifiedDisplayText != string.Empty && modifiedDisplayText.Contains("Welcome to the exomoons catalogue"))
            {
                Debug.Log("LethalLib (Batby): Adding Vanilla Levels (" + vanillaLevelsList.Count + " Found)");
                seperationCount = 0;
                foreach (ExtendedSelectableLevel vanillaLevel in vanillaLevelsList)
                {
                    Debug.Log("LethalLib (Batby): Listing " + vanillaLevel.SelectableLevel.PlanetName);
                    moonName = new string(vanillaLevel.SelectableLevel.PlanetName.SkipWhile(c => !char.IsLetter(c)).ToArray());
                    unmodifiedDisplayText += "* " + moonName + " " + GetMoonConditions(vanillaLevel.SelectableLevel) + "\n";
                    seperationCount++;
                    if (seperationCount == seperationCountMax)
                    {
                        unmodifiedDisplayText += "\n";
                        seperationCount = 0;
                    }
                }

                Debug.Log("LethalLib (Batby): Adding Custom Levels (" + customLevelsList.Count + " Found)");
                seperationCount = 0;
                foreach (ExtendedSelectableLevel customLevel in customLevelsList)
                {
                    Debug.Log("LethalLib (Batby): Listing " + customLevel.SelectableLevel.PlanetName);
                    moonName = new string(customLevel.SelectableLevel.PlanetName.SkipWhile(c => !char.IsLetter(c)).ToArray());
                    unmodifiedDisplayText += "* " + moonName + " " + GetMoonConditions(customLevel.SelectableLevel) + "\n";
                    seperationCount++;
                    if (seperationCount == seperationCountMax)
                    {
                        unmodifiedDisplayText += "\n";
                        seperationCount = 0;
                    }
                }

                unmodifiedDisplayText += "\r\n";
                //Debug.Log("LethalLib (Batby): modifiedDisplayText Is Now :" + "\n" + unmodifiedDisplayText);
                modifiedDisplayText = unmodifiedDisplayText;
            }
        }

        public static string GetMoonConditions(SelectableLevel selectableLevel)
        {
            string returnString = string.Empty;

            //This is in the LC decompile logic but the actual game doesn't do this so disabled for now.
            //if (selectableLevel.lockedForDemo)
                //returnString = " (Locked)";
            if (selectableLevel.currentWeather != LevelWeatherType.None)
                returnString = "(" + selectableLevel.currentWeather.ToString() + ")";

            return (returnString);
        }
    }
}
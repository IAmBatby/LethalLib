using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static LethalLib.Modules.Levels;

namespace LethalLib.Modules
{
    public class TerminalUtils {
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
        public static TerminalKeyword CreateTerminalKeyword(string word, bool isVerb = false, CompatibleNoun[] compatibleNouns = null, TerminalNode specialKeywordResult = null, TerminalKeyword defaultVerb = null, bool accessTerminalObjects = false) {

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

        private static bool PatchTerminal = true;
        public static Terminal ActiveTerminal;
        static TerminalKeyword RouteKeyword;
        static TerminalKeyword InfoKeyword;

        private static void GrabTerminal(){
            ActiveTerminal = GameObject.Find("TerminalScript").GetComponent<Terminal>(); //Terminal object reference 
            RouteKeyword = ActiveTerminal.terminalNodes.allKeywords[26];
            InfoKeyword = ActiveTerminal.terminalNodes.allKeywords[6];
        }
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        public static void AddMoonTerminalKeyword(TerminalKeyword MoonEntryName, SelectableLevel Level) { 
            TerminalKeyword TerminalEntry = MoonEntryName; //get our bundle's Terminal Keyword 
            TerminalEntry.defaultVerb = RouteKeyword;
            Array.Resize<SelectableLevel>(ref ActiveTerminal.moonsCatalogueList, ActiveTerminal.moonsCatalogueList.Length + 1); //Resize list of moons for catalogue
            ActiveTerminal.moonsCatalogueList[ActiveTerminal.moonsCatalogueList.Length -1 ] = Level; //Add our moon to that list
                
            Array.Resize<TerminalKeyword>(ref ActiveTerminal.terminalNodes.allKeywords, ActiveTerminal.terminalNodes.allKeywords.Length + 1);
            ActiveTerminal.terminalNodes.allKeywords[ActiveTerminal.terminalNodes.allKeywords.Length - 1] = TerminalEntry; //Add our terminal entry 
            TerminalEntry.defaultVerb = RouteKeyword; //Set its default verb to "route"


        }

        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        public static void AddRouteNode(TerminalKeyword MoonEntryName, TerminalNode RouteNode) {
            RouteNode.terminalOptions[0].noun = ActiveTerminal.terminalNodes.allKeywords[4];
            RouteNode.terminalOptions[0].result = new TerminalNode {
                displayText = "You have cancelled the order.",
                maxCharactersToType = 25,
                buyItemIndex = -1,
                buyRerouteToMoon = -1,
                displayPlanetInfo = -1,
                shipUnlockableID = -1,
                creatureFileID = -1,
                storyLogFileID = -1,
                playSyncedClip = -1
            };
            RouteNode.terminalOptions[1].noun = ActiveTerminal.terminalNodes.allKeywords[3];

            //Resize our RouteKeyword array and put our new route confirmation into it
            Array.Resize<CompatibleNoun>(ref RouteKeyword.compatibleNouns, RouteKeyword.compatibleNouns.Length + 1);
            RouteKeyword.compatibleNouns[RouteKeyword.compatibleNouns.Length - 1] = new CompatibleNoun {
                noun = MoonEntryName,
                result = RouteNode
            };
        }
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        public static void AddMoonInfo(TerminalKeyword MoonEntryName, TerminalNode MoonInfo) {
            //Resize our RouteKeyword array and put our new info into it
            Array.Resize<CompatibleNoun>(ref InfoKeyword.compatibleNouns, InfoKeyword.compatibleNouns.Length + 1);
            InfoKeyword.compatibleNouns[InfoKeyword.compatibleNouns.Length - 1] = new CompatibleNoun {
                noun = MoonEntryName,
                result = MoonInfo
            };
        }

        public static void AddMoonsToCatalogue() {
            if (!PatchTerminal) {
                return;
            }

            Dictionary<string, CustomLevel> moons = CustomLevelList;
            Terminal ActiveTerminal = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            TerminalNode specialKeywordResult = ActiveTerminal.terminalNodes.allKeywords[21].specialKeywordResult;
            specialKeywordResult.displayText.Substring(specialKeywordResult.displayText.Length - 3);

            foreach (string MoonName in CustomLevelList.Keys) {
                TerminalNode terminalNode = specialKeywordResult;
                terminalNode.displayText = terminalNode.displayText + "\n* " + MoonName + " [planetTime]";
            }

            TerminalNode terminalNode2 = specialKeywordResult;
            terminalNode2.displayText += "\n\n";
            PatchTerminal = false;
            return;
        }
    }
}

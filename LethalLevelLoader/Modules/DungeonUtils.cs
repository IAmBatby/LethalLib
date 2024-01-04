using DunGen.Graph;
using LethalLevelLoader.Extras;
using LethalLevelLoader.Modules;
using static LethalLevelLoader.Modules.Dungeon;
using System;
using System.Collections.Generic;
using System.Text;
using DunGen;
using UnityEngine;

namespace LethalLevelLoader.Modules
{
    public class DungeonUtils
    {
        public static void AddDungeon(DungeonFlow dungeon, int defaultRarity, string sourceName, ExtendedDungeonPreferences dungeonPrefences = null, AudioClip firstTimeDungeonAudio = null)
        {
            ExtendedDungeonFlow extendedDungeonFlow = ScriptableObject.CreateInstance<ExtendedDungeonFlow>();

            if (dungeonPrefences == null)
                extendedDungeonFlow.extendedDungeonPreferences = ScriptableObject.CreateInstance<ExtendedDungeonPreferences>();
            else
                extendedDungeonFlow.extendedDungeonPreferences = dungeonPrefences;

            extendedDungeonFlow.Initialize(dungeon, firstTimeDungeonAudio, ContentType.Custom, "sourceName", newDungeonRarity: defaultRarity);
            Dungeon.AddExtendedDungeonFlow(extendedDungeonFlow);
        }

        public static string debugString;

        public static ExtendedDungeonFlowWithRarity[] GetValidExtendedDungeonFlows(ExtendedLevel extendedLevel)
        {
            List<ExtendedDungeonFlowWithRarity> potentialExtendedDungeonFlowsList = new List<ExtendedDungeonFlowWithRarity>();
            List<ExtendedDungeonFlowWithRarity> returnExtendedDungeonFlowsList = new List<ExtendedDungeonFlowWithRarity>();

            debugString = "\n" + "Trying To Find All Matching DungeonFlows" + "\n";

            if (extendedLevel.allowedDungeonTypes == ContentType.Custom || extendedLevel.allowedDungeonTypes == ContentType.Any)
                foreach (ExtendedDungeonFlow customDungeonFlow in customDungeonFlowsList)
                    potentialExtendedDungeonFlowsList.Add(new ExtendedDungeonFlowWithRarity(customDungeonFlow, customDungeonFlow.dungeonRarity));

            if (extendedLevel.allowedDungeonTypes == ContentType.Vanilla || extendedLevel.allowedDungeonTypes == ContentType.Any)
                foreach (ExtendedDungeonFlowWithRarity vanillaDungeonFlow in GetVanillaExtendedDungeonFlows(extendedLevel))
                    potentialExtendedDungeonFlowsList.Add(vanillaDungeonFlow);

            debugString += "Potential DungeonFlows Collected, List Below: " + "\n";

            foreach (ExtendedDungeonFlowWithRarity debugDungeon in potentialExtendedDungeonFlowsList)
                debugString += debugDungeon.extendedDungeonFlow.name + ", ";

            debugString += "\n" + "\n";

            foreach (ExtendedDungeonFlowWithRarity customDungeonFlow in new List<ExtendedDungeonFlowWithRarity>(potentialExtendedDungeonFlowsList))
                if (MatchViaManualModList(extendedLevel, customDungeonFlow.extendedDungeonFlow, out int outRarity) == true)
                {
                    customDungeonFlow.rarity = outRarity;
                    returnExtendedDungeonFlowsList.Add(customDungeonFlow);
                    potentialExtendedDungeonFlowsList.Remove(customDungeonFlow);
                    debugString += "Matched " + extendedLevel.NumberlessPlanetName + " With " + customDungeonFlow.extendedDungeonFlow.name + " Based On Manual Mods List!" + "\n";
                }

            foreach (ExtendedDungeonFlowWithRarity customDungeonFlow in new List<ExtendedDungeonFlowWithRarity>(potentialExtendedDungeonFlowsList))
                if (MatchViaManualLevelList(extendedLevel, customDungeonFlow.extendedDungeonFlow, out int outRarity) == true)
                {
                    customDungeonFlow.rarity = outRarity;
                    returnExtendedDungeonFlowsList.Add(customDungeonFlow);
                    potentialExtendedDungeonFlowsList.Remove(customDungeonFlow);
                    debugString += "Matched " + extendedLevel.NumberlessPlanetName + " With " + customDungeonFlow.extendedDungeonFlow.name + " Based On Manual Levels List!" + "\n";
                }

            foreach (ExtendedDungeonFlowWithRarity customDungeonFlow in new List<ExtendedDungeonFlowWithRarity>(potentialExtendedDungeonFlowsList))
                if (MatchViaRoutePrice(extendedLevel, customDungeonFlow.extendedDungeonFlow, out int outRarity) == true)
                {
                    customDungeonFlow.rarity = outRarity;
                    returnExtendedDungeonFlowsList.Add(customDungeonFlow);
                    potentialExtendedDungeonFlowsList.Remove(customDungeonFlow);
                    debugString += "Matched " + extendedLevel.NumberlessPlanetName + " With " + customDungeonFlow.extendedDungeonFlow.name + " Based On Dynamic Route Price Settings!" + "\n";
                }


            debugString += extendedLevel.NumberlessPlanetName + " Level Tags: ";
            foreach (string tag in extendedLevel.levelTags)
                debugString += tag + ", ";
            debugString += "\n" + "\n";

            foreach (ExtendedDungeonFlowWithRarity customDungeonFlow in new List<ExtendedDungeonFlowWithRarity>(potentialExtendedDungeonFlowsList))
            {
                if (MatchViaLevelTags(extendedLevel, customDungeonFlow.extendedDungeonFlow, out int outRarity) == true)
                {
                    customDungeonFlow.rarity = outRarity;
                    returnExtendedDungeonFlowsList.Add(customDungeonFlow);
                    potentialExtendedDungeonFlowsList.Remove(customDungeonFlow);
                    debugString += " - Matched " + extendedLevel.NumberlessPlanetName + " With " + customDungeonFlow.extendedDungeonFlow.name + " Based On Level Tags!" + "\n";
                }
                else
                    debugString += "\n";
            }

            debugString += "\n" + "Matching DungeonFlows Collected, Count Is: " + returnExtendedDungeonFlowsList.Count + "\n";

            DebugHelper.Log(debugString + "\n");

            return (returnExtendedDungeonFlowsList.ToArray());
        }

        //If Moon Accepts Vanilla Dungeons, Add All The Ones Added With ID In The SelectableLevel.
        public static ExtendedDungeonFlowWithRarity[] GetVanillaExtendedDungeonFlows(ExtendedLevel extendedLevel)
        {
            List<ExtendedDungeonFlowWithRarity> returnExtendedDungeonFlowsList = new List<ExtendedDungeonFlowWithRarity>();

            foreach (IntWithRarity intWithRarity in extendedLevel.selectableLevel.dungeonFlowTypes)
                if (RoundManager.Instance.dungeonFlowTypes[intWithRarity.id] != null)
                    if (Dungeon.TryGetExtendedDungeonFlow(RoundManager.Instance.dungeonFlowTypes[intWithRarity.id], out ExtendedDungeonFlow outExtendedDungeonFlow, ContentType.Vanilla))
                        returnExtendedDungeonFlowsList.Add(new ExtendedDungeonFlowWithRarity(outExtendedDungeonFlow, intWithRarity.rarity));

            return (returnExtendedDungeonFlowsList.ToArray());
        }

        public static bool MatchViaManualModList(ExtendedLevel extendedLevel, ExtendedDungeonFlow extendedDungeonFlow, out int rarity)
        {
            rarity = extendedDungeonFlow.dungeonRarity;

            foreach (StringWithRarity stringWithRarity in extendedDungeonFlow.extendedDungeonPreferences.manualLevelSourceReferenceList)
                if (stringWithRarity.name.Contains(extendedLevel.sourceName))
                {
                    rarity = (int)stringWithRarity.spawnChance;
                    return (true);
                }

            return (false);
        }

        public static bool MatchViaManualLevelList(ExtendedLevel extendedLevel, ExtendedDungeonFlow extendedDungeonFlow, out int rarity)
        {
            rarity = extendedDungeonFlow.dungeonRarity;

            foreach (StringWithRarity stringWithRarity in extendedDungeonFlow.extendedDungeonPreferences.manualLevelNameReferenceList)
                if (stringWithRarity.name.Contains(extendedLevel.NumberlessPlanetName))
                {
                    rarity = (int)stringWithRarity.spawnChance;
                    return (true);
                }

            return (false);
        }

        public static bool MatchViaRoutePrice(ExtendedLevel extendedLevel, ExtendedDungeonFlow extendedDungeonFlow, out int rarity)
        {
            rarity = extendedDungeonFlow.dungeonRarity;

            foreach (Vector2WithRarity vectorWithRarity in extendedDungeonFlow.extendedDungeonPreferences.dynamicRoutePricesList)
            {
                if ((extendedLevel.levelCost >= vectorWithRarity.min) && (extendedLevel.levelCost <= vectorWithRarity.max))
                {
                    rarity = vectorWithRarity.rarity;
                    return (true);
                }
            }


            return (false);
        }

        public static bool MatchViaLevelTags(ExtendedLevel extendedLevel, ExtendedDungeonFlow extendedDungeonFlow, out int rarity)
        {
            rarity = extendedDungeonFlow.dungeonRarity;

            debugString += extendedDungeonFlow.name + " Level Tags: ";

            foreach (StringWithRarity stringWithRarity in extendedDungeonFlow.extendedDungeonPreferences.levelTagsList)
                debugString += stringWithRarity.name + ", ";

            foreach (string levelTag in extendedLevel.levelTags)
                foreach (StringWithRarity stringWithRarity in extendedDungeonFlow.extendedDungeonPreferences.levelTagsList)
                    if (stringWithRarity.name.Contains(levelTag))
                    {
                        rarity = (int)stringWithRarity.spawnChance;
                        return (true);
                    }

            return (false);
        }


    }
}

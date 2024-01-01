using HarmonyLib;
using LethalLib.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LethalLib.Modules
{
    public enum LevelType { Vanilla, Custom, Any } //Any included for built in checks.

    public class ExtendedLevel
    {
        public SelectableLevel selectableLevel;

        public string sourceName = "Lethal Company"; //Levels from AssetBundles will have this as their Assembly Name.
        public string NumberlessPlanetName
        {
            get
            {
                if (selectableLevel != null)
                    return new string(selectableLevel.PlanetName.SkipWhile(c => !char.IsLetter(c)).ToArray());
                else
                    return null;
            }
        } //ChatGPT did this one i'll be honest.

        public LevelType levelType;

        public ExtendedLevel(SelectableLevel newSelectableLevel, LevelType newLevelType, bool generateTerminalAssets, string newSourceName = "Lethal Company")
        {
            levelType = newLevelType;
            selectableLevel = newSelectableLevel;
            sourceName = newSourceName;

            if (newLevelType == LevelType.Custom)
            {
                selectableLevel.levelID = 9 + Levels.customLevelsList.Count;
                selectableLevel.sceneName = "InitSceneLaunchOptions";
            }

            if (generateTerminalAssets == true)
                TerminalUtils.CreateLevelTerminalData(this);

            Levels.AddSelectableLevel(this);
        }
    }
}
using DunGen.Graph;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalLevelLoader.Modules
{
    [CreateAssetMenu(menuName = "LethalLib/ExtendedDungeonFlow")]
    public class ExtendedDungeonFlow : ScriptableObject
    {
        public DungeonFlow dungeonFlow;
        public int dungeonID;
        public int dungeonRarity;

        public LevelType dungeonType;
        public string sourceName = "Lethal Company";
        public AudioClip dungeonFirstTimeAudio;

        public ExtendedDungeonPreferences extendedDungeonPreferences;



        public void Initialize(DungeonFlow newDungeonFlow, AudioClip newFirstTimeAudio, LevelType newDungeonType, string newSourceName, int newDungeonRarity = 0, ExtendedDungeonPreferences newPreferences = null)
        {
            dungeonFlow = newDungeonFlow;
            dungeonFirstTimeAudio = newFirstTimeAudio;
            dungeonType = newDungeonType;
            dungeonRarity = newDungeonRarity;

            //dungeonID = RoundManager.Instance.dungeonFlowTypes.Length + Dungeon.allExtendedDungeonsList.Count;

            if (extendedDungeonPreferences == null)
                extendedDungeonPreferences = new ExtendedDungeonPreferences();

            extendedDungeonPreferences.targetedLevelTags.Add("Custom");
        }
    }

    [Serializable]
    public class ExtendedDungeonPreferences
    {
        public List<string> targetedLevelTags = new List<string>();
    }
}

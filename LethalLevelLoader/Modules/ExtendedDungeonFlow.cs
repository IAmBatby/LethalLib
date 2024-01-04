using DunGen.Graph;
using LethalLevelLoader.Extras;
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

        public ContentType dungeonType;
        public string sourceName = "Lethal Company";
        public AudioClip dungeonFirstTimeAudio;

        public ExtendedDungeonPreferences extendedDungeonPreferences;



        public void Initialize(DungeonFlow newDungeonFlow, AudioClip newFirstTimeAudio, ContentType newDungeonType, string newSourceName, int newDungeonRarity = 0, ExtendedDungeonPreferences newPreferences = null)
        {
            dungeonFlow = newDungeonFlow;
            dungeonFirstTimeAudio = newFirstTimeAudio;
            dungeonType = newDungeonType;
            dungeonRarity = newDungeonRarity;

            if (name == string.Empty)
                name = dungeonFlow.name;

            //dungeonID = RoundManager.Instance.dungeonFlowTypes.Length + Dungeon.allExtendedDungeonsList.Count;

            if (extendedDungeonPreferences == null)
                extendedDungeonPreferences = ScriptableObject.CreateInstance<ExtendedDungeonPreferences>();

            if (dungeonType == ContentType.Custom)
                extendedDungeonPreferences.levelTagsList.Add(new StringWithRarity("Custom", 300));
            else
                extendedDungeonPreferences.levelTagsList.Add(new StringWithRarity("Vanilla", 300));
            extendedDungeonPreferences.levelCostMin = 700;

        }
    }

    /*[Serializable]
    public class ExtendedDungeonPreferences
    {
        public List<string> targetedLevelTags = new List<string>();
    }*/
}

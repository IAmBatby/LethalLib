using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalLib.Extras
{
    [CreateAssetMenu(menuName = "LethalLib/DungeonPreferences")]
    public class DungeonPreferencesDef : ScriptableObject
    {
        [Header("Dynamic Level List Settings")]
        public bool enableInjectionViaLevelCost;
        public int levelCostMin;
        public int levelCostMax;

        [Space(20)]

        public bool enableInjectionViaLevelDungeonMultiplierSetting;
        public int sizeMultiplierMin;
        public int sizeMultiplierMax;

        [Space(20)]

        public bool enableInjectionViaLevelTags;
        public List<StringWithRarity> levelTagsList = new List<StringWithRarity>();

        [Space(15)]
        [Header("Manual Level List Settings")]
        public List<StringWithRarity> manualLevelSourceReferenceList = new List<StringWithRarity>();
        [Space(5)]
        public List<StringWithRarity> manualLevelNameReferenceList = new List<StringWithRarity>();
    }

    [System.Serializable]
    public class StringWithRarity
    {
        public string name;
        [Range(0, 1)]
        public float spawnChance;
    }
}

using DunGen.Graph;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalLevelLoader.Extras
{
    [CreateAssetMenu(menuName = "ScriptableObjects/DungeonDef")]
    public class DungeonDef : ScriptableObject
    {
        public DungeonFlow dungeonFlow;
        [Range(0f, 300f)]
        public int rarity;
        public AudioClip firstTimeDungeonAudio;
    }
}

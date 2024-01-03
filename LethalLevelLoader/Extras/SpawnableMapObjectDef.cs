using LethalLevelLoader.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalLevelLoader.Extras
{
    [CreateAssetMenu(menuName = "ScriptableObjects/SpawnableMapObject")]
    public class SpawnableMapObjectDef : ScriptableObject
    {
        public SpawnableMapObject spawnableMapObject;
    }
}

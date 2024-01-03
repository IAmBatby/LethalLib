using LethalLevelLoader.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalLevelLoader.Extras
{
    [CreateAssetMenu(menuName = "ScriptableObjects/SpawnableOutsideObject")]
    public class SpawnableOutsideObjectDef : ScriptableObject
    {
        public SpawnableOutsideObjectWithRarity spawnableMapObject;
    }
}

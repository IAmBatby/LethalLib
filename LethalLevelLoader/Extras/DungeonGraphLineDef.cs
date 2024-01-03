using DunGen.Graph;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalLevelLoader.Extras
{
    [CreateAssetMenu(menuName = "ScriptableObjects/DungeonGraphLine")]
    public class DungeonGraphLineDef : ScriptableObject
    {
        public GraphLine graphLine;
    }
}

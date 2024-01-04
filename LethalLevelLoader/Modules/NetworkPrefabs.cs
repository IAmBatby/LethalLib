using System;
using System.Collections.Generic;
using System.Text;
//using static LethalLib.Modules.Items;
using static LethalLevelLoader.Plugin;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using LethalLevelLoader.Extras;

namespace LethalLevelLoader.Modules
{
    public class NetworkPrefabs
    {
        private static List<GameObject> _networkPrefabs = new List<GameObject>();
        internal static void Init()
        {
            On.GameNetworkManager.Start += GameNetworkManager_Start;
        }

        /// <summary>
        /// Registers a prefab to be added to the network manager.
        /// </summary>
        public static void RegisterNetworkPrefab(GameObject prefab)
        {
            if (!_networkPrefabs.Contains(prefab))
                _networkPrefabs.Add(prefab);
            //UnityEngine.Object.FindObjectOfType<NetworkManager>().AddNetworkPrefab(prefab);
        }

        private static void GameNetworkManager_Start(On.GameNetworkManager.orig_Start orig, GameNetworkManager self)
        {
            orig(self);

            DebugHelper.Log("Game NetworkManager Start");

            foreach (GameObject obj in _networkPrefabs)
            {
                DebugHelper.Log("Registering: " + obj.name);
                self.GetComponent<NetworkManager>().AddNetworkPrefab(obj);
            }
            
        }
    }
}

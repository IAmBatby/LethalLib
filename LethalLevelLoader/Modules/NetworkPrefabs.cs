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
        private static List<string> _networkPrefabNames = new List<string>();
        internal static void Init()
        {
            On.GameNetworkManager.Start += GameNetworkManager_Start;
        }

        /// <summary>
        /// Registers a prefab to be added to the network manager.
        /// </summary>
        public static void RegisterNetworkPrefab(GameObject prefab)
        {
            _networkPrefabs.Add(prefab);
            _networkPrefabNames.Add(prefab.name);
            //UnityEngine.Object.FindObjectOfType<NetworkManager>().AddNetworkPrefab(prefab);
        }

        private static void GameNetworkManager_Start(On.GameNetworkManager.orig_Start orig, GameNetworkManager self)
        {
            orig(self);

            DebugHelper.Log("Game NetworkManager Start");

            NetworkManager networkManager = self.GetComponent<NetworkManager>();

            List<GameObject> registeredPrefabs = new List<GameObject>();

            foreach (NetworkPrefab networkPrefab in networkManager.NetworkConfig.Prefabs.m_Prefabs)
                registeredPrefabs.Add(networkPrefab.Prefab);

            foreach (GameObject obj in _networkPrefabs)
            {
                if (!registeredPrefabs.Contains(obj))
                {
                    self.GetComponent<NetworkManager>().AddNetworkPrefab(obj);
                }
            }
            
        }
    }
}

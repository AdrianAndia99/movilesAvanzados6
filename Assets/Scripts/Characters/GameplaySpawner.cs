using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Game.Lobby;
using Game.Characters;

namespace Game.Gameplay
{
    public class GameplaySpawner : NetworkBehaviour
    {
        [SerializeField] private Transform[] spawnPoints; // optional preset spawn points
        [SerializeField] private CharacterDatabase characterDatabase;

        private bool _spawned = false;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                Debug.Log("[GameplaySpawner] OnNetworkSpawn (Server) subscribing to OnLoadComplete");
                NetworkManager.SceneManager.OnLoadComplete += OnSceneLoadComplete;
            }
            else
            {
                Debug.Log("[GameplaySpawner] OnNetworkSpawn (Client) - no subscription");
            }
        }

    private new void OnDestroy()
        {
            if (NetworkManager != null && NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
            }
        }

        private void OnSceneLoadComplete(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
        {
            if (!IsServer) return;

            if (!_spawned)
            {
                Debug.Log($"[GameplaySpawner] OnLoadComplete triggered by client {clientId} for scene '{sceneName}' loadMode={loadSceneMode}. Spawning players now.");
                SpawnAllPlayers();
                _spawned = true;
            }
            else
            {
                Debug.Log("[GameplaySpawner] OnLoadComplete called but spawn already happened.");
            }
        }

        private void SpawnAllPlayers()
        {
            Debug.Log("[GameplaySpawner] SpawnAllPlayers start");
            var lobbyManager = LobbyManager.Instance;
            if (lobbyManager == null)
            {
                Debug.LogError("[GameplaySpawner] No LobbyManager found in gameplay scene.");
                return;
            }

            // Fallback: if this spawner's CharacterDatabase no asignado, intenta usar el del lobby
            if (characterDatabase == null)
            {
                characterDatabase = lobbyManager.CharacterDatabase;
                Debug.Log("[GameplaySpawner] Adopted CharacterDatabase from LobbyManager");
            }

            if (characterDatabase == null)
            {
                Debug.LogError("[GameplaySpawner] CharacterDatabase no asignado en GameplaySpawner.");
                return;
            }

            // 1. Copiar datos de selección antes de eliminar los LobbyPlayer como PlayerObjects
            var selections = new Dictionary<ulong, int>();
            var playersSnapshot = new List<LobbyPlayer>(lobbyManager.Players); // snapshot para evitar modificación durante despawn
            foreach (var lp in playersSnapshot)
            {
                selections[lp.OwnerClientIdCached] = lp.CharacterIndex.Value;
                Debug.Log($"[GameplaySpawner] Cached selection ClientId={lp.OwnerClientIdCached} CharIndex={lp.CharacterIndex.Value}");
            }

            // 2. Despawn de los player objects actuales (LobbyPlayer)
            foreach (var lp in playersSnapshot)
            {
                if (lp != null && lp.NetworkObject != null && lp.NetworkObject.IsSpawned)
                {
                    Debug.Log($"[GameplaySpawner] Despawning lobby player object for ClientId={lp.OwnerClientIdCached}");
                    lp.NetworkObject.Despawn(true); // true => destruir instancia server-side
                }
                else
                {
                    Debug.LogWarning("[GameplaySpawner] LobbyPlayer missing or not spawned while attempting despawn.");
                }
            }

            // 3. Spawn de los nuevos prefabs seleccionados como PlayerObjects
            int i = 0;
            foreach (var kvp in selections)
            {
                var clientId = kvp.Key;
                var charIndex = kvp.Value;
                var prefab = characterDatabase.Get(charIndex);
                if (prefab == null)
                {
                    Debug.LogWarning($"[GameplaySpawner] Missing character prefab for index {charIndex}, defaulting index 0");
                    prefab = characterDatabase.Get(0);
                    if (prefab == null) continue;
                }

                Vector3 pos;
                Quaternion rot;
                if (spawnPoints != null && spawnPoints.Length > 0)
                {
                    var sp = spawnPoints[i % spawnPoints.Length];
                    pos = sp.position; rot = sp.rotation;
                }
                else
                {
                    pos = new Vector3(i * 2f, 0, 0);
                    rot = Quaternion.identity;
                }

                Debug.Log($"[GameplaySpawner] Spawning character for ClientId={clientId} CharIndex={charIndex} at {pos}");
                var instance = Instantiate(prefab, pos, rot);
                var netObj = instance.GetComponent<NetworkObject>();
                if (netObj == null)
                {
                    Debug.LogError("[GameplaySpawner] Character prefab missing NetworkObject component");
                    Destroy(instance);
                    continue;
                }

                // Importante: SpawnAsPlayerObject hará que este nuevo objeto sea el PlayerObject del cliente
                netObj.SpawnAsPlayerObject(clientId, true);
                Debug.Log($"[GameplaySpawner] Spawned PlayerObject NetId={netObj.NetworkObjectId} for ClientId={clientId}");
                i++;
            }
            Debug.Log("[GameplaySpawner] SpawnAllPlayers complete");
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Game.Characters;

namespace Game.Lobby
{
    public class LobbyManager : NetworkBehaviour
    {
        [Header("Config")] [SerializeField] private int maxPlayers = 5;
        [SerializeField] private string gameplaySceneName = "GameScene"; // replace with actual gameplay scene name
        [SerializeField] private CharacterDatabase characterDatabase;

        public static LobbyManager Instance { get; private set; }

        private readonly List<LobbyPlayer> _players = new();
        public IReadOnlyList<LobbyPlayer> Players => _players;

        public override void OnNetworkSpawn()
        {
            Debug.Log($"[LobbyManager] OnNetworkSpawn IsServer={IsServer} IsHost={NetworkManager.IsHost}");
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[LobbyManager] Duplicate instance detected. Destroying new one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (IsServer)
            {
                DontDestroyOnLoad(gameObject);
                Debug.Log("[LobbyManager] Marked as DontDestroyOnLoad");
                LobbyPlayer.OnLobbyPlayerSpawned += HandlePlayerSpawned;
                LobbyPlayer.OnLobbyPlayerDespawned += HandlePlayerDespawned;

                // Escaneo inicial: puede que el host ya tenga su LobbyPlayer spawneado antes de que suscribamos eventos
                var existing = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);
                foreach (var lp in existing)
                {
                    if (!_players.Contains(lp))
                    {
                        _players.Add(lp);
                        Debug.Log($"[LobbyManager] Initial scan added existing LobbyPlayer (ClientId={lp.OwnerClientIdCached}) CharIndex={lp.CharacterIndex.Value}");
                    }
                }
                Debug.Log($"[LobbyManager] Initial player count after scan: {_players.Count}");
            }
        }

    private new void OnDestroy()
        {
            if (Instance == this)
            {
                LobbyPlayer.OnLobbyPlayerSpawned -= HandlePlayerSpawned;
                LobbyPlayer.OnLobbyPlayerDespawned -= HandlePlayerDespawned;
                Instance = null;
            }
        }

        private void HandlePlayerSpawned(LobbyPlayer player)
        {
            if (!_players.Contains(player))
            {
                _players.Add(player);
                Debug.Log($"[LobbyManager] Player spawned (ClientId={player.OwnerClientIdCached}) CharIndex={player.CharacterIndex.Value} Total={_players.Count}");
            }
            else
            {
                Debug.LogWarning($"[LobbyManager] Duplicate player reference ignored (ClientId={player.OwnerClientIdCached})");
            }
        }

        private void HandlePlayerDespawned(LobbyPlayer player)
        {
            if (_players.Contains(player))
            {
                _players.Remove(player);
                Debug.Log($"[LobbyManager] Player despawned (ClientId={player.OwnerClientIdCached}) Remaining={_players.Count}");
            }
        }

        public bool PlayerCountExceedsLimit() => _players.Count >= maxPlayers;

        public bool AllPlayersReady()
        {
            if (_players.Count == 0) return false;
            foreach (var p in _players)
            {
                if (!p.IsReady.Value)
                {
                    return false;
                }
            }
            return true;
        }

    public new bool IsHost(ulong clientId) => NetworkManager.IsHost && NetworkManager.LocalClientId == clientId;

        [ServerRpc(RequireOwnership = false)]
        public void RequestStartGameServerRpc(ServerRpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;
            Debug.Log($"[LobbyManager] StartGame requested by ClientId={senderId}");
            // Only host (server local) can start
            if (senderId != NetworkManager.LocalClientId)
            {
                Debug.LogWarning($"[LobbyManager] Reject start request from non-host ClientId={senderId}");
                return;
            }
            if (!AllPlayersReady())
            {
                Debug.LogWarning("[LobbyManager] Cannot start: not all players ready.");
                return;
            }

            if (NetworkManager.SceneManager != null)
            {
                Debug.Log($"[LobbyManager] Loading gameplay scene '{gameplaySceneName}' with {_players.Count} players.");
                foreach (var p in _players)
                {
                    p.ResetReadyServer();
                }
                NetworkManager.SceneManager.LoadScene(gameplaySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError("[LobbyManager] No NetworkSceneManager available to load scene");
            }
        }

        public CharacterDatabase CharacterDatabase => characterDatabase;
    }
}

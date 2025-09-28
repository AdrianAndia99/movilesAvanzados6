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
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate LobbyManager detected. Destroying new one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (IsServer)
            {
                LobbyPlayer.OnLobbyPlayerSpawned += HandlePlayerSpawned;
                LobbyPlayer.OnLobbyPlayerDespawned += HandlePlayerDespawned;
            }
        }

        private void OnDestroy()
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
            }
        }

        private void HandlePlayerDespawned(LobbyPlayer player)
        {
            if (_players.Contains(player))
            {
                _players.Remove(player);
            }
        }

        public bool PlayerCountExceedsLimit() => _players.Count >= maxPlayers;

        public bool AllPlayersReady()
        {
            if (_players.Count == 0) return false;
            foreach (var p in _players)
            {
                if (!p.IsReady.Value) return false;
            }
            return true;
        }

        public bool IsHost(ulong clientId) => NetworkManager.IsHost && NetworkManager.LocalClientId == clientId;

        [ServerRpc(RequireOwnership = false)]
        public void RequestStartGameServerRpc(ServerRpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;
            // Only host can start
            if (senderId != NetworkManager.LocalClientId) return;
            if (!AllPlayersReady()) return;

            // Transition using NetworkSceneManager
            if (NetworkManager.SceneManager != null)
            {
                foreach (var p in _players)
                {
                    p.ResetReadyServer(); // clear ready state to avoid carry-over
                }
                NetworkManager.SceneManager.LoadScene(gameplaySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError("No NetworkSceneManager available to load scene");
            }
        }

        public CharacterDatabase CharacterDatabase => characterDatabase;
    }
}

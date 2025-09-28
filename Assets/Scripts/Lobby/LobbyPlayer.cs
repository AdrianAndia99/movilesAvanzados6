using UnityEngine;
using Unity.Netcode;
using System;
using Unity.Collections;

namespace Game.Lobby
{
    public class LobbyPlayer : NetworkBehaviour
    {
        public static event Action<LobbyPlayer> OnLobbyPlayerSpawned; // when added to lobby
        public static event Action<LobbyPlayer> OnLobbyPlayerDespawned; // when removed
        public static event Action<LobbyPlayer> OnReadyStateChanged;
        public static event Action<LobbyPlayer> OnCharacterChanged;

        [Tooltip("Player display name (client authority to set once).")]
        public NetworkVariable<FixedString32Bytes> DisplayName = new NetworkVariable<FixedString32Bytes>("Player", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> CharacterIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public ulong OwnerClientIdCached => OwnerClientId;

        private const int MaxCharacters = 30; // available characters 0..29

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Limit max players (enforced also in LobbyManager)
                var lobbyManager = FindObjectOfType<LobbyManager>();
                if (lobbyManager != null && lobbyManager.PlayerCountExceedsLimit())
                {
                    // Kick this late join
                    NetworkObject.Despawn();
                    return;
                }
            }

            IsReady.OnValueChanged += (_, __) => OnReadyStateChanged?.Invoke(this);
            CharacterIndex.OnValueChanged += (_, __) => OnCharacterChanged?.Invoke(this);
            OnLobbyPlayerSpawned?.Invoke(this);
        }

        public override void OnNetworkDespawn()
        {
            OnLobbyPlayerDespawned?.Invoke(this);
            IsReady.OnValueChanged -= (_, __) => OnReadyStateChanged?.Invoke(this);
            CharacterIndex.OnValueChanged -= (_, __) => OnCharacterChanged?.Invoke(this);
        }

        [ServerRpc(RequireOwnership = true)]
        public void SetDisplayNameServerRpc(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) newName = $"Player{OwnerClientId}";
            DisplayName.Value = newName.Substring(0, Math.Min(32, newName.Length));
        }

        [ServerRpc(RequireOwnership = true)]
        public void ToggleReadyServerRpc()
        {
            IsReady.Value = !IsReady.Value;
        }

        [ServerRpc(RequireOwnership = true)]
        public void SetCharacterServerRpc(int index)
        {
            if (index < 0 || index >= MaxCharacters) return;
            CharacterIndex.Value = index;
        }

        public void ResetReadyServer()
        {
            if (IsServer) IsReady.Value = false;
        }
    }
}

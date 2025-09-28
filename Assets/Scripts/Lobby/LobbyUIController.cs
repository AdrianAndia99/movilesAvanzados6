using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Game.Lobby;
using Game.Characters;

namespace Game.UI
{
    public class LobbyUIController : MonoBehaviour
    {
        [Header("References")] [SerializeField] private Button readyButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button nextCharacterButton;
        [SerializeField] private Button prevCharacterButton;
        [SerializeField] private Image characterPreviewImage; // optional (if using sprites)
        [SerializeField] private TextMeshProUGUI playerListText;
        [SerializeField] private TextMeshProUGUI statusText;

        private LobbyPlayer localLobbyPlayer;
        private int localCharacterIndex = 0;
        private CharacterDatabase characterDatabase;

        private void Awake()
        {
            if (readyButton) readyButton.onClick.AddListener(OnReadyClicked);
            if (startGameButton) startGameButton.onClick.AddListener(OnStartGameClicked);
            if (nextCharacterButton) nextCharacterButton.onClick.AddListener(() => ChangeCharacter(1));
            if (prevCharacterButton) prevCharacterButton.onClick.AddListener(() => ChangeCharacter(-1));
        }

        private void Start()
        {
            characterDatabase = FindObjectOfType<CharacterDatabase>();
            InvokeRepeating(nameof(RefreshUI), 0.2f, 0.5f); // simple polling for demo; could use events
        }

        private void UpdateLocalPlayerRef()
        {
            if (localLobbyPlayer != null) return;
            foreach (var lp in FindObjectsOfType<LobbyPlayer>())
            {
                if (lp.IsOwner)
                {
                    localLobbyPlayer = lp;
                    localCharacterIndex = lp.CharacterIndex.Value;
                    break;
                }
            }
        }

        private void RefreshUI()
        {
            UpdateLocalPlayerRef();
            var lobbyManager = LobbyManager.Instance;
            if (lobbyManager == null)
            {
                statusText.text = "LobbyManager not found";
                return;
            }

            // Player list
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var p in lobbyManager.Players)
            {
                sb.Append(p.DisplayName.Value.ToString());
                sb.Append(" - ");
                sb.Append(p.IsReady.Value ? "READY" : "...");
                sb.Append(" Char:");
                sb.Append(p.CharacterIndex.Value);
                sb.AppendLine();
            }
            if (playerListText) playerListText.text = sb.ToString();

            bool allReady = lobbyManager.AllPlayersReady();
            if (statusText) statusText.text = allReady ? "All players ready" : "Waiting players...";

            if (startGameButton)
            {
                startGameButton.interactable = NetworkManager.Singleton.IsHost && allReady;
            }
        }

        private void OnReadyClicked()
        {
            if (localLobbyPlayer == null) return;
            if (localLobbyPlayer.IsOwner)
            {
                localLobbyPlayer.ToggleReadyServerRpc();
            }
        }

        private void OnStartGameClicked()
        {
            if (!NetworkManager.Singleton.IsHost) return;
            LobbyManager.Instance.RequestStartGameServerRpc();
        }

        private void ChangeCharacter(int delta)
        {
            if (localLobbyPlayer == null) return;
            int newIndex = (localLobbyPlayer.CharacterIndex.Value + delta + 30) % 30;
            localLobbyPlayer.SetCharacterServerRpc(newIndex);
            localCharacterIndex = newIndex;
        }
    }
}

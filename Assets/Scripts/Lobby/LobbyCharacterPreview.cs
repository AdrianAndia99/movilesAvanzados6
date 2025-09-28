using UnityEngine;
using Game.Characters;
using Game.Lobby;
using Unity.Netcode;

namespace Game.UI
{
    // Attach this to a child of the Lobby UI or a dedicated 3D area (e.g., a small stage) for local player preview.
    public class LobbyCharacterPreview : MonoBehaviour
    {
        [SerializeField] private Transform previewRoot; // where to spawn visual
        [SerializeField] private CharacterDatabase characterDatabase;
        [SerializeField] private bool onlyForLocalPlayer = true; // set false if you want to pass a LobbyPlayer reference manually

        private LobbyPlayer localPlayer;
        private GameObject currentPreviewInstance;
        private int lastIndex = -1;

        private void Start()
        {
            if (previewRoot == null) previewRoot = transform;
            if (characterDatabase == null) characterDatabase = FindObjectOfType<CharacterDatabase>();
            InvokeRepeating(nameof(Tick), 0.25f, 0.25f); // simple polling; can be event-driven
        }

        private void Tick()
        {
            if (localPlayer == null)
            {
                foreach (var lp in FindObjectsOfType<LobbyPlayer>())
                {
                    if (!onlyForLocalPlayer || lp.IsOwner)
                    {
                        localPlayer = lp;
                        break;
                    }
                }
                if (localPlayer == null) return;
            }

            int idx = localPlayer.CharacterIndex.Value;
            if (idx != lastIndex)
            {
                RefreshPreview(idx);
                lastIndex = idx;
            }
        }

        private void RefreshPreview(int characterIndex)
        {
            if (characterDatabase == null) return;
            if (currentPreviewInstance != null)
            {
                Destroy(currentPreviewInstance);
            }
            var prefab = characterDatabase.Get(characterIndex);
            if (prefab == null) return;

            // We only want the visual model, not the network behaviour.
            currentPreviewInstance = Instantiate(prefab, previewRoot);
            currentPreviewInstance.transform.localPosition = Vector3.zero;
            currentPreviewInstance.transform.localRotation = Quaternion.identity;

            // Remove NetworkObject if present to avoid warnings in lobby preview.
            var netObj = currentPreviewInstance.GetComponent<NetworkObject>();
            if (netObj)
            {
                Destroy(netObj);
            }
        }
    }
}

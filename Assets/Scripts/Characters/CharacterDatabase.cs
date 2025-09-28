using UnityEngine;

namespace Game.Characters
{
    [CreateAssetMenu(menuName = "Game/Character Database", fileName = "CharacterDatabase")]
    public class CharacterDatabase : ScriptableObject
    {
        [Tooltip("List of available character prefabs (should have NetworkObject). Index 0..29")] 
        public GameObject[] characterPrefabs = new GameObject[30];

        public GameObject Get(int index)
        {
            if (index < 0 || index >= characterPrefabs.Length)
            {
                Debug.LogWarning($"Character index {index} out of range");
                return null;
            }
            return characterPrefabs[index];
        }
    }
}

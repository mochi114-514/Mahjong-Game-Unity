using UnityEngine;

namespace MahjongPrototype.UI3D
{
    // PROTOTYPE: scene-only helper that triggers 3D tile instantiate verification.
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong 3D Tile Spawn Test")]
    public sealed class Mahjong3DTileSpawnTest : MonoBehaviour
    {
        [SerializeField] private Mahjong3DHandView handView;
        [SerializeField] private bool spawnOnStart = true;

        private void Start()
        {
            if (spawnOnStart)
                Spawn();
        }

        public void Spawn()
        {
            if (handView == null)
            {
                Debug.LogWarning($"{nameof(Mahjong3DTileSpawnTest)}: Hand view is not assigned.", this);
                return;
            }

            handView.SpawnTestTiles();
        }
    }
}

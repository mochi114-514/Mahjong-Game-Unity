using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [CreateAssetMenu(
        fileName = "MahjongTileSpriteCatalog",
        menuName = "Mahjong Prototype/UI/Tile Sprite Catalog")]
    public sealed class MahjongTileSpriteCatalog : ScriptableObject
    {
        [SerializeField] private List<Entry> entries = new List<Entry>();

        public bool TryGetSprite(Tile tile, out Sprite sprite)
        {
            sprite = null;

            int typeIndex = tile.TypeIndex;
            if (typeIndex < 0 || entries == null)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null)
                    continue;

                if (entry.TypeIndex == typeIndex && entry.Sprite != null)
                {
                    sprite = entry.Sprite;
                    return true;
                }
            }

            return false;
        }

        [Serializable]
        public sealed class Entry
        {
            [SerializeField, Range(0, 33)] private int typeIndex;
            [SerializeField] private Sprite sprite;

            public int TypeIndex => typeIndex;
            public Sprite Sprite => sprite;
        }
    }
}

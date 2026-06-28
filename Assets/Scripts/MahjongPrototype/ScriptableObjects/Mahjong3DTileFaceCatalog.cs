using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.UI3D
{
    [CreateAssetMenu(
        fileName = "Mahjong3DTileFaceCatalog",
        menuName = "Mahjong Prototype/UI3D/Tile Face Catalog")]
    public sealed class Mahjong3DTileFaceCatalog : ScriptableObject
    {
        [SerializeField] private List<Entry> entries = new List<Entry>();

        public bool TryGetFrontFaceMesh(Tile tile, out Mesh mesh)
        {
            mesh = null;

            int typeIndex = tile.TypeIndex;
            if (typeIndex < 0 || entries == null)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null || entry.TypeIndex != typeIndex)
                    continue;

                if (entry.FrontFaceMesh == null)
                    return false;

                mesh = entry.FrontFaceMesh;
                return true;
            }

            return false;
        }

        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private int typeIndex;
            [SerializeField] private Mesh frontFaceMesh;

            public int TypeIndex => typeIndex;
            public Mesh FrontFaceMesh => frontFaceMesh;
        }
    }
}

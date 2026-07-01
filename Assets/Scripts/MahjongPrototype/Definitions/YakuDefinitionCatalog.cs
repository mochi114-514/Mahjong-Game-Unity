using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.Definitions
{
    [CreateAssetMenu(
        fileName = "YakuDefinitionCatalog",
        menuName = "Mahjong Prototype/Yaku Definition Catalog")]
    public sealed class YakuDefinitionCatalog : ScriptableObject
    {
        [SerializeField] private List<YakuDefinition> definitions = new List<YakuDefinition>();

        public IReadOnlyList<YakuDefinition> Definitions => definitions;

        public bool TryGet(YakuKind kind, out YakuDefinition definition)
        {
            if (definitions != null)
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    YakuDefinition candidate = definitions[i];
                    if (candidate == null || !candidate.IsEnabled || candidate.Kind != kind)
                        continue;

                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public bool Contains(YakuKind kind)
        {
            return TryGet(kind, out _);
        }
    }
}

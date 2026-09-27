using System.Collections.Generic;
using UnityEngine;

namespace GodTower.Level
{
    /// <summary>Ordered list of playable levels. Loaded from Resources by GameSession.</summary>
    [CreateAssetMenu(menuName = "God Tower/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] List<LevelConfig> levels = new List<LevelConfig>();

        public int Count => levels.Count;

        public LevelConfig Get(int index) =>
            index >= 0 && index < levels.Count ? levels[index] : null;

        public void SetLevels(IEnumerable<LevelConfig> configs)
        {
            levels.Clear();
            levels.AddRange(configs);
        }
    }
}

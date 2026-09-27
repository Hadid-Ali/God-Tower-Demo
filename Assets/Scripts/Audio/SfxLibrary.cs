using System;
using UnityEngine;

namespace GodTower.Audio
{
    /// <summary>
    /// Optional clip overrides. Place an instance at Resources/SfxLibrary and assign real clips;
    /// anything left empty falls back to the synthesized placeholder.
    /// </summary>
    [CreateAssetMenu(menuName = "God Tower/Sfx Library", fileName = "SfxLibrary")]
    public sealed class SfxLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public SfxId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        public Entry[] entries = Array.Empty<Entry>();

        public bool TryGet(SfxId id, out Entry entry)
        {
            foreach (Entry e in entries)
            {
                if (e.id != id || e.clip == null) continue;
                entry = e;
                return true;
            }
            entry = default;
            return false;
        }
    }
}

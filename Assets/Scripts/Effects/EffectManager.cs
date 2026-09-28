using System;
using System.Collections.Generic;
using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>
    /// Single entry point for triggering effects by type; also owns cleanup for retry/exit.
    /// Effect prefabs in the list are spawned into the game under this manager when the level starts, and the spawned
    /// copies are what run (the prefab assets are never touched). Effects already placed in the scene are used as-is.
    /// Fault-tolerant by design: an empty list, a missing type, or an effect that throws (e.g. an unassigned
    /// prefab) is logged and skipped so the level keeps running.
    /// </summary>
    public sealed class EffectManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Effect prefabs (spawned at level start) or effects already in the scene.")] EffectBase[] effects = Array.Empty<EffectBase>();

        readonly Dictionary<EffectType, EffectBase> _byType = new Dictionary<EffectType, EffectBase>();
        readonly HashSet<EffectType> _reportedMissing = new HashSet<EffectType>();
        readonly List<EffectBase> _live = new List<EffectBase>();
        readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>Raised after an effect starts, with its type and who triggered it.</summary>
        public event Action<EffectType, string> EffectTriggered;

        public void Initialize(EffectContext context)
        {
            _byType.Clear();
            _reportedMissing.Clear();
            DespawnAll();
            if (effects == null) return;

            foreach (EffectBase entry in effects)
            {
                if (entry == null) continue;
                try
                {
                    EffectBase effect = Spawn(entry);
                    _live.Add(effect);
                    effect.Initialize(context);
                    _byType[effect.Type] = effect;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EffectManager] {entry.name} failed to initialize and is disabled: {e.Message}", entry);
                }
            }

            if (_byType.Count == 0) Debug.LogWarning("[EffectManager] No effects registered; triggers will be ignored.", this);
        }

        /// <param name="pushMeters">Push distance for effects that push the climber; 0 = the effect's own setting.</param>
        public bool Trigger(EffectType type, string source, float pushMeters = 0f)
        {
            if (!_byType.TryGetValue(type, out EffectBase effect) || effect == null)
            {
                if (_reportedMissing.Add(type)) Debug.LogWarning($"[EffectManager] No effect registered for {type}; ignoring.", this);
                return false;
            }

            try
            {
                if (pushMeters > 0f) effect.Play(pushMeters);
                else effect.Play();
            }
            catch (Exception e)
            {
                Debug.LogException(e, effect);
                SafeClear(effect);
                return false;
            }

            try { EffectTriggered?.Invoke(type, source); }
            catch (Exception e) { Debug.LogException(e, this); }
            return true;
        }

        public void ClearAll()
        {
            foreach (EffectBase effect in _live) SafeClear(effect);
        }

        /// <summary>A prefab asset has no scene, so it gets a live copy under this manager; scene effects are used directly.</summary>
        EffectBase Spawn(EffectBase entry)
        {
            if (entry.gameObject.scene.IsValid()) return entry;
            EffectBase copy = Instantiate(entry, transform);
            copy.name = entry.name;
            _spawned.Add(copy.gameObject);
            return copy;
        }

        void DespawnAll()
        {
            foreach (GameObject spawned in _spawned)
            {
                if (spawned != null) Destroy(spawned);
            }
            _spawned.Clear();
            _live.Clear();
        }

        static void SafeClear(EffectBase effect)
        {
            if (effect == null) return;
            try { effect.Clear(); }
            catch (Exception e) { Debug.LogException(e, effect); }
        }
    }
}

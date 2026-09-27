using System;
using System.Collections.Generic;
using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>
    /// Single entry point for triggering effects by type; also owns cleanup for retry/exit.
    /// Fault-tolerant by design: an empty list, a missing type, or an effect that throws (e.g. an unassigned
    /// prefab) is logged and skipped so the level keeps running.
    /// </summary>
    public sealed class EffectManager : MonoBehaviour
    {
        [SerializeField] EffectBase[] effects = Array.Empty<EffectBase>();

        readonly Dictionary<EffectType, EffectBase> _byType = new Dictionary<EffectType, EffectBase>();
        readonly HashSet<EffectType> _reportedMissing = new HashSet<EffectType>();

        /// <summary>Raised after an effect starts, with its type and who triggered it.</summary>
        public event Action<EffectType, string> EffectTriggered;

        public void Initialize(EffectContext context)
        {
            _byType.Clear();
            _reportedMissing.Clear();
            if (effects == null) return;

            foreach (EffectBase effect in effects)
            {
                if (effect == null) continue;
                try
                {
                    effect.Initialize(context);
                    _byType[effect.Type] = effect;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EffectManager] {effect.name} failed to initialize and is disabled: {e.Message}", effect);
                }
            }

            if (_byType.Count == 0) Debug.LogWarning("[EffectManager] No effects registered; triggers will be ignored.", this);
        }

        public bool Trigger(EffectType type, string source)
        {
            if (!_byType.TryGetValue(type, out EffectBase effect) || effect == null)
            {
                if (_reportedMissing.Add(type)) Debug.LogWarning($"[EffectManager] No effect registered for {type}; ignoring.", this);
                return false;
            }

            try
            {
                effect.Play();
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
            if (effects == null) return;
            foreach (EffectBase effect in effects) SafeClear(effect);
        }

        static void SafeClear(EffectBase effect)
        {
            if (effect == null) return;
            try { effect.Clear(); }
            catch (Exception e) { Debug.LogException(e, effect); }
        }
    }
}

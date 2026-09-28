using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>Base for every triggerable event (webhook gloves, boosts, hazards).</summary>
    public abstract class EffectBase : MonoBehaviour
    {
        public abstract EffectType Type { get; }

        protected EffectContext Context { get; private set; }

        public virtual void Initialize(EffectContext context) => Context = context;

        /// <summary>Start (or restart) the effect. Must be safe to call repeatedly.</summary>
        public abstract void Play();

        /// <summary>
        /// Start with a push distance chosen by the caller (e.g. a level encounter), in meters. Effects that push the
        /// climber use it instead of their own setting; the rest ignore it.
        /// </summary>
        public virtual void Play(float pushMeters) => Play();

        /// <summary>Stop immediately and return everything to its pool (retry, level exit).</summary>
        public abstract void Clear();

        protected void OnDestroy() => Clear();
    }
}

using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>
    /// One world-space particle system shared by every effect that needs this kind of burst
    /// (stars, flashes, fire). Emitting at a position avoids spawning a system per impact.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class ParticleBurst : MonoBehaviour
    {
        [SerializeField, Min(1)] int defaultCount = 8;

        ParticleSystem _system;

        void Awake() => _system = GetComponent<ParticleSystem>();

        public void Emit(Vector3 position, int count = -1)
        {
            var emitParams = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true,
            };
            _system.Emit(emitParams, count > 0 ? count : defaultCount);
        }

        public void Clear() => _system.Clear(true);
    }
}

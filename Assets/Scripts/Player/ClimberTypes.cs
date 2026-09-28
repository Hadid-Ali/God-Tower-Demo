using UnityEngine;

namespace GodTower.Player
{
    /// <summary>Describes how an attack affects the climber.</summary>
    public struct HitData
    {
        /// <summary>Height lost over the stagger, in meters.</summary>
        public float KnockbackMeters;
        /// <summary>How long control is overridden, in seconds.</summary>
        public float Stagger;
        /// <summary>If false the hit can never push the climber below the base (e.g. the webhook gloves).</summary>
        public bool Lethal;
        /// <summary>Knocks the climber off the tower into a fall they must recover from.</summary>
        public bool KnockOff;
        /// <summary>Initial downward speed for <see cref="KnockOff"/>, in m/s.</summary>
        public float FallSpeed;
        /// <summary>World direction the hit came from (for reactions).</summary>
        public Vector3 Direction;
        /// <summary>Slide down at constant speed over the whole stagger, instead of a fast slam that eases off.</summary>
        public bool Linear;

        public static HitData Knockback(float knockbackMeters, float stagger, bool lethal, Vector3 direction) => new HitData
        {
            KnockbackMeters = knockbackMeters,
            Stagger = stagger,
            Lethal = lethal,
            Direction = direction,
        };

        /// <summary>A steady push down: the climber slides <paramref name="meters"/> at constant speed over <paramref name="duration"/> seconds.</summary>
        public static HitData Push(float meters, float duration, bool lethal, Vector3 direction) => new HitData
        {
            KnockbackMeters = meters,
            Stagger = duration,
            Lethal = lethal,
            Direction = direction,
            Linear = true,
        };

        public static HitData KnockOffTower(float fallSpeed, Vector3 direction) => new HitData
        {
            KnockOff = true,
            Lethal = true,
            FallSpeed = fallSpeed,
            Stagger = 0f,
            Direction = direction,
        };
    }
}

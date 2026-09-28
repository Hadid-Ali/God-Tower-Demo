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

        public static HitData Knockback(float knockbackMeters, float stagger, bool lethal, Vector3 direction) => new HitData
        {
            KnockbackMeters = knockbackMeters,
            Stagger = stagger,
            Lethal = lethal,
            Direction = direction,
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

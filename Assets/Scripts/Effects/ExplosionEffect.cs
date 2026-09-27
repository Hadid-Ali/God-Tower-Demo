using System.Collections;
using GodTower.Audio;
using GodTower.Player;
using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>
    /// A bomb drops onto the climber and blasts them off the tower. The player recovers by tapping
    /// Climb to re-grab; failing to do so after leaving the safe zone is the level's loss condition.
    /// </summary>
    public sealed class ExplosionEffect : HazardEffect
    {
        [SerializeField] ParticleBurst fireBurst;
        [SerializeField] ParticleBurst smokeBurst;
        [SerializeField] ParticleBurst flashBurst;
        [SerializeField] float dropHeight = 14f;
        [SerializeField] float zoomDistance = 24f;
        [SerializeField] float zoomDuration = 2.2f;

        public override EffectType Type => EffectType.Explosion;

        protected override IEnumerator RunAttack()
        {
            yield return Telegraph(new Vector3(0f, 2.6f, -1.6f));

            var start = new Vector3(0.4f, dropHeight, -1.2f);
            var impact = new Vector3(0f, 0.6f, -0.6f);
            GameObject bomb = SpawnProjectile(Context.Climber.ChestPosition + start);
            yield return Home(bomb.transform, start, impact, flightTime, (t, dt) => t.Rotate(200f * dt, 0f, 90f * dt, Space.Self));

            Vector3 position = bomb.transform.position;
            DespawnProjectile(bomb);

            if (fireBurst != null) fireBurst.Emit(position, 45);
            if (smokeBurst != null) smokeBurst.Emit(position, 20);
            if (flashBurst != null) flashBurst.Emit(position, 2);
            Context.Screen?.Flash(new Color(1f, 0.6f, 0.2f), 0.7f, 0.4f);
            Context.Rig.Shake(1.1f, 0.6f, 24);
            Context.Rig.RequestZoom(zoomDistance, zoomDuration);
            AudioService.TryPlay(SfxId.Explosion);

            if (!Context.Climber.ApplyHit(HitData.KnockOffTower(Context.Config.explosionFallSpeed, Vector3.down)) && Context.View != null)
                Context.View.Jolt(Vector3.down);
        }
    }
}

using System.Collections;
using GodTower.Audio;
using GodTower.Player;
using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>A volley of spinning axes from one side: warning, impact, controlled setback.</summary>
    public sealed class AxeEffect : HazardEffect
    {
        [SerializeField] ParticleBurst sparkBurst;
        [SerializeField, Min(1)] int axesPerVolley = 3;
        [SerializeField] float volleyInterval = 0.16f;
        [SerializeField] float spinDegreesPerSecond = 1080f;
        [SerializeField] float stagger = 0.75f;
        [SerializeField] float startDistance = 10f;

        public override EffectType Type => EffectType.Axe;

        protected override IEnumerator RunAttack()
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            yield return Telegraph(new Vector3(side * 2.3f, 0.5f, -1.6f));

            for (int i = 0; i < axesPerVolley; i++)
            {
                StartCoroutine(ThrowAxe(side, i));
                yield return new WaitForSeconds(volleyInterval);
            }
        }

        IEnumerator ThrowAxe(float side, int index)
        {
            var start = new Vector3(side * startDistance, 1.2f + index * 0.5f, -1.2f);
            var impact = new Vector3(side * 0.25f, Random.Range(-0.3f, 0.4f), -0.45f);
            GameObject axe = SpawnProjectile(Context.Climber.ChestPosition + start);
            Transform axeTransform = axe.transform;
            float spin = -side * spinDegreesPerSecond;

            yield return Home(axeTransform, start, impact, flightTime, (t, dt) => t.Rotate(0f, 0f, spin * dt, Space.World));

            Vector3 direction = new Vector3(-side, 0f, 0f);
            if (sparkBurst != null) sparkBurst.Emit(axeTransform.position, 10);
            Context.Rig.Shake(0.45f, 0.28f);
            AudioHandler.TryPlay(SfxId.Impact, 1f, 0.1f);

            float knockback = Context.Config.ToMeters(Context.Config.axeKnockback);
            if (Context.Climber.ApplyHit(HitData.Knockback(knockback, stagger, lethal: true, direction)))
                Context.Screen?.Flash(new Color(1f, 0.3f, 0.2f), 0.35f, 0.2f);
            else if (Context.View != null)
                Context.View.Jolt(direction);

            // Bounce off and tumble away.
            Vector3 velocity = new Vector3(side * 5f, 4f, -2f);
            for (float t = 0f; t < 0.7f; t += Time.deltaTime)
            {
                velocity += Physics.gravity * Time.deltaTime;
                axeTransform.position += velocity * Time.deltaTime;
                axeTransform.Rotate(0f, 0f, spin * 0.6f * Time.deltaTime, Space.World);
                yield return null;
            }

            DespawnProjectile(axe);
        }
    }
}

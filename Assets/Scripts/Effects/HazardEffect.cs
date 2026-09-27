using System;
using System.Collections;
using System.Collections.Generic;
using GodTower.Audio;
using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>
    /// Shared flow for attacks: a readable warning that tracks the climber, then projectiles that home
    /// in on the climber's live position, then an impact. Subclasses define the choreography.
    /// </summary>
    public abstract class HazardEffect : EffectBase
    {
        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected GameObject warningPrefab;
        [SerializeField] protected float warningTime = 0.8f;
        [SerializeField] protected float flightTime = 0.5f;

        readonly List<GameObject> _live = new List<GameObject>();
        PrefabPool _projectiles;
        PrefabPool _warnings;

        public override void Initialize(EffectContext context)
        {
            base.Initialize(context);
            _projectiles ??= new PrefabPool(projectilePrefab, transform, prewarm: 3, maxSize: 16);
            if (warningPrefab != null) _warnings ??= new PrefabPool(warningPrefab, transform, prewarm: 1, maxSize: 4);
        }

        public override void Play()
        {
            if (Context == null) return;
            StartCoroutine(RunAttack());
        }

        protected abstract IEnumerator RunAttack();

        /// <summary>Shows a pulsing marker at an offset from the climber's chest for <see cref="warningTime"/>.</summary>
        protected IEnumerator Telegraph(Vector3 chestOffset)
        {
            AudioHandler.TryPlay(SfxId.Warning, 0.8f);
            GameObject marker = _warnings?.Get();
            if (marker != null) _live.Add(marker);

            for (float t = 0f; t < warningTime; t += Time.deltaTime)
            {
                if (marker != null) marker.transform.position = Context.Climber.ChestPosition + chestOffset;
                yield return null;
            }

            if (marker != null) Despawn(marker, _warnings);
        }

        protected GameObject SpawnProjectile(Vector3 position)
        {
            GameObject projectile = _projectiles.Get();
            projectile.transform.position = position;
            _live.Add(projectile);
            return projectile;
        }

        protected void DespawnProjectile(GameObject projectile) => Despawn(projectile, _projectiles);

        /// <summary>Moves from a point relative to the climber onto the climber's live chest position.</summary>
        protected IEnumerator Home(Transform projectile, Vector3 startOffset, Vector3 impactOffset, float duration, Action<Transform, float> animate = null)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                Vector3 chest = Context.Climber.ChestPosition;
                projectile.position = Vector3.Lerp(chest + startOffset, chest + impactOffset, k * k);
                animate?.Invoke(projectile, Time.deltaTime);
                yield return null;
            }
            projectile.position = Context.Climber.ChestPosition + impactOffset;
        }

        void Despawn(GameObject instance, PrefabPool pool)
        {
            _live.Remove(instance);
            pool.Release(instance);
        }

        public override void Clear()
        {
            StopAllCoroutines();
            foreach (GameObject instance in _live)
            {
                if (instance == null) continue;
                _projectiles?.Release(instance);
                _warnings?.Release(instance);
            }
            _live.Clear();
        }
    }
}

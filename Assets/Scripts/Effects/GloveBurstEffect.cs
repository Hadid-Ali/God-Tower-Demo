using System.Collections.Generic;
using DG.Tweening;
using GodTower.Audio;
using GodTower.Player;
using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>
    /// The required webhook event: a swarm of boxing gloves converges on the climber from every side
    /// with star bursts, flashes, screen shake and punch sounds. Each trigger launches a new wave;
    /// the number of gloves on screen is capped (oldest recycled first) so the scene always recovers.
    /// Only the first landed punch per wave costs height; the climber's recovery window absorbs the rest.
    /// </summary>
    public sealed class GloveBurstEffect : EffectBase
    {
        [SerializeField] GameObject glovePrefab;
        [SerializeField] ParticleBurst starBurst;
        [SerializeField] ParticleBurst flashBurst;

        [Header("Swarm")]
        [SerializeField, Min(6)] int glovesPerWave = 20;
        [SerializeField, Min(6)] int maxActiveGloves = 40;
        [SerializeField] float spawnRadius = 8f;
        [SerializeField, Tooltip("Gloves spread their launch over this window.")] float launchWindow = 0.85f;
        [SerializeField] float flightTime = 0.36f;
        [SerializeField] float reboundTime = 0.45f;
        [SerializeField, Tooltip("Random offset around the chest so gloves pile on the whole body.")] float targetSpread = 0.55f;

        [Header("Impact")]
        [SerializeField] float stagger = 0.9f;
        [SerializeField] float shakeStrength = 0.35f;
        [SerializeField] float zoomDistance = 16f;

        struct ActiveGlove
        {
            public GameObject Instance;
            public Sequence Sequence;
        }

        readonly List<ActiveGlove> _active = new List<ActiveGlove>();
        PrefabPool _pool;
        int _wave;
        int _damagedWave = -1;

        public override EffectType Type => EffectType.GloveBurst;

        public override void Initialize(EffectContext context)
        {
            base.Initialize(context);
            _pool ??= new PrefabPool(glovePrefab, transform, prewarm: glovesPerWave, maxSize: maxActiveGloves + glovesPerWave);
        }

        public override void Play()
        {
            if (Context == null) return;
            _wave++;

            Context.Rig.RequestZoom(zoomDistance, launchWindow + flightTime + 0.8f);
            Context.Screen?.Flash(Color.white, 0.25f, 0.15f);
            AudioHandler.TryPlay(SfxId.Whoosh, 0.8f, 0.1f);

            // Golden-angle distribution sends gloves in from every direction without clumping.
            float angleOffset = Random.value * 360f;
            for (int i = 0; i < glovesPerWave; i++)
            {
                float angle = angleOffset + i * 137.5f;
                float delay = launchWindow * i / glovesPerWave + Random.Range(0f, 0.06f);
                Launch(angle, delay, _wave);
            }
        }

        void Launch(float angleDegrees, float delay, int wave)
        {
            if (_active.Count >= maxActiveGloves) RecycleOldest();

            GameObject glove = _pool.Get();
            glove.SetActive(false);

            float radians = angleDegrees * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            // Pull the launch point toward the camera so gloves sweep across the frame, big and readable.
            Vector3 cameraward = Vector3.back * Random.Range(1.5f, 4f);
            Vector3 impactOffset = Random.insideUnitSphere * targetSpread;
            Vector3 baseScale = glove.transform.localScale;

            Sequence sequence = DOTween.Sequence().SetLink(glove);
            sequence.AppendInterval(delay);
            sequence.AppendCallback(() =>
            {
                glove.transform.position = Context.Climber.ChestPosition + direction * spawnRadius + cameraward;
                glove.transform.localScale = baseScale;
                glove.SetActive(true);
            });
            // Home on the climber's live position so knockback never makes gloves miss.
            sequence.Append(DOVirtual.Float(0f, 1f, flightTime, t =>
            {
                Vector3 start = Context.Climber.ChestPosition + direction * spawnRadius + cameraward;
                Vector3 end = Context.Climber.ChestPosition + impactOffset + Vector3.back * 0.5f;
                Vector3 position = Vector3.LerpUnclamped(start, end, t * t);
                glove.transform.rotation = Quaternion.LookRotation(end - start, Vector3.up);
                glove.transform.position = position;
            }));
            sequence.AppendCallback(() => Impact(glove.transform.position, -direction, wave));
            sequence.Append(glove.transform.DOBlendableMoveBy(direction * 2.2f + Vector3.down * 1.2f, reboundTime).SetEase(Ease.OutQuad));
            sequence.Join(glove.transform.DOScale(Vector3.zero, reboundTime).SetEase(Ease.InBack));
            sequence.Join(glove.transform.DOBlendableRotateBy(new Vector3(0f, 0f, 540f), reboundTime, RotateMode.LocalAxisAdd));
            sequence.OnComplete(() => Release(glove));

            _active.Add(new ActiveGlove { Instance = glove, Sequence = sequence });
        }

        void Impact(Vector3 position, Vector3 hitDirection, int wave)
        {
            if (starBurst != null) starBurst.Emit(position, 5);
            if (flashBurst != null) flashBurst.Emit(position, 1);
            Context.Rig.Shake(shakeStrength, 0.22f);
            AudioHandler.TryPlay(SfxId.Punch, 0.9f, 0.15f);

            bool damaged = false;
            if (_damagedWave != wave)
            {
                float knockback = Context.Config.ToMeters(Context.Config.gloveKnockback);
                // Non-lethal by contract: the webhook can never cause a loss.
                damaged = Context.Climber.ApplyHit(HitData.Knockback(knockback, stagger, lethal: false, hitDirection));
                if (damaged)
                {
                    _damagedWave = wave;
                    Context.Screen?.Flash(new Color(1f, 0.9f, 0.4f), 0.45f, 0.2f);
                }
            }

            if (!damaged && Context.View != null) Context.View.Jolt(hitDirection);
        }

        void RecycleOldest()
        {
            ActiveGlove oldest = _active[0];
            _active.RemoveAt(0);
            oldest.Sequence.Kill();
            _pool.Release(oldest.Instance);
        }

        void Release(GameObject glove)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Instance != glove) continue;
                _active.RemoveAt(i);
                break;
            }
            _pool.Release(glove);
        }

        public override void Clear()
        {
            foreach (ActiveGlove glove in _active)
            {
                glove.Sequence.Kill();
                if (glove.Instance != null) _pool?.Release(glove.Instance);
            }
            _active.Clear();
            if (starBurst != null) starBurst.Clear();
            if (flashBurst != null) flashBurst.Clear();
        }
    }
}

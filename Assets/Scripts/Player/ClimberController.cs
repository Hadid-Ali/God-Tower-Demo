using System;
using DG.Tweening;
using GodTower.Level;
using UnityEngine;

namespace GodTower.Player
{
    /// <summary>
    /// Kinematic tower climber with an explicit state flow: Idle, Climb, Hit, Fall, Win, Lose.
    /// Height is tracked at the hands (world y); the transform follows the tower surface.
    /// Gameplay effects talk to it through <see cref="ApplyHit"/> and <see cref="ApplyBoost"/>.
    /// </summary>
    public sealed class ClimberController : MonoBehaviour
    {
        [Header("Climbing")]
        [SerializeField, Tooltip("m/s² while speeding up.")] float acceleration = 9f;
        [SerializeField, Tooltip("m/s² while letting go.")] float deceleration = 16f;
        [SerializeField, Tooltip("Hand height above the transform pivot (feet).")] float handOffset = 2.05f;
        [SerializeField, Tooltip("Radius of the tower column, in meters.")] float towerRadius = 1f;
        [SerializeField, Tooltip("Gap between the tower surface and the body centre.")] float surfaceGap = 0.42f;

        [Header("Hits and falls")]
        [SerializeField, Tooltip("Share of the stagger spent sliding down.")] float knockbackPortion = 0.55f;
        [SerializeField] float recoveryInvulnerability = 1.1f;
        [SerializeField] float gravity = 20f;
        [SerializeField] float maxFallSpeed = 26f;
        [SerializeField, Tooltip("Seconds after being knocked off before a tap can re-grab the tower.")] float regrabDelay = 0.4f;
        [SerializeField] float regrabStagger = 0.3f;
        [SerializeField, Tooltip("How far below the start point the body keeps falling after a loss (visual only).")] float loseFallDepth = 14f;

        public event Action<ClimberState> StateChanged;
        public event Action<HitData> HitTaken;
        public event Action Regrabbed;
        public event Action SummitReached;
        public event Action FellBelowBase;
        public event Action<string> BoostStarted;
        public event Action BoostEnded;

        public ClimberState State { get; private set; } = ClimberState.Idle;
        public float HeightMeters => _handY;
        public float HeightUnits => _config != null ? _config.ToUnits(Mathf.Max(0f, _handY)) : 0f;
        public float GoalUnits => _config != null ? _config.goalHeight : 1f;
        public float Progress01 => _config != null ? Mathf.Clamp01(_handY / _config.GoalMeters) : 0f;
        public float VerticalSpeed { get; private set; }
        public bool ControlEnabled { get; set; }
        public bool IsBoosting => _boostTimer > 0f;
        public float BoostRemaining01 => _boostTotal > 0f ? Mathf.Clamp01(_boostTimer / _boostTotal) : 0f;
        public string BoostLabel { get; private set; }
        public bool CanRegrab => State == ClimberState.Fall && _stateTimer >= regrabDelay && ControlEnabled;
        public bool IsInvulnerable => _invulnerableTimer > 0f || State == ClimberState.Hit || State == ClimberState.Fall;
        public bool PassedSafeZone { get; private set; }

        IClimbInput _input;
        LevelConfig _config;
        float _handY;
        float _floorY; // Start height: the climber never drops below it except when losing.
        float _climbVelocity;
        float _stateTimer;
        float _invulnerableTimer;

        float _hitStartY;
        float _hitTargetY;
        float _hitDuration;

        float _fallVelocity;

        float _boostTimer;
        float _boostTotal;
        float _boostSpeed;

        public void Configure(LevelConfig config, IClimbInput input)
        {
            _config = config;
            _input = input;
            _floorY = config.StartMeters;
            _handY = _floorY;
            _climbVelocity = 0f;
            PassedSafeZone = false;
            SetState(ClimberState.Idle);
            PlaceOnTower();
        }

        void Update()
        {
            if (_config == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            _stateTimer += dt;
            if (_invulnerableTimer > 0f) _invulnerableTimer -= dt;
            TickBoost(dt);

            float previousY = _handY;
            switch (State)
            {
                case ClimberState.Idle:
                case ClimberState.Climb:
                    TickClimb(dt);
                    break;
                case ClimberState.Hit:
                    TickHit();
                    break;
                case ClimberState.Fall:
                    TickFall(dt);
                    break;
                case ClimberState.Lose:
                    TickLoseFall(dt);
                    break;
                case ClimberState.Win:
                    return; // The summit tween owns the transform.
            }

            VerticalSpeed = (_handY - previousY) / dt;
            if (_handY >= _floorY + _config.SafeZoneMeters) PassedSafeZone = true;

            if (State != ClimberState.Lose && _handY >= _config.GoalMeters)
            {
                _handY = _config.GoalMeters;
                SetState(ClimberState.Win);
                SummitReached?.Invoke();
            }

            PlaceOnTower();
        }

        void TickClimb(float dt)
        {
            float target = 0f;
            if (IsBoosting) target = Mathf.Max(_boostSpeed, _config.climbSpeed);
            else if (ControlEnabled && _input != null && _input.IsHeld) target = _config.climbSpeed;

            float rate = target > _climbVelocity ? acceleration : deceleration;
            if (IsBoosting) rate *= 2f;
            _climbVelocity = Mathf.MoveTowards(_climbVelocity, target, rate * dt);
            _handY += _climbVelocity * dt;

            SetState(_climbVelocity > 0.05f ? ClimberState.Climb : ClimberState.Idle);
        }

        void TickHit()
        {
            float t = _hitDuration > 0f ? Mathf.Clamp01(_stateTimer / (_hitDuration * knockbackPortion)) : 1f;
            _handY = Mathf.Lerp(_hitStartY, _hitTargetY, 1f - Mathf.Pow(1f - t, 3f));

            if (_stateTimer < _hitDuration) return;
            _invulnerableTimer = recoveryInvulnerability;
            SetState(ClimberState.Idle);
        }

        void TickFall(float dt)
        {
            if (IsBoosting)
            {
                // A boost (e.g. the phoenix) catches a falling climber.
                Regrab();
                return;
            }

            _fallVelocity = Mathf.Max(_fallVelocity - gravity * dt, -maxFallSpeed);
            _handY += _fallVelocity * dt;

            if (CanRegrab && _input != null && _input.PressedThisFrame)
            {
                Regrab();
                return;
            }

            if (_handY > _floorY) return;

            if (PassedSafeZone)
            {
                SetState(ClimberState.Lose);
                FellBelowBase?.Invoke();
            }
            else
            {
                _handY = _floorY;
                _invulnerableTimer = recoveryInvulnerability;
                SetState(ClimberState.Idle);
            }
        }

        void TickLoseFall(float dt)
        {
            float bottom = _floorY - loseFallDepth;
            if (_handY <= bottom) return;
            _fallVelocity = Mathf.Max(_fallVelocity - gravity * dt, -maxFallSpeed);
            _handY = Mathf.Max(_handY + _fallVelocity * dt, bottom);
        }

        void Regrab()
        {
            _fallVelocity = 0f;
            _climbVelocity = 0f;
            BeginStagger(_handY, Mathf.Max(_floorY, _handY), regrabStagger);
            Regrabbed?.Invoke();
        }

        /// <summary>Applies an attack. Returns false if it was ignored (recovering, finished, or already reeling).</summary>
        public bool ApplyHit(HitData hit)
        {
            if (_config == null || State == ClimberState.Win || State == ClimberState.Lose) return false;
            if (IsInvulnerable) return false;

            HitTaken?.Invoke(hit);
            _climbVelocity = 0f;

            if (hit.KnockOff)
            {
                StartFall(hit.FallSpeed);
                return true;
            }

            float target = _handY - hit.KnockbackMeters;
            if (target < _floorY)
            {
                if (hit.Lethal && PassedSafeZone)
                {
                    StartFall(4f);
                    return true;
                }
                target = _floorY;
            }

            BeginStagger(_handY, target, hit.Stagger);
            return true;
        }

        /// <summary>
        /// Temporary upward boost. Re-triggering refreshes the timer up to <paramref name="maxDuration"/>;
        /// speed takes the strongest active boost and never stacks multiplicatively.
        /// </summary>
        public void ApplyBoost(string label, float duration, float speed, float maxDuration)
        {
            if (_config == null || State == ClimberState.Win || State == ClimberState.Lose) return;

            bool wasBoosting = IsBoosting;
            _boostTimer = Mathf.Min(Mathf.Max(_boostTimer, 0f) + duration, maxDuration);
            _boostTotal = Mathf.Max(_boostTimer, wasBoosting ? _boostTotal : 0f);
            _boostSpeed = wasBoosting ? Mathf.Max(_boostSpeed, speed) : speed;
            BoostLabel = label;
            BoostStarted?.Invoke(label);
        }

        public void CancelBoost()
        {
            if (!IsBoosting) return;
            _boostTimer = 0f;
            EndBoost();
        }

        void TickBoost(float dt)
        {
            if (_boostTimer <= 0f) return;
            _boostTimer -= dt;
            if (_boostTimer <= 0f) EndBoost();
        }

        void EndBoost()
        {
            _boostTimer = 0f;
            _boostTotal = 0f;
            _boostSpeed = 0f;
            BoostLabel = null;
            BoostEnded?.Invoke();
        }

        /// <summary>Pull up onto the summit platform. Called by the level flow after <see cref="SummitReached"/>.</summary>
        public Tween PlaySummitClimb(Vector3 standPoint)
        {
            CancelBoost();
            transform.DOKill();
            return transform.DOJump(standPoint, 1.4f, 1, 0.9f).SetEase(Ease.OutQuad).SetLink(gameObject);
        }

        void BeginStagger(float fromY, float toY, float duration)
        {
            _hitStartY = fromY;
            _hitTargetY = toY;
            _hitDuration = Mathf.Max(0.05f, duration);
            _stateTimer = 0f;
            State = ClimberState.Hit; // Re-entering Hit restarts the stagger even from Hit.
            StateChanged?.Invoke(State);
        }

        void StartFall(float initialSpeed)
        {
            _fallVelocity = -Mathf.Abs(initialSpeed);
            SetState(ClimberState.Fall);
        }

        void SetState(ClimberState next)
        {
            if (State == next) return;
            State = next;
            _stateTimer = 0f;
            StateChanged?.Invoke(next);
        }

        /// <summary>The climber hangs on the camera-facing side of the tower (-Z).</summary>
        void PlaceOnTower()
        {
            transform.SetPositionAndRotation(
                new Vector3(0f, _handY - handOffset, -(towerRadius + surfaceGap)),
                Quaternion.identity);
        }

        /// <summary>World position of the climber's chest; effects aim here.</summary>
        public Vector3 ChestPosition => transform.position + Vector3.up * (handOffset * 0.62f);
    }
}

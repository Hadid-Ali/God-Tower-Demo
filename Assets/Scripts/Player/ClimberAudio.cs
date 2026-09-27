using GodTower.Audio;
using UnityEngine;

namespace GodTower.Player
{
    /// <summary>Climbing and recovery sounds: a scuff every hand-over-hand grip, a whoosh when knocked off, a thud on re-grab.</summary>
    public sealed class ClimberAudio : MonoBehaviour
    {
        [SerializeField] ClimberController climber;
        [SerializeField, Tooltip("Meters climbed between grip sounds.")] float stepDistance = 0.65f;

        float _distanceSinceStep;

        void OnEnable()
        {
            climber.StateChanged += OnStateChanged;
            climber.Regrabbed += OnRegrabbed;
        }

        void OnDisable()
        {
            climber.StateChanged -= OnStateChanged;
            climber.Regrabbed -= OnRegrabbed;
        }

        void Update()
        {
            if (climber.State != ClimberState.Climb || climber.IsBoosting) return;
            _distanceSinceStep += Mathf.Max(0f, climber.VerticalSpeed) * Time.deltaTime;
            if (_distanceSinceStep < stepDistance) return;
            _distanceSinceStep = 0f;
            AudioService.TryPlay(SfxId.ClimbStep, 0.6f, 0.2f);
        }

        static void OnStateChanged(ClimberState state)
        {
            if (state == ClimberState.Fall) AudioService.TryPlay(SfxId.Whoosh, 1f, 0.05f);
        }

        static void OnRegrabbed() => AudioService.TryPlay(SfxId.Grab);
    }
}

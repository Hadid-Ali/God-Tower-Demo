using System.Collections;
using DG.Tweening;
using GodTower.Audio;
using GodTower.Cameras;
using GodTower.Effects;
using GodTower.Level;
using GodTower.Player;
using GodTower.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GodTower.Core
{
    /// <summary>
    /// Owns the flow of one level: Intro → Playing ⇄ Paused → Won | Lost. Builds the level from the
    /// selected <see cref="LevelConfig"/>, wires the climber, camera, effects and UI together, routes
    /// webhook bumps, and makes sure every exit path (retry, menu, next level) leaves nothing running.
    /// Effects and UI (<see cref="GameHudController"/>) are optional so the scene can be played while they are still being built.
    /// </summary>
    public sealed class LevelController : MonoBehaviour
    {
        const string WebhookSource = "Webhook";
        const string TowerSource = "Tower";

        [Header("World")]
        [SerializeField] LevelBuilder builder;
        [SerializeField] ClimberController climber;
        [SerializeField] ClimberView climberView;
        [SerializeField] ClimbInput input;
        [SerializeField] CameraRig cameraRig;
        [SerializeField] EffectManager effects;

        [Header("UI")]
        [SerializeField, Tooltip("Optional. Without it the level plays with no HUD and restarts itself on win/lose.")] GameHudController ui;

        [Header("Flow")]
        [SerializeField, Tooltip("Test mode: play this scene on its own with the fallback config. No GameSession is created, " +
                                 "so there is no webhook listener, level catalog, menu flow or session audio. Retry and exit reload this scene.")]
        bool standalone;
        [SerializeField, Tooltip("Used in standalone mode, or when the scene is played directly without a session selection.")] LevelConfig fallbackConfig;
        [SerializeField] float introDuration = 1.3f;
        [SerializeField] float resultDelay = 1.6f;

        GameSession _session;
        LevelConfig _config;
        EncounterRunner _encounters;
        float _bestHeight;

        public LevelFlowState Flow { get; private set; } = LevelFlowState.Intro;

        /// <summary>Read by <see cref="GameSession"/> before the scene starts to skip creating the session.</summary>
        public bool Standalone => standalone;

        void Start()
        {
            _session = standalone ? null : GameSession.Instance;
            _config = _session != null && _session.SelectedLevel != null ? _session.SelectedLevel : fallbackConfig;
            if (_config == null)
            {
                Debug.LogError("[LevelController] No level config available.");
                enabled = false;
                return;
            }
            if (standalone) Debug.Log($"[LevelController] Standalone mode: playing '{_config.displayName}' without a session.");

            builder.Build(_config);
            climber.Configure(_config, input);
            cameraRig.SetTarget(climber.transform);
            _encounters = new EncounterRunner(_config.encounters, _config.goalHeight);
            _encounters.SkipUpTo(climber.HeightUnits);
            _bestHeight = climber.HeightUnits;
            if (effects != null) effects.Initialize(new EffectContext(climber, climberView, cameraRig, _config));

            climber.SummitReached += OnSummitReached;
            climber.FellBelowBase += OnFellBelowBase;
            if (ui != null)
            {
                ui.Bind(_config, climber, effects);
                ui.PauseToggleRequested += TogglePause;
                ui.NextLevelRequested += NextLevel;
                ui.QuitRequested += QuitGame;
            }
            if (_session != null) _session.BumpReceived += OnBump;

            StartCoroutine(Intro());
        }

        IEnumerator Intro()
        {
            SetFlow(LevelFlowState.Intro);
            yield return new WaitForSeconds(introDuration);
            SetFlow(LevelFlowState.Playing);
        }

        void Update()
        {
            if (WasBackPressed()) TogglePause();

            if (Flow != LevelFlowState.Playing) return;
            _bestHeight = Mathf.Max(_bestHeight, climber.HeightUnits);
            _encounters.Tick(climber.HeightUnits, TriggerEncounter);
        }

        void TriggerEncounter(Encounter encounter)
        {
            if (effects == null) return;
            float pushMeters = _config.GoalMeters * encounter.pushPercent / 100f;
            effects.Trigger(encounter.effect, TowerSource, pushMeters);
        }

        static bool WasBackPressed()
        {
            // Escape doubles as the Android back button.
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame);
        }

        void OnBump()
        {
            // The listener already filtered on AcceptingBumps, but state may have changed since.
            if (Flow == LevelFlowState.Playing && effects != null) effects.Trigger(EffectType.GloveBurst, WebhookSource);
        }

        void OnSummitReached()
        {
            SetFlow(LevelFlowState.Won);
            if (effects != null) effects.ClearAll();
            cameraRig.ClearEffects();
            cameraRig.RequestZoom(15f, 3f);
            climber.PlaySummitClimb(builder.SummitStandPoint);
            if (_session != null) SaveData.MarkCompleted(_session.SelectedLevelIndex);
            AudioHandler.TryPlay(SfxId.Win);
            StartCoroutine(EndLevelAfterDelay(won: true));
        }

        void OnFellBelowBase()
        {
            SetFlow(LevelFlowState.Lost);
            AudioHandler.TryPlay(SfxId.Lose);
            StartCoroutine(EndLevelAfterDelay(won: false));
        }

        /// <summary>A win shows the level complete menu; a loss (or a win with no menu) restarts the level.</summary>
        IEnumerator EndLevelAfterDelay(bool won)
        {
            yield return new WaitForSeconds(resultDelay);
            if (effects != null) effects.ClearAll();

            if (won && ui != null && ui.TryShowLevelComplete()) yield break;

            Debug.Log($"[LevelController] {(won ? "Won" : "Lost")} (best {Mathf.FloorToInt(_bestHeight)}): restarting.");
            Retry();
        }

        public void TogglePause()
        {
            if (Flow == LevelFlowState.Playing) Pause();
            else if (Flow == LevelFlowState.Paused) Resume();
        }

        public void Pause()
        {
            if (Flow != LevelFlowState.Playing) return;
            SetFlow(LevelFlowState.Paused);
            Time.timeScale = 0f;
            input.ReleaseAll();
            AudioHandler.TryPlay(SfxId.Click);
        }

        public void Resume()
        {
            if (Flow != LevelFlowState.Paused) return;
            Time.timeScale = 1f;
            SetFlow(LevelFlowState.Playing);
            AudioHandler.TryPlay(SfxId.Click);
        }

        void Retry() => Leave(() =>
        {
            if (_session != null) _session.RestartLevel();
            else ReloadThisScene();
        });

        /// <summary>Continue: the next level in the catalog, or this level again when there is none (or no session).</summary>
        void NextLevel() => Leave(() =>
        {
            if (_session == null) ReloadThisScene();
            else if (_session.HasNextLevel) _session.PlayNextLevel();
            else _session.RestartLevel();
        });

        void QuitGame() => Leave(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });

        void Leave(System.Action load)
        {
            AudioHandler.TryPlay(SfxId.Click);
            Cleanup();
            load();
        }

        /// <summary>Without a session there is no catalog to go to, so every exit replays this scene.</summary>
        static void ReloadThisScene()
        {
            Scene scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // Works even when the scene is not in Build Settings.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.buildIndex);
#endif
        }

        /// <summary>Stops everything the level started so nothing leaks into the next scene.</summary>
        void Cleanup()
        {
            StopAllCoroutines();
            SetFlow(LevelFlowState.Lost);
            if (effects != null) effects.ClearAll();
            cameraRig.ClearEffects();
            if (ui != null) ui.ClearTransient();
            input.ReleaseAll();
            DOTween.KillAll();
            Time.timeScale = 1f;
        }

        void SetFlow(LevelFlowState next)
        {
            Flow = next;
            climber.ControlEnabled = next == LevelFlowState.Playing;
            if (_session != null && _session.Server != null) _session.Server.AcceptingBumps = next == LevelFlowState.Playing;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Pause();
        }

        void OnDestroy()
        {
            if (_session != null)
            {
                _session.BumpReceived -= OnBump;
                if (_session.Server != null) _session.Server.AcceptingBumps = false;
            }
            if (ui != null)
            {
                ui.PauseToggleRequested -= TogglePause;
                ui.NextLevelRequested -= NextLevel;
                ui.QuitRequested -= QuitGame;
            }
            if (climber != null)
            {
                climber.SummitReached -= OnSummitReached;
                climber.FellBelowBase -= OnFellBelowBase;
            }
            Time.timeScale = 1f;
        }
    }
}

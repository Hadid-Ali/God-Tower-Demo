using GodTower.Audio;
using GodTower.Core;
using GodTower.Level;
using GodTower.Net;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>Main menu flow: Home → Level Select / Settings. All five levels are always selectable.</summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] CanvasGroup homePanel;
        [SerializeField] CanvasGroup levelSelectPanel;
        [SerializeField] CanvasGroup settingsPanel;

        [Header("Home")]
        [SerializeField] Button playButton;
        [SerializeField] Button levelsButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button quitButton;
        [SerializeField] TMP_Text webhookHint;

        [Header("Level select")]
        [SerializeField] RectTransform levelList;
        [SerializeField] LevelSelectButton levelButtonPrefab;
        [SerializeField] Button levelBackButton;

        [Header("Settings")]
        [SerializeField] Slider masterSlider;
        [SerializeField] Slider sfxSlider;
        [SerializeField] Toggle reducedShakeToggle;
        [SerializeField] Button settingsBackButton;

        CanvasGroup _current;

        void Start()
        {
            Time.timeScale = 1f;
            playButton.onClick.AddListener(PlayFirstUnfinished);
            levelsButton.onClick.AddListener(() => Open(levelSelectPanel));
            settingsButton.onClick.AddListener(() => Open(settingsPanel));
            quitButton.onClick.AddListener(Application.Quit);
            quitButton.gameObject.SetActive(!Application.isEditor && Application.platform != RuntimePlatform.WebGLPlayer);
            levelBackButton.onClick.AddListener(() => Open(homePanel));
            settingsBackButton.onClick.AddListener(() => Open(homePanel));

            masterSlider.SetValueWithoutNotify(SaveData.MasterVolume);
            sfxSlider.SetValueWithoutNotify(SaveData.SfxVolume);
            reducedShakeToggle.SetIsOnWithoutNotify(SaveData.ReducedShake);
            masterSlider.onValueChanged.AddListener(OnMasterVolume);
            sfxSlider.onValueChanged.AddListener(v => SaveData.SfxVolume = v);
            reducedShakeToggle.onValueChanged.AddListener(v => SaveData.ReducedShake = v);

            BuildLevelList();
            ShowWebhookHint();

            UITween.SetVisible(levelSelectPanel, false);
            UITween.SetVisible(settingsPanel, false);
            UITween.SetVisible(homePanel, false);
            Open(homePanel, playSound: false);
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (_current != homePanel) Open(homePanel);
        }

        void BuildLevelList()
        {
            LevelCatalog catalog = GameSession.Instance != null ? GameSession.Instance.Catalog : null;
            if (catalog == null) return;

            for (int i = levelList.childCount - 1; i >= 0; i--) Destroy(levelList.GetChild(i).gameObject);
            for (int i = 0; i < catalog.Count; i++)
            {
                LevelConfig config = catalog.Get(i);
                int index = i;
                LevelSelectButton button = Instantiate(levelButtonPrefab, levelList);
                button.Setup(config.number, config.displayName, Mathf.RoundToInt(config.goalHeight), SaveData.IsCompleted(i), () => StartLevel(index));
            }
        }

        void ShowWebhookHint()
        {
            string ip = NetworkInfo.GetLocalIPv4();
            string host = Application.isMobilePlatform && ip != null ? ip : "localhost";
            webhookHint.text = $"Webhook: GET/POST http://{host}:{BumpServer.DefaultPort}/bump";
        }

        void PlayFirstUnfinished()
        {
            LevelCatalog catalog = GameSession.Instance != null ? GameSession.Instance.Catalog : null;
            int index = 0;
            if (catalog != null)
            {
                while (index < catalog.Count - 1 && SaveData.IsCompleted(index)) index++;
            }
            StartLevel(index);
        }

        void StartLevel(int index)
        {
            AudioHandler.TryPlay(SfxId.Click);
            GameSession.Instance?.PlayLevel(index);
        }

        void OnMasterVolume(float value)
        {
            SaveData.MasterVolume = value;
            AudioHandler.ApplyMasterVolume();
        }

        void Open(CanvasGroup panel, bool playSound = true)
        {
            if (playSound) AudioHandler.TryPlay(SfxId.Click);
            if (_current != null && _current != panel) UITween.HidePanel(_current);
            _current = panel;
            UITween.ShowPanel(panel);
        }
    }
}

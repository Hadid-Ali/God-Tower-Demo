using System;
using GodTower.Level;
using GodTower.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GodTower.Core
{
    /// <summary>
    /// Persistent root that survives scene loads. Owns the webhook listener, the main-thread
    /// dispatcher, and which level is selected. Audio lives in the scene's AudioHandler. Created automatically before the first scene.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        public const string CatalogResourcePath = "LevelCatalog";

        public static GameSession Instance { get; private set; }

        /// <summary>Raised on the main thread for every accepted /bump request.</summary>
        public event Action BumpReceived;

        public LevelCatalog Catalog { get; private set; }
        public int SelectedLevelIndex { get; private set; }
        public MainThreadDispatcher Dispatcher { get; private set; }
        public BumpServer Server { get; private set; }

        public LevelConfig SelectedLevel => Catalog != null ? Catalog.Get(SelectedLevelIndex) : null;
        public bool HasNextLevel => Catalog != null && SelectedLevelIndex + 1 < Catalog.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;

            // The first scene's objects are loaded (not yet awake) at this point: a standalone level runs without a session.
            var level = FindAnyObjectByType<LevelController>(FindObjectsInactive.Include);
            if (level != null && level.Standalone)
            {
                Debug.Log("[GameSession] Skipped: the scene's LevelController is in standalone mode.");
                return;
            }

            var go = new GameObject("[GameSession]");
            DontDestroyOnLoad(go);
            go.AddComponent<GameSession>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Catalog = Resources.Load<LevelCatalog>(CatalogResourcePath);
            if (Catalog == null) Debug.LogError($"[GameSession] Missing Resources/{CatalogResourcePath}. Run Tools/God Tower/Build Project.");

            Dispatcher = gameObject.AddComponent<MainThreadDispatcher>();

            Server = new BumpServer(BumpServer.DefaultPort);
            Server.BumpAccepted += OnBumpAcceptedOffThread;
            Server.Start();
        }

        // Called on the listener's worker thread: never touch Unity objects here.
        void OnBumpAcceptedOffThread() => Dispatcher.Enqueue(RaiseBump);

        void RaiseBump() => BumpReceived?.Invoke();

        public void SelectLevel(int index)
        {
            if (Catalog == null) return;
            SelectedLevelIndex = Mathf.Clamp(index, 0, Catalog.Count - 1);
        }

        public void PlayLevel(int index)
        {
            SelectLevel(index);
            LoadScene(SceneNames.Game);
        }

        public void PlayNextLevel()
        {
            if (HasNextLevel) PlayLevel(SelectedLevelIndex + 1);
            else LoadMenu();
        }

        public void RestartLevel() => LoadScene(SceneNames.Game);

        public void LoadMenu() => LoadScene(SceneNames.MainMenu);

        static void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        void OnApplicationQuit() => ShutdownServer();

        void OnDestroy()
        {
            if (Instance != this) return;
            ShutdownServer();
            Instance = null;
        }

        void ShutdownServer()
        {
            if (Server == null) return;
            Server.BumpAccepted -= OnBumpAcceptedOffThread;
            Server.Dispose();
            Server = null;
        }
    }
}

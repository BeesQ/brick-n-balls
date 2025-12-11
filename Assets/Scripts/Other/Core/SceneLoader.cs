using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class SceneLoader : MonoBehaviour {
    public static SceneLoader Instance { get; private set; }

    private const string GameSceneName = "GameScene";

    public event Action OnGameSceneLoaded;
    public event Action OnGameSceneUnloaded;

    private bool isGameSceneLoaded = false;

    public bool IsGameSceneLoaded => isGameSceneLoaded;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        LoadGameScene();
    }

    #region Public Methods
    public void LoadGameScene() {
        if (isGameSceneLoaded)
            return;

        SceneManager.LoadSceneAsync(GameSceneName, LoadSceneMode.Additive)
            .completed += _ => {
                isGameSceneLoaded = true;
                OnGameSceneLoaded?.Invoke();
            };
    }

    public void UnloadGameScene() {
        if (!isGameSceneLoaded)
            return;

        SceneManager.UnloadSceneAsync(GameSceneName)
            .completed += _ => {
                isGameSceneLoaded = false;
                OnGameSceneUnloaded?.Invoke();
            };
    }

    public void ReloadGameScene(Action onComplete = null) {
        if (!isGameSceneLoaded) {
            LoadGameScene();
            return;
        }

        SceneManager.UnloadSceneAsync(GameSceneName)
            .completed += _ => {
                isGameSceneLoaded = false;
                OnGameSceneUnloaded?.Invoke();

                SceneManager.LoadSceneAsync(GameSceneName, LoadSceneMode.Additive)
                    .completed += _ => {
                        isGameSceneLoaded = true;
                        OnGameSceneLoaded?.Invoke();
                        onComplete?.Invoke();
                    };
            };
    }
    #endregion Public Methods
}
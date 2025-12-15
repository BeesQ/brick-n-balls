using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class SceneLoader : MonoBehaviour {
    public static SceneLoader Instance { get; private set; }

    private const string GameSceneName = "GameScene";
    private const string AudioSceneName = "AudioScene";

    public event Action OnGameSceneLoaded;
    public event Action OnGameSceneUnloaded;
    public event Action OnAudioSceneLoaded;

    private bool isGameSceneLoaded = false;
    private bool isAudioSceneLoaded = false;

    public bool IsGameSceneLoaded => isGameSceneLoaded;
    public bool IsAudioSceneLoaded => isAudioSceneLoaded;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        LoadAudioScene();
        LoadGameScene();
    }

    #region Audio Scene
    public void LoadAudioScene() {
        if (isAudioSceneLoaded)
            return;

        SceneManager.LoadSceneAsync(AudioSceneName, LoadSceneMode.Additive)
            .completed += _ => {
                isAudioSceneLoaded = true;
                OnAudioSceneLoaded?.Invoke();
            };
    }
    #endregion Audio Scene

    #region Game Scene
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
    #endregion Game Scene
}
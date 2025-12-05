using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class SceneLoader : MonoBehaviour {
    public static SceneLoader Instance { get; private set; }

    private const string GameSceneName = "GameScene";

    public event Action OnGameSceneLoaded;
    public event Action OnGameSceneUnloaded;

    private bool isGameSceneLoaded = false;

    #region Public Methods
    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

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

    public bool IsGameSceneLoaded => isGameSceneLoaded; 
    #endregion
}
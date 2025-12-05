using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour {
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text ballsRemainingText;

    private void Start() {
        ShowMainMenu();
    }

    #region Public Methods
    public void ShowMainMenu() {
        mainMenuPanel.SetActive(true);
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }

    public void ShowGameHUD() {
        mainMenuPanel.SetActive(false);
        hudPanel.SetActive(true);
        gameOverPanel.SetActive(false);
    }

    public void ShowGameOver(int finalScore) {
        mainMenuPanel.SetActive(false);
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        finalScoreText.text = $"Score: {finalScore}";
    }

    public void UpdateScore(int score) {
        scoreText.text = $"Score: {score}";
    }

    public void UpdateBallsRemaining(int balls) {
        ballsRemainingText.text = $"Balls: {balls}";
    } 
    #endregion

    #region Button callbacks
    public void OnStartGameClicked() {
        SceneLoader.Instance.LoadGameScene();
        ShowGameHUD();
    }

    public void OnGoBackToMenuClicked() {
        SceneLoader.Instance.UnloadGameScene();
        ShowMainMenu();
    } 
    #endregion
}
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour {
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text ballsRemainingText;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable() {
        GameEvents.OnScoreChanged += UpdateScore;
        GameEvents.OnBallsRemainingChanged += UpdateBallsRemaining;
        GameEvents.OnGameOver += ShowGameOver;
        GameEvents.OnGameStarted += OnGameStarted;
    }

    private void OnDisable() {
        GameEvents.OnScoreChanged -= UpdateScore;
        GameEvents.OnBallsRemainingChanged -= UpdateBallsRemaining;
        GameEvents.OnGameOver -= ShowGameOver;
        GameEvents.OnGameStarted -= OnGameStarted;
    }

    private void Start() {
        ShowMainMenu();
    }

    #region Panel Management
    public void ShowMainMenu() {
        SetPanelState(mainMenu: true, hud: false, gameOver: false);
    }

    public void ShowGameHUD() {
        SetPanelState(mainMenu: false, hud: true, gameOver: false);

        UpdateScore(0);
        UpdateBallsRemaining(GameManager.Instance?.StartingBalls ?? 5);
    }

    public void ShowGameOver(int finalScore) {
        SetPanelState(mainMenu: false, hud: false, gameOver: true);

        if (finalScoreText != null) {
            finalScoreText.text = $"Score: {finalScore}";
        }
    }

    private void SetPanelState(bool mainMenu, bool hud, bool gameOver) {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(mainMenu);

        if (hudPanel != null)
            hudPanel.SetActive(hud);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(gameOver);
    }
    #endregion

    #region UI Updates
    public void UpdateScore(int score) {
        if (scoreText != null) {
            scoreText.text = $"Score: {score}";
        }
    }

    public void UpdateBallsRemaining(int balls) {
        if (ballsRemainingText != null) {
            ballsRemainingText.text = $"Balls: {balls}";
        }
    }
    #endregion

    #region Event Handlers
    private void OnGameStarted() { }
    #endregion

    #region Button Callbacks
    public void OnStartGameClicked() {
        ShowGameHUD();
        GameManager.Instance?.ResetGame();
    }

    public void OnGoBackToMenuClicked() {
        ShowMainMenu();
    }
    #endregion
}
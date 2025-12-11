using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int startingBalls = 10;

    private int ballsRemaining;
    private int activeBallsInPlay;
    private int currentScore;
    private bool isGameOver;
    private bool isGameActive;

    #region Properties
    public int BallsRemaining => ballsRemaining;
    public int Score => currentScore;
    public bool IsGameOver => isGameOver;
    public bool IsGameActive => isGameActive;
    #endregion

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable() {
        GameEvents.OnAllBricksDestroyed += OnAllBricksDestroyed;
    }

    private void OnDisable() {
        GameEvents.OnAllBricksDestroyed -= OnAllBricksDestroyed;
    }

    private void Start() {
        isGameActive = false;
        isGameOver = true;
    }

    public void StartNewGame() {
        ballsRemaining = startingBalls;
        activeBallsInPlay = 0;
        currentScore = 0;
        isGameOver = false;
        isGameActive = true;

        GameEvents.BallsRemainingChanged(ballsRemaining);
        GameEvents.ScoreChanged(currentScore);
        GameEvents.GameStarted();

        Debug.Log($"Game Started -> Balls: {ballsRemaining}");
    }

    public bool CanShoot() {
        return isGameActive && ballsRemaining > 0 && !isGameOver;
    }

    public void OnBallShot() {
        if (isGameOver || !isGameActive)
            return;

        ballsRemaining--;
        activeBallsInPlay++;

        GameEvents.BallsRemainingChanged(ballsRemaining);
        GameEvents.BallShot();

        Debug.Log($"Ball Shot -> Remaining: {ballsRemaining}, In Play: {activeBallsInPlay}");
    }

    public void OnBallDestroyed() {
        if (isGameOver || !isGameActive)
            return;

        activeBallsInPlay--;

        GameEvents.BallDestroyed();

        Debug.Log($"Ball Destroyed -> Remaining: {ballsRemaining}, In Play: {activeBallsInPlay}");

        if (activeBallsInPlay <= 0 && ballsRemaining <= 0) {
            TriggerGameOver();
        }
    }

    public void AddScore(int points) {
        if (isGameOver || !isGameActive)
            return;

        currentScore += points;
        GameEvents.ScoreChanged(currentScore);

        Debug.Log($"Score: {currentScore}");
    }

    private void OnAllBricksDestroyed() {
        Debug.Log("Victory!");
        TriggerGameOver();
    }

    private void TriggerGameOver() {
        if (isGameOver)
            return;

        isGameOver = true;
        isGameActive = false;

        Debug.Log($"=== GAME OVER === Final Score: {currentScore}");

        GameEvents.GameOver(currentScore);
    }

    public void ResetGame() {
        BrickSpawner.Instance?.ResetBricks();
        StartNewGame();
    }

    public void StopGame() {
        isGameActive = false;
        isGameOver = true;
    }
}
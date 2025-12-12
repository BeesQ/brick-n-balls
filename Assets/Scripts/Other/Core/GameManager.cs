using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    [Header("Ball Settings")]
    [SerializeField] private int startingBalls = 5;
    [SerializeField] private float ballSpeed = 15f;

    [Header("Brick Settings")]
    [SerializeField] private int brickRows = 3;
    [SerializeField] private int brickColumns = 8;
    [SerializeField] private int brickStartingHealth = 3;

    [Header("Grid Layout")]
    [SerializeField] private float brickPadding = 0.1f;
    [SerializeField] private float brickTopOffset = 0.5f;
    [SerializeField] private float brickSideMargin = 0.3f;

    private int ballsRemaining;
    private int activeBallsInPlay;
    private int currentScore;
    private bool isGameOver;
    private bool isGameActive;

    #region Config Properties
    public int StartingBalls => startingBalls;
    public float BallSpeed => ballSpeed;
    public int BrickRows => brickRows;
    public int BrickColumns => brickColumns;
    public int BrickStartingHealth => brickStartingHealth;
    public float BrickPadding => brickPadding;
    public float BrickTopOffset => brickTopOffset;
    public float BrickSideMargin => brickSideMargin;
    #endregion

    #region Runtime Properties
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
        GameEvents.OnBallShot += HandleBallShot;
        GameEvents.OnBallDestroyed += HandleBallDestroyed;
        GameEvents.OnBrickHit += HandleBrickHit;
        GameEvents.OnAllBricksDestroyed += HandleAllBricksDestroyed;
    }

    private void OnDisable() {
        GameEvents.OnBallShot -= HandleBallShot;
        GameEvents.OnBallDestroyed -= HandleBallDestroyed;
        GameEvents.OnBrickHit -= HandleBrickHit;
        GameEvents.OnAllBricksDestroyed -= HandleAllBricksDestroyed;
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

    private void HandleBallShot() {
        if (isGameOver || !isGameActive)
            return;

        ballsRemaining--;
        activeBallsInPlay++;

        GameEvents.BallsRemainingChanged(ballsRemaining);

        Debug.Log($"Ball Shot -> Remaining: {ballsRemaining}, In Play: {activeBallsInPlay}");
    }

    private void HandleBallDestroyed() {
        if (isGameOver || !isGameActive)
            return;

        activeBallsInPlay--;

        Debug.Log($"Ball Destroyed -> Remaining: {ballsRemaining}, In Play: {activeBallsInPlay}");

        if (activeBallsInPlay <= 0 && ballsRemaining <= 0) {
            TriggerGameOver();
        }
    }

    private void HandleBrickHit(int brickId, int remainingHealth) {
        if (isGameOver || !isGameActive)
            return;

        currentScore += Consts.Scoring.PointsPerBrickHit;
        GameEvents.ScoreChanged(currentScore);

        Debug.Log($"Score: {currentScore}");
    }

    private void HandleAllBricksDestroyed() {
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
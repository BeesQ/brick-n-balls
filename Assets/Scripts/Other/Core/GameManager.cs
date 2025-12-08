using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int startingBalls = 5;

    private int ballsRemaining;
    private int activeBallsInPlay;
    private int currentScore;
    private bool isGameOver;

    #region Properties
    public int BallsRemaining => ballsRemaining;
    public int Score => currentScore;
    public bool IsGameOver => isGameOver;
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
        StartNewGame();
    }

    public void StartNewGame() {
        ballsRemaining = startingBalls;
        activeBallsInPlay = 0;
        currentScore = 0;
        isGameOver = false;

        GameEvents.BallsRemainingChanged(ballsRemaining);
        GameEvents.ScoreChanged(currentScore);
        GameEvents.GameStarted();

        Debug.Log($"Game Started -> Balls: {ballsRemaining}");
    }

    public bool CanShoot() {
        return ballsRemaining > 0 && !isGameOver;
    }

    public void OnBallShot() {
        if (isGameOver)
            return;

        ballsRemaining--;
        activeBallsInPlay++;

        GameEvents.BallsRemainingChanged(ballsRemaining);
        GameEvents.BallShot();

        Debug.Log($"Ball Shot -> Remaining: {ballsRemaining}, In Play: {activeBallsInPlay}");
    }

    public void OnBallDestroyed() {
        if (isGameOver)
            return;

        activeBallsInPlay--;

        GameEvents.BallDestroyed();

        Debug.Log($"Ball Destroyed -> Remaining: {ballsRemaining}, In Play: {activeBallsInPlay}");

        if (activeBallsInPlay <= 0 && ballsRemaining <= 0) {
            TriggerGameOver();
        }
    }

    public void AddScore(int points) {
        if (isGameOver)
            return;

        currentScore += points;

        // Note: GameEvents.ScoreChanged is called from BrickView.TakeDamage
        // to ensure it fires after score update

        Debug.Log($"Score: {currentScore}");
    }

    private void OnAllBricksDestroyed() {
        Debug.Log("Victory!");
    }

    private void TriggerGameOver() {
        if (isGameOver)
            return;

        isGameOver = true;

        Debug.Log($"=== GAME OVER === Final Score: {currentScore}");

        GameEvents.GameOver(currentScore);
    }

    public void ResetGame() {
        BrickSpawner.Instance?.ResetBricks();

        StartNewGame();
    }
}
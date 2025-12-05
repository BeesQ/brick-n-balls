using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int startingBalls = 5;

    private int ballsRemaining;
    private int activeBallsInPlay;
    private int currentScore;

    public int BallsRemaining => ballsRemaining;
    public int Score => currentScore;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start() {
        StartNewGame();
    }

    public void StartNewGame() {
        ballsRemaining = startingBalls;
        activeBallsInPlay = 0;
        currentScore = 0;

        Debug.Log($"Start -> Balls: {ballsRemaining}");
    }

    public bool CanShoot() {
        return ballsRemaining > 0;
    }

    public void OnBallShot() {
        ballsRemaining--;
        activeBallsInPlay++;

        Debug.Log($"Shot -> Remaining: {ballsRemaining}, in play: {activeBallsInPlay}");
    }

    public void OnBallDestroyed() {
        activeBallsInPlay--;

        Debug.Log($"Ball destroyed -> Remaining: {ballsRemaining}, in play: {activeBallsInPlay}");

        if (activeBallsInPlay <= 0 && ballsRemaining <= 0) {
            GameOver();
        }
    }

    public void AddScore(int points) {
        currentScore += points;
        Debug.Log($"Score: {currentScore}");
    }

    private void GameOver() {
        Debug.Log($"=== GAME OVER === -> Final Score: {currentScore}");
        // UIManager.Instance?.ShowGameOver(currentScore);
    }
}
using System;
using UnityEngine;

public static class GameEvents {
    #region Score Events
    public static event Action<int> OnScoreChanged;

    public static void ScoreChanged(int newScore) {
        OnScoreChanged?.Invoke(newScore);
    }
    #endregion Score Events

    #region Ball Events
    public static event Action<int> OnBallsRemainingChanged;

    public static void BallsRemainingChanged(int remaining) {
        OnBallsRemainingChanged?.Invoke(remaining);
    }

    public static event Action OnBallShot;

    public static void BallShot() {
        OnBallShot?.Invoke();
    }

    public static event Action OnBallDestroyed;

    public static void BallDestroyed() {
        OnBallDestroyed?.Invoke();
    }
    #endregion Ball Events

    #region Brick Events
    public static event Action<int, int, Vector3> OnBrickHit;

    public static void BrickHit(int brickId, int previousHealth, Vector3 position) {
        OnBrickHit?.Invoke(brickId, previousHealth, position);
    }

    public static event Action<int> OnBrickDestroyed;

    public static void BrickDestroyed(int brickId) {
        OnBrickDestroyed?.Invoke(brickId);
    }

    public static event Action OnAllBricksDestroyed;

    public static void AllBricksDestroyed() {
        OnAllBricksDestroyed?.Invoke();
    }
    #endregion Brick Events

    #region Wall Events
    public static event Action<Vector3> OnWallHit;

    public static void WallHit(Vector3 position) {
        OnWallHit?.Invoke(position);
    }
    #endregion Wall Events

    #region UI Events
    public static event Action OnButtonClicked;

    public static void ButtonClicked() {
        OnButtonClicked?.Invoke();
    }
    #endregion UI Events

    #region Game State Events
    public static event Action OnGameStarted;

    public static void GameStarted() {
        OnGameStarted?.Invoke();
    }

    public static event Action<int> OnGameOver;

    public static void GameOver(int finalScore) {
        OnGameOver?.Invoke(finalScore);
    }
    #endregion Game State Events

    #region Cleanup
    public static void ClearAllEvents() {
        OnScoreChanged = null;
        OnBallsRemainingChanged = null;
        OnBallShot = null;
        OnBallDestroyed = null;
        OnBrickHit = null;
        OnBrickDestroyed = null;
        OnAllBricksDestroyed = null;
        OnWallHit = null;
        OnButtonClicked = null;
        OnGameStarted = null;
        OnGameOver = null;
    }
    #endregion Cleanup
}
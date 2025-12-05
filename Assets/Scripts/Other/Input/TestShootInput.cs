using UnityEngine;
using UnityEngine.InputSystem;

public class TestShootInput : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Camera mainCamera;

    [Header("Spawn Point (should match BallSpawner)")]
    [SerializeField] private Vector2 shootOrigin = new Vector2(0f, -5f);

    private Mouse mouse;

    private void Start() {
        if (mainCamera == null) {
            mainCamera = Camera.main;
        }

        mouse = Mouse.current;
    }

    private void Update() {
        if (mouse == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame) {
            TryShoot();
        }
    }

    private void TryShoot() {
        if (!GameManager.Instance.CanShoot()) {
            Debug.Log("No balls remaining");
            return;
        }

        Vector2 direction = GetShootDirection();

        // Only allow shooting upward
        if (direction.y <= 0.1f) {
            Debug.Log("Must shoot upward");
            return;
        }

        BallSpawner.Instance.SpawnBall(direction);
        GameManager.Instance.OnBallShot();
    }

    private Vector2 GetShootDirection() {
        Vector2 mouseScreenPos = mouse.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f)
        );
        mouseWorldPos.z = 0f;

        Vector2 direction = new Vector2(
            mouseWorldPos.x - shootOrigin.x,
            mouseWorldPos.y - shootOrigin.y
        ).normalized;

        return direction;
    }
}
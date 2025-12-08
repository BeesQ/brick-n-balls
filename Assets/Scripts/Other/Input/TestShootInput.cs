using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TestShootInput : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Camera gameCamera;

    [Header("Spawn Point")]
    [SerializeField] private float spawnOffsetFromBottom = 1f;

    private Mouse mouse;
    private Vector2 shootOrigin;

    private void Start() {
        mouse = Mouse.current;

        if (gameCamera == null) {
            gameCamera = FindGameSceneCamera();
        }

        CalculateShootOrigin();
    }

    private Camera FindGameSceneCamera() {
        Scene gameScene = SceneManager.GetSceneByName("GameScene");

        if (!gameScene.IsValid()) {
            return Camera.main;
        }

        foreach (GameObject rootObj in gameScene.GetRootGameObjects()) {
            Camera cam = rootObj.GetComponentInChildren<Camera>();
            if (cam != null) {
                return cam;
            }
        }

        return Camera.main;
    }

    private void CalculateShootOrigin() {
        float bottomY = gameCamera.transform.position.y - gameCamera.orthographicSize;

        shootOrigin = new Vector2(
            gameCamera.transform.position.x,
            bottomY + spawnOffsetFromBottom
        );

        Debug.Log($"Shoot origin: {shootOrigin}");
    }

    private void Update() {
        if (mouse == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame) {
            if (IsPointerOverUI())
                return;

            TryShoot();
        }
    }

    private bool IsPointerOverUI() {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void TryShoot() {
        if (!GameManager.Instance.CanShoot()) {
            Debug.Log("No balls remaining");
            return;
        }

        Vector2 direction = GetShootDirection();

        if (direction.y <= 0.1f) {
            Debug.Log("Must shoot upward");
            return;
        }

        BallSpawner.Instance.SpawnBall(direction);
        GameManager.Instance.OnBallShot();
    }

    private Vector2 GetShootDirection() {
        Vector2 mouseScreenPos = mouse.position.ReadValue();
        Vector3 mouseWorldPos = gameCamera.ScreenToWorldPoint(
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
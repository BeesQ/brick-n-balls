using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ShootInput : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Transform spawnPoint;

    [Header("Rotation Settings")]
    [SerializeField] private float minAngle = 10f;
    [SerializeField] private float maxAngle = 170f;

    private Mouse mouse;
    private Vector2 currentAimDirection;

    private void Start() {
        mouse = Mouse.current;

        if (gameCamera == null) {
            gameCamera = FindGameSceneCamera();
        }

        currentAimDirection = Vector2.up;
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

    private void Update() {
        if (mouse == null)
            return;

        UpdateCannonRotation();

        if (mouse.leftButton.wasPressedThisFrame) {
            if (IsPointerOverUI())
                return;

            TryShoot();
        }
    }

    private void UpdateCannonRotation() {
        Vector2 mouseScreenPos = mouse.position.ReadValue();
        Vector3 mouseWorldPos = gameCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f)
        );
        mouseWorldPos.z = 0f;

        Vector3 pivotPos = transform.position;
        Vector2 rawDirection = new Vector2(
            mouseWorldPos.x - pivotPos.x,
            mouseWorldPos.y - pivotPos.y
        );

        if (rawDirection.sqrMagnitude < 0.01f)
            return;

        float angle = Mathf.Atan2(rawDirection.y, rawDirection.x) * Mathf.Rad2Deg;

        if (angle < 0f)
            angle += 360f;

        if (angle < minAngle || angle > maxAngle) {
            if (rawDirection.x < 0f) {
                angle = maxAngle;
            }
            else {
                angle = minAngle;
            }
        }

        float angleRad = angle * Mathf.Deg2Rad;
        currentAimDirection = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
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

        Vector2 spawnPosition = GetSpawnPosition();
        BallSpawner.Instance.SpawnBall(spawnPosition, currentAimDirection);
        GameEvents.BallShot();
    }

    private Vector2 GetSpawnPosition() {
        if (spawnPoint != null) {
            return new Vector2(spawnPoint.position.x, spawnPoint.position.y);
        }

        return new Vector2(transform.position.x, transform.position.y);
    }
}
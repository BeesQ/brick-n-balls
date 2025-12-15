using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BallView : MonoBehaviour {
    private Entity linkedEntity;
    private EntityManager entityManager;
    private bool isInitialized = false;

    private Camera gameCamera;
    private float ballRadius;
    private float destroyBelowY;

    private bool IsWorldValid =>
        World.DefaultGameObjectInjectionWorld != null &&
        World.DefaultGameObjectInjectionWorld.IsCreated;

    public void Initialize(Entity entity) {
        linkedEntity = entity;
        if (IsWorldValid) {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        gameCamera = FindGameSceneCamera();
        CalculateBallRadius();
        CalculateDestroyThreshold();

        isInitialized = true;
    }

    private Camera FindGameSceneCamera() {
        Scene scene = SceneManager.GetSceneByName("GameScene");

        if (scene.IsValid()) {
            foreach (GameObject rootObj in scene.GetRootGameObjects()) {
                Camera cam = rootObj.GetComponentInChildren<Camera>();
                if (cam != null) {
                    return cam;
                }
            }
        }

        return Camera.main;
    }

    private void CalculateBallRadius() {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null) {
            ballRadius = sr.bounds.extents.y;
        }
        else {
            ballRadius = transform.localScale.y * 0.5f;
        }
    }

    private void CalculateDestroyThreshold() {
        if (gameCamera == null) {
            destroyBelowY = -10f;
            return;
        }

        float screenBottom = gameCamera.transform.position.y - gameCamera.orthographicSize;
        float onePixelInWorld = (gameCamera.orthographicSize * 2f) / Screen.height;

        destroyBelowY = screenBottom - ballRadius - onePixelInWorld;
    }

    private void Update() {
        if (!isInitialized)
            return;

        if (!IsWorldValid) {
            return;
        }

        if (!entityManager.Exists(linkedEntity)) {
            Destroy(gameObject);
            return;
        }

        SyncPositionFromECS();
        CheckOutOfBounds();
    }

    private void SyncPositionFromECS() {
        if (entityManager.HasComponent<LocalTransform>(linkedEntity)) {
            LocalTransform entityTransform = entityManager.GetComponentData<LocalTransform>(linkedEntity);
            transform.position = new Vector3(
                entityTransform.Position.x,
                entityTransform.Position.y,
                0f
            );
        }
    }

    private void CheckOutOfBounds() {
        if (transform.position.y < destroyBelowY) {
            DestroyBall();
        }
    }

    private void DestroyBall() {
        if (IsWorldValid && entityManager.Exists(linkedEntity)) {
            entityManager.DestroyEntity(linkedEntity);
        }

        GameEvents.BallDestroyed();
        Destroy(gameObject);
    }
}
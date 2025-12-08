using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class BallView : MonoBehaviour {
    [Header("Boundaries")]
    [SerializeField] private float destroyBelowY = -6f;

    private Entity linkedEntity;
    private EntityManager entityManager;
    private bool isInitialized = false;

    private bool IsWorldValid =>
        World.DefaultGameObjectInjectionWorld != null &&
        World.DefaultGameObjectInjectionWorld.IsCreated;

    public void Initialize(Entity entity) {
        linkedEntity = entity;
        if (IsWorldValid) {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
        isInitialized = true;
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

        GameManager.Instance?.OnBallDestroyed();
        Destroy(gameObject);
    }
}
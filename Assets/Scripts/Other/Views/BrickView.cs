using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class BrickView : MonoBehaviour, IDamageable {
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Entity linkedEntity;
    private EntityManager entityManager;
    private bool isInitialized = false;

    private int brickId;
    private int currentHealth;
    private int maxHealth;

    #region Properties
    public int BrickId => brickId;
    public int CurrentHealth => currentHealth;
    public bool IsAlive => currentHealth > 0;
    #endregion

    private bool IsWorldValid =>
        World.DefaultGameObjectInjectionWorld != null &&
        World.DefaultGameObjectInjectionWorld.IsCreated;

    public void Initialize(Entity entity, int id, int health) {
        linkedEntity = entity;
        brickId = id;
        maxHealth = health;
        currentHealth = health;

        if (IsWorldValid) {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
        isInitialized = true;

        if (spriteRenderer == null) {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        UpdateVisual();
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

    public void TakeDamage(int damage) {
        if (!IsAlive)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        GameManager.Instance?.AddScore(Consts.Scoring.PointsPerBrickHit);

        if (IsAlive) {
            UpdateVisual();
            GameEvents.BrickHit(brickId, currentHealth);
        }
        else {
            DestroyBrick();
        }
    }

    private void UpdateVisual() {
        if (spriteRenderer != null) {
            spriteRenderer.color = Consts.GetColorForHealth(currentHealth);
        }
    }

    private void DestroyBrick() {
        GameEvents.BrickDestroyed(brickId);

        if (IsWorldValid && entityManager.Exists(linkedEntity)) {
            entityManager.DestroyEntity(linkedEntity);
        }

        BrickSpawner.Instance?.OnBrickDestroyed(this);

        Destroy(gameObject);
    }

    public void SetSize(float size) {
        SetSize(new Vector2(size, size));
    }

    public void SetSize(Vector2 size) {
        if (spriteRenderer != null && spriteRenderer.sprite != null) {
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            transform.localScale = new Vector3(
                size.x / spriteSize.x,
                size.y / spriteSize.y,
                1f
            );
        }
        else {
            transform.localScale = new Vector3(size.x, size.y, 1f);
        }
    }
}
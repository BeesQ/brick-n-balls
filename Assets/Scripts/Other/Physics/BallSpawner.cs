using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BallSpawner : MonoBehaviour {
    public static BallSpawner Instance { get; private set; }

    [Header("Ball Settings")]
    [SerializeField] private float ballSpeed = 12f;
    [SerializeField] private float ballMass = 1f;
    [SerializeField] private float restitution = 1f;

    [Header("Prefab")]
    [SerializeField] private GameObject ballVisualPrefab;

    private EntityManager entityManager;
    private int ballIdCounter = 0;
    private Scene gameScene;
    private float ballRadius;

    private bool IsWorldValid =>
        World.DefaultGameObjectInjectionWorld != null &&
        World.DefaultGameObjectInjectionWorld.IsCreated;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CalculateBallRadius();
    }

    private void Start() {
        if (!IsWorldValid) {
            Debug.LogError("BallSpawner: ECS World not available");
            return;
        }

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        gameScene = SceneManager.GetSceneByName("GameScene");
    }

    private void CalculateBallRadius() {
        if (ballVisualPrefab == null) {
            ballRadius = 0.3f;
            Debug.LogWarning("BallSpawner: No prefab assigned, using default radius");
            return;
        }

        SpriteRenderer sr = ballVisualPrefab.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null) {
            Vector3 prefabScale = ballVisualPrefab.transform.localScale;
            Vector2 spriteExtents = sr.sprite.bounds.extents;
            ballRadius = Mathf.Max(spriteExtents.x * prefabScale.x, spriteExtents.y * prefabScale.y);
            Debug.Log($"BallSpawner: Calculated radius from sprite: {ballRadius}");
            return;
        }

        MeshRenderer mr = ballVisualPrefab.GetComponent<MeshRenderer>();
        if (mr != null) {
            ballRadius = mr.bounds.extents.x;
            Debug.Log($"BallSpawner: Calculated radius from mesh: {ballRadius}");
            return;
        }

        ballRadius = 0.3f;
        Debug.LogWarning("BallSpawner: Could not determine radius, using default");
    }

    public void SpawnBall(Vector2 spawnPosition, Vector2 direction) {
        if (!IsWorldValid) {
            Debug.LogError("BallSpawner: Cannot spawn - ECS World not available");
            return;
        }

        float3 position = new float3(spawnPosition.x, spawnPosition.y, 0f);
        float3 normalizedDir = math.normalize(new float3(direction.x, direction.y, 0f));

        Entity ballEntity = CreateBallPhysicsEntity(position, normalizedDir);
        CreateBallVisual(ballEntity, position);

        ballIdCounter++;
    }

    private Entity CreateBallPhysicsEntity(float3 position, float3 direction) {
        Entity entity = entityManager.CreateEntity();

        entityManager.AddComponentData(entity, new LocalTransform {
            Position = position,
            Rotation = quaternion.identity,
            Scale = 1f
        });

        var material = new Unity.Physics.Material {
            Friction = 0f,
            Restitution = restitution,
            CollisionResponse = CollisionResponsePolicy.CollideRaiseCollisionEvents,
            FrictionCombinePolicy = Unity.Physics.Material.CombinePolicy.Minimum,
            RestitutionCombinePolicy = Unity.Physics.Material.CombinePolicy.Maximum
        };

        BlobAssetReference<Unity.Physics.Collider> sphereCollider = Unity.Physics.SphereCollider.Create(
            new SphereGeometry {
                Center = float3.zero,
                Radius = ballRadius
            },
            PhysicsLayers.BallFilter,
            material
        );

        entityManager.AddComponentData(entity, new PhysicsCollider {
            Value = sphereCollider
        });

        float3 velocity = direction * ballSpeed;
        entityManager.AddComponentData(entity, new PhysicsVelocity {
            Linear = velocity,
            Angular = float3.zero
        });

        entityManager.AddComponentData(entity, PhysicsMass.CreateDynamic(
            sphereCollider.Value.MassProperties,
            ballMass
        ));

        entityManager.AddComponentData(entity, new PhysicsDamping {
            Linear = 0f,
            Angular = 0f
        });

        entityManager.AddComponentData(entity, new PhysicsGravityFactor {
            Value = 0f
        });

        entityManager.AddComponentData(entity, new BallPhysicsComponent {
            BallId = ballIdCounter
        });

        entityManager.AddSharedComponent(entity, new PhysicsWorldIndex {
            Value = 0
        });

        return entity;
    }

    private void CreateBallVisual(Entity entity, float3 position) {
        GameObject visualGO = Instantiate(
            ballVisualPrefab,
            new Vector3(position.x, position.y, 0f),
            Quaternion.identity
        );

        if (gameScene.IsValid()) {
            SceneManager.MoveGameObjectToScene(visualGO, gameScene);
        }

        BallView ballView = visualGO.GetComponent<BallView>();
        if (ballView == null) {
            ballView = visualGO.AddComponent<BallView>();
        }

        ballView.Initialize(entity);
    }
}
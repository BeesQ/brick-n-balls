using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public class WallSetup : MonoBehaviour {
    [Header("Play Area Size")]
    [SerializeField] private float areaWidth = 10f;
    [SerializeField] private float areaHeight = 12f;
    [SerializeField] private float wallThickness = 1f;

    [Header("Position Offset")]
    [SerializeField] private Vector2 areaCenter = Vector2.zero;

    private EntityManager entityManager;

    private void Start() {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        CreateWalls();
    }

    private void CreateWalls() {
        float halfWidth = areaWidth / 2f;
        float halfHeight = areaHeight / 2f;

        // Left wall
        CreateWallEntity(
            new float3(areaCenter.x - halfWidth - wallThickness / 2f, areaCenter.y, 0f),
            new float3(wallThickness, areaHeight, 1f)
        );

        // Right wall
        CreateWallEntity(
            new float3(areaCenter.x + halfWidth + wallThickness / 2f, areaCenter.y, 0f),
            new float3(wallThickness, areaHeight, 1f)
        );

        // Top wall
        CreateWallEntity(
            new float3(areaCenter.x, areaCenter.y + halfHeight + wallThickness / 2f, 0f),
            new float3(areaWidth + wallThickness * 2f, wallThickness, 1f)
        );
    }

    private void CreateWallEntity(float3 position, float3 size) {
        Entity entity = entityManager.CreateEntity();

        // Transform
        entityManager.AddComponentData(entity, new LocalTransform {
            Position = position,
            Rotation = quaternion.identity,
            Scale = 1f
        });

        // Physics material - perfect bounce
        var material = new Unity.Physics.Material {
            Friction = 0f,
            Restitution = 1f,
            CollisionResponse = CollisionResponsePolicy.Collide,
            FrictionCombinePolicy = Unity.Physics.Material.CombinePolicy.Minimum,
            RestitutionCombinePolicy = Unity.Physics.Material.CombinePolicy.Maximum
        };

        // Box collider
        BlobAssetReference<Unity.Physics.Collider> boxCollider = Unity.Physics.BoxCollider.Create(
            new BoxGeometry {
                Center = float3.zero,
                Size = size,
                Orientation = quaternion.identity,
                BevelRadius = 0f
            },
            CollisionFilter.Default,
            material
        );

        entityManager.AddComponentData(entity, new PhysicsCollider {
            Value = boxCollider
        });

        entityManager.AddSharedComponent(entity, new PhysicsWorldIndex {
            Value = 0
        });
    }

    // Debug visualization in editor
    private void OnDrawGizmos() {
        float halfWidth = areaWidth / 2f;
        float halfHeight = areaHeight / 2f;

        Gizmos.color = Color.green;

        // Left wall
        Gizmos.DrawWireCube(
            new Vector3(areaCenter.x - halfWidth - wallThickness / 2f, areaCenter.y, 0f),
            new Vector3(wallThickness, areaHeight, 0.1f)
        );

        // Right wall
        Gizmos.DrawWireCube(
            new Vector3(areaCenter.x + halfWidth + wallThickness / 2f, areaCenter.y, 0f),
            new Vector3(wallThickness, areaHeight, 0.1f)
        );

        // Top wall
        Gizmos.DrawWireCube(
            new Vector3(areaCenter.x, areaCenter.y + halfHeight + wallThickness / 2f, 0f),
            new Vector3(areaWidth + wallThickness * 2f, wallThickness, 0.1f)
        );

        // Bottom boundary (red - no wall)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            new Vector3(areaCenter.x - halfWidth, areaCenter.y - halfHeight, 0f),
            new Vector3(areaCenter.x + halfWidth, areaCenter.y - halfHeight, 0f)
        );
    }
}
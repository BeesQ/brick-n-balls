using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class WallSetup : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Camera gameCamera;

    [Header("Wall Settings")]
    [SerializeField] private float wallThickness = 1f;

    private EntityManager entityManager;

    private int lastScreenWidth;
    private int lastScreenHeight;
    private float lastAspect;

    private List<Entity> wallEntities = new List<Entity>();

    public float ScreenHalfWidth { get; private set; }
    public float ScreenHalfHeight { get; private set; }
    public Vector2 ScreenCenter { get; private set; }
    public float BottomY => ScreenCenter.y - ScreenHalfHeight;

    public event System.Action OnBoundsChanged;

    private bool IsWorldValid =>
        World.DefaultGameObjectInjectionWorld != null &&
        World.DefaultGameObjectInjectionWorld.IsCreated;

    private void Start() {
        if (!IsWorldValid) {
            Debug.LogError("WallSetup: ECS World not available");
            return;
        }

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (gameCamera == null) {
            gameCamera = FindGameSceneCamera();
        }

        if (gameCamera == null) {
            Debug.LogError("WallSetup: No camera found");
            return;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastAspect = gameCamera.aspect;

        CalculateScreenBounds();
        CreateWalls();
    }

    private void Update() {
        if (HasScreenSizeChanged()) {
            OnScreenSizeChanged();
        }
    }

    private bool HasScreenSizeChanged() {
        bool changed = Screen.width != lastScreenWidth ||
                       Screen.height != lastScreenHeight ||
                       !Mathf.Approximately(gameCamera.aspect, lastAspect);

        if (changed) {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastAspect = gameCamera.aspect;
        }

        return changed;
    }

    private void OnScreenSizeChanged() {
        Debug.Log($"Screen size changed: {Screen.width}x{Screen.height}");

        DestroyWalls();

        CalculateScreenBounds();
        CreateWalls();

        OnBoundsChanged?.Invoke();
    }

    private void DestroyWalls() {
        if (IsWorldValid) {
            foreach (Entity entity in wallEntities) {
                if (entityManager.Exists(entity)) {
                    entityManager.DestroyEntity(entity);
                }
            }
        }

        wallEntities.Clear();
    }

    private Camera FindGameSceneCamera() {
        Scene gameScene = SceneManager.GetSceneByName("GameScene");

        if (!gameScene.IsValid()) {
            Debug.LogWarning("GameScene not found, using Camera.main");
            return Camera.main;
        }

        foreach (GameObject rootObj in gameScene.GetRootGameObjects()) {
            Camera cam = rootObj.GetComponentInChildren<Camera>();
            if (cam != null) {
                return cam;
            }
        }

        Debug.LogWarning("No camera in GameScene, using Camera.main");
        return Camera.main;
    }

    private void CalculateScreenBounds() {
        ScreenHalfHeight = gameCamera.orthographicSize;
        ScreenHalfWidth = ScreenHalfHeight * gameCamera.aspect;

        ScreenCenter = new Vector2(
            gameCamera.transform.position.x,
            gameCamera.transform.position.y
        );

        Debug.Log($"Screen bounds: Width={ScreenHalfWidth * 2:F2}, Height={ScreenHalfHeight * 2:F2}");
    }

    private void CreateWalls() {
        if (!IsWorldValid) {
            Debug.LogError("WallSetup: Cannot create walls - ECS World not available");
            return;
        }

        Entity leftWall = CreateWallEntity(
            new float3(ScreenCenter.x - ScreenHalfWidth - wallThickness / 2f, ScreenCenter.y, 0f),
            new float3(wallThickness, ScreenHalfHeight * 2f + wallThickness * 2f, 1f)
        );
        wallEntities.Add(leftWall);

        Entity rightWall = CreateWallEntity(
            new float3(ScreenCenter.x + ScreenHalfWidth + wallThickness / 2f, ScreenCenter.y, 0f),
            new float3(wallThickness, ScreenHalfHeight * 2f + wallThickness * 2f, 1f)
        );
        wallEntities.Add(rightWall);

        Entity topWall = CreateWallEntity(
            new float3(ScreenCenter.x, ScreenCenter.y + ScreenHalfHeight + wallThickness / 2f, 0f),
            new float3(ScreenHalfWidth * 2f + wallThickness * 2f, wallThickness, 1f)
        );
        wallEntities.Add(topWall);
    }

    private Entity CreateWallEntity(float3 position, float3 size) {
        Entity entity = entityManager.CreateEntity();

        entityManager.AddComponentData(entity, new LocalTransform {
            Position = position,
            Rotation = quaternion.identity,
            Scale = 1f
        });

        var material = new Unity.Physics.Material {
            Friction = 0f,
            Restitution = 1f,
            CollisionResponse = CollisionResponsePolicy.Collide,
            FrictionCombinePolicy = Unity.Physics.Material.CombinePolicy.Minimum,
            RestitutionCombinePolicy = Unity.Physics.Material.CombinePolicy.Maximum
        };

        BlobAssetReference<Unity.Physics.Collider> boxCollider = Unity.Physics.BoxCollider.Create(
            new BoxGeometry {
                Center = float3.zero,
                Size = size,
                Orientation = quaternion.identity,
                BevelRadius = 0f
            },
            PhysicsLayers.WallFilter,
            material
        );

        entityManager.AddComponentData(entity, new PhysicsCollider {
            Value = boxCollider
        });

        entityManager.AddSharedComponent(entity, new PhysicsWorldIndex {
            Value = 0
        });

        return entity;
    }

    private void OnDestroy() {
        DestroyWalls();
    }

    private void OnDrawGizmos() {
        Camera cam = gameCamera != null ? gameCamera : Camera.main;
        if (cam == null) return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector2 center = new Vector2(cam.transform.position.x, cam.transform.position.y);

        // Visible screen (white)
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(
            new Vector3(center.x, center.y, 0f),
            new Vector3(halfWidth * 2f, halfHeight * 2f, 0.1f)
        );

        // Walls (green)
        Gizmos.color = Color.green;

        Gizmos.DrawWireCube(
            new Vector3(center.x - halfWidth - wallThickness / 2f, center.y, 0f),
            new Vector3(wallThickness, halfHeight * 2f + wallThickness * 2f, 0.1f)
        );

        Gizmos.DrawWireCube(
            new Vector3(center.x + halfWidth + wallThickness / 2f, center.y, 0f),
            new Vector3(wallThickness, halfHeight * 2f + wallThickness * 2f, 0.1f)
        );

        Gizmos.DrawWireCube(
            new Vector3(center.x, center.y + halfHeight + wallThickness / 2f, 0f),
            new Vector3(halfWidth * 2f + wallThickness * 2f, wallThickness, 0.1f)
        );

        // Bottom line
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            new Vector3(center.x - halfWidth, center.y - halfHeight, 0f),
            new Vector3(center.x + halfWidth, center.y - halfHeight, 0f)
        );
    }
}
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class BrickSpawner : MonoBehaviour {
    public static BrickSpawner Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera gameCamera;
    [SerializeField] private WallSetup wallSetup;

    [Header("Brick Settings")]
    [SerializeField] private int startingHealth = 3;
    [SerializeField] private float brickHeight = 1f;
    [SerializeField] private float brickDepth = 1f;

    [Header("Grid Settings")]
    [SerializeField] private int rows = 3;
    [SerializeField] private int columns = 8;
    [SerializeField] private float horizontalPadding = 0.15f;
    [SerializeField] private float verticalPadding = 0.1f;
    [SerializeField] private float topOffset = 1.5f;

    [Header("Prefab")]
    [SerializeField] private GameObject brickVisualPrefab;

    private EntityManager entityManager;
    private Scene gameScene;
    private Transform bricksParent;

    private int brickIdCounter = 0;
    private List<BrickData> allBricks = new List<BrickData>();

    private int lastScreenWidth;
    private int lastScreenHeight;
    private float lastAspect;

    private class BrickData {
        public int Id;
        public int Row;
        public int Column;
        public Entity Entity;
        public BrickView View;
        public bool IsDestroyed;
    }

    #region Properties
    public int ActiveBrickCount => allBricks.FindAll(b => !b.IsDestroyed).Count;
    #endregion

    private bool IsWorldValid =>
        World.DefaultGameObjectInjectionWorld != null &&
        World.DefaultGameObjectInjectionWorld.IsCreated;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start() {
        if (!IsWorldValid) {
            Debug.LogError("BrickSpawner: ECS World not available");
            return;
        }

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        gameScene = SceneManager.GetSceneByName("GameScene");

        CreateBricksParent();

        if (gameCamera == null) {
            gameCamera = FindGameSceneCamera();
        }

        if (wallSetup == null) {
            wallSetup = FindAnyObjectByType<WallSetup>();
        }

        if (wallSetup != null) {
            wallSetup.OnBoundsChanged += RepositionBricks;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastAspect = gameCamera != null ? gameCamera.aspect : 1f;

        SpawnBricks();
    }

    private void Update() {
        if (HasScreenSizeChanged()) {
            RepositionBricks();
        }
    }

    private bool HasScreenSizeChanged() {
        if (gameCamera == null)
            return false;

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

    private Camera FindGameSceneCamera() {
        Scene scene = SceneManager.GetSceneByName("GameScene");

        if (!scene.IsValid()) {
            return Camera.main;
        }

        foreach (GameObject rootObj in scene.GetRootGameObjects()) {
            Camera cam = rootObj.GetComponentInChildren<Camera>();
            if (cam != null) {
                return cam;
            }
        }

        return Camera.main;
    }

    private void CreateBricksParent() {
        GameObject parentGO = new GameObject("Bricks");
        if (gameScene.IsValid()) {
            SceneManager.MoveGameObjectToScene(parentGO, gameScene);
        }
        bricksParent = parentGO.transform;
    }

    private void CalculateGridLayout(out float brickWidth, out float startX, out float startY) {
        float screenHalfHeight = gameCamera.orthographicSize;
        float screenHalfWidth = screenHalfHeight * gameCamera.aspect;
        Vector2 screenCenter = new Vector2(
            gameCamera.transform.position.x,
            gameCamera.transform.position.y
        );

        float availableWidth = (screenHalfWidth * 2f) - (horizontalPadding * 2f);
        float totalPaddingWidth = horizontalPadding * (columns - 1);
        brickWidth = (availableWidth - totalPaddingWidth) / columns;

        startX = screenCenter.x - screenHalfWidth + horizontalPadding + (brickWidth / 2f);
        startY = screenCenter.y + screenHalfHeight - topOffset;
    }

    private Vector3 CalculateBrickPosition(int row, int col, float brickWidth, float startX, float startY) {
        float xPos = startX + col * (brickWidth + horizontalPadding);
        float yPos = startY - row * (brickHeight + verticalPadding);
        return new Vector3(xPos, yPos, 0f);
    }

    public void SpawnBricks() {
        if (gameCamera == null) {
            Debug.LogError("BrickSpawner: No camera assigned");
            return;
        }

        if (!IsWorldValid) {
            Debug.LogError("BrickSpawner: ECS World not available");
            return;
        }

        CalculateGridLayout(out float brickWidth, out float startX, out float startY);
        Vector2 brickSize = new Vector2(brickWidth, brickHeight);

        for (int row = 0; row < rows; row++) {
            for (int col = 0; col < columns; col++) {
                Vector3 position = CalculateBrickPosition(row, col, brickWidth, startX, startY);
                SpawnBrick(position, brickSize, row, col);
            }
        }

        Debug.Log($"Spawned {rows * columns} bricks in {rows}x{columns} grid");
    }

    private void SpawnBrick(Vector3 position, Vector2 size, int row, int col) {
        float3 pos = new float3(position.x, position.y, 0f);
        float3 brickSize = new float3(size.x, size.y, brickDepth);

        Entity brickEntity = CreateBrickPhysicsEntity(pos, brickSize);
        BrickView brickView = CreateBrickVisual(brickEntity, position, size);

        BrickData data = new BrickData {
            Id = brickIdCounter,
            Row = row,
            Column = col,
            Entity = brickEntity,
            View = brickView,
            IsDestroyed = false
        };

        allBricks.Add(data);
        brickIdCounter++;
    }

    private Entity CreateBrickPhysicsEntity(float3 position, float3 size) {
        Entity entity = entityManager.CreateEntity();

        entityManager.AddComponentData(entity, new LocalTransform {
            Position = position,
            Rotation = quaternion.identity,
            Scale = 1f
        });

        var material = new Unity.Physics.Material {
            Friction = 0f,
            Restitution = 1f,
            CollisionResponse = CollisionResponsePolicy.CollideRaiseCollisionEvents,
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
            PhysicsLayers.BrickFilter,
            material
        );

        entityManager.AddComponentData(entity, new PhysicsCollider {
            Value = boxCollider
        });

        entityManager.AddComponentData(entity, new BrickPhysicsComponent {
            BrickId = brickIdCounter
        });

        entityManager.AddSharedComponent(entity, new PhysicsWorldIndex {
            Value = 0
        });

        return entity;
    }

    private BrickView CreateBrickVisual(Entity entity, Vector3 position, Vector2 size) {
        GameObject visualGO;

        if (brickVisualPrefab != null) {
            visualGO = Instantiate(brickVisualPrefab, position, Quaternion.identity);
        }
        else {
            visualGO = CreateDefaultBrickVisual(position);
        }

        if (bricksParent != null) {
            visualGO.transform.SetParent(bricksParent);
        }
        else if (gameScene.IsValid()) {
            SceneManager.MoveGameObjectToScene(visualGO, gameScene);
        }

        BrickView brickView = visualGO.GetComponent<BrickView>();
        if (brickView == null) {
            brickView = visualGO.AddComponent<BrickView>();
        }

        brickView.SetSize(size);
        brickView.Initialize(entity, brickIdCounter, startingHealth);

        return brickView;
    }

    private GameObject CreateDefaultBrickVisual(Vector3 position) {
        GameObject go = new GameObject($"Brick_{brickIdCounter}");
        go.transform.position = position;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        sr.sprite = Sprite.Create(
            tex,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );

        return go;
    }

    public void RepositionBricks() {
        if (!IsWorldValid || gameCamera == null)
            return;

        CalculateGridLayout(out float brickWidth, out float startX, out float startY);
        Vector2 brickSize = new Vector2(brickWidth, brickHeight);
        float3 colliderSize = new float3(brickWidth, brickHeight, brickDepth);

        foreach (BrickData brick in allBricks) {
            if (brick.IsDestroyed)
                continue;

            Vector3 newPosition = CalculateBrickPosition(brick.Row, brick.Column, brickWidth, startX, startY);

            // Update ECS entity position
            if (entityManager.Exists(brick.Entity)) {
                entityManager.SetComponentData(brick.Entity, new LocalTransform {
                    Position = new float3(newPosition.x, newPosition.y, 0f),
                    Rotation = quaternion.identity,
                    Scale = 1f
                });

                // Update collider size
                UpdateBrickCollider(brick.Entity, colliderSize);
            }

            // Update visual position and size
            if (brick.View != null) {
                brick.View.transform.position = newPosition;
                brick.View.SetSize(brickSize);
            }
        }

        Debug.Log($"Repositioned {ActiveBrickCount} bricks");
    }

    private void UpdateBrickCollider(Entity entity, float3 size) {
        if (!entityManager.HasComponent<PhysicsCollider>(entity))
            return;

        var material = new Unity.Physics.Material {
            Friction = 0f,
            Restitution = 1f,
            CollisionResponse = CollisionResponsePolicy.CollideRaiseCollisionEvents,
            FrictionCombinePolicy = Unity.Physics.Material.CombinePolicy.Minimum,
            RestitutionCombinePolicy = Unity.Physics.Material.CombinePolicy.Maximum
        };

        BlobAssetReference<Unity.Physics.Collider> newCollider = Unity.Physics.BoxCollider.Create(
            new BoxGeometry {
                Center = float3.zero,
                Size = size,
                Orientation = quaternion.identity,
                BevelRadius = 0f
            },
            PhysicsLayers.BrickFilter,
            material
        );

        entityManager.SetComponentData(entity, new PhysicsCollider {
            Value = newCollider
        });
    }

    public void OnBrickDestroyed(BrickView brick) {
        BrickData data = allBricks.Find(b => b.Id == brick.BrickId);
        if (data != null) {
            data.IsDestroyed = true;
            data.View = null;
            data.Entity = Entity.Null;
        }

        if (ActiveBrickCount == 0) {
            GameEvents.AllBricksDestroyed();
        }
    }

    public BrickView GetBrickById(int brickId) {
        BrickData data = allBricks.Find(b => b.Id == brickId && !b.IsDestroyed);
        return data?.View;
    }

    public void ClearAllBricks() {
        foreach (BrickData brick in allBricks) {
            if (!brick.IsDestroyed) {
                if (brick.View != null) {
                    Destroy(brick.View.gameObject);
                }

                if (IsWorldValid && entityManager.Exists(brick.Entity)) {
                    entityManager.DestroyEntity(brick.Entity);
                }
            }
        }

        allBricks.Clear();
        brickIdCounter = 0;
    }

    public void ResetBricks() {
        ClearAllBricks();
        SpawnBricks();
    }

    private void OnDestroy() {
        if (wallSetup != null) {
            wallSetup.OnBoundsChanged -= RepositionBricks;
        }

        ClearAllBricks();

        if (bricksParent != null) {
            Destroy(bricksParent.gameObject);
        }
    }

    private void OnDrawGizmos() {
        Camera cam = gameCamera != null ? gameCamera : Camera.main;
        if (cam == null) return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector2 center = new Vector2(cam.transform.position.x, cam.transform.position.y);

        Gizmos.color = Color.cyan;

        float availableWidth = (halfWidth * 2f) - (horizontalPadding * 2f);
        float totalPaddingWidth = horizontalPadding * (columns - 1);
        float brickWidth = (availableWidth - totalPaddingWidth) / columns;

        float startX = center.x - halfWidth + horizontalPadding + (brickWidth / 2f);
        float startY = center.y + halfHeight - topOffset;

        for (int row = 0; row < rows; row++) {
            for (int col = 0; col < columns; col++) {
                float xPos = startX + col * (brickWidth + horizontalPadding);
                float yPos = startY - row * (brickHeight + verticalPadding);

                Gizmos.DrawWireCube(
                    new Vector3(xPos, yPos, 0f),
                    new Vector3(brickWidth, brickHeight, 0.1f)
                );
            }
        }
    }
}
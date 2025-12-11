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
    [SerializeField] private float brickDepth = 1f;

    [Header("Grid Settings")]
    [SerializeField] private int rows = 3;
    [SerializeField] private int columns = 8;
    [SerializeField] private float padding = 0.1f;
    [SerializeField] private float topOffset = 0.5f;
    [SerializeField] private float sideMargin = 0.3f;

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

    private float currentBrickSize;
    private bool isInitialized = false;

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
        Initialize();
    }

    private void Initialize() {
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

        isInitialized = true;
    }

    private void Update() {
        if (!isInitialized || allBricks.Count == 0)
            return;

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
        if (bricksParent != null)
            return;

        GameObject parentGO = new GameObject("Bricks");
        if (gameScene.IsValid()) {
            SceneManager.MoveGameObjectToScene(parentGO, gameScene);
        }
        bricksParent = parentGO.transform;
    }

    private float CalculateBrickSize() {
        float screenHalfHeight = gameCamera.orthographicSize;
        float screenHalfWidth = screenHalfHeight * gameCamera.aspect;

        float availableWidth = (screenHalfWidth * 2f) - (sideMargin * 2f);
        float availableHeight = screenHalfHeight - topOffset;

        float totalHorizontalPadding = padding * (columns - 1);
        float totalVerticalPadding = padding * (rows - 1);

        float maxBrickWidth = (availableWidth - totalHorizontalPadding) / columns;
        float maxBrickHeight = (availableHeight - totalVerticalPadding) / rows;

        return Mathf.Min(maxBrickWidth, maxBrickHeight);
    }

    private void CalculateGridLayout(out float brickSize, out float startX, out float startY) {
        float screenHalfHeight = gameCamera.orthographicSize;
        Vector2 screenCenter = new Vector2(
            gameCamera.transform.position.x,
            gameCamera.transform.position.y
        );

        brickSize = CalculateBrickSize();
        currentBrickSize = brickSize;

        float totalGridWidth = (columns * brickSize) + ((columns - 1) * padding);
        startX = screenCenter.x - (totalGridWidth / 2f) + (brickSize / 2f);
        startY = screenCenter.y + screenHalfHeight - topOffset - (brickSize / 2f);
    }

    private Vector3 CalculateBrickPosition(int row, int col, float brickSize, float startX, float startY) {
        float xPos = startX + col * (brickSize + padding);
        float yPos = startY - row * (brickSize + padding);
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

        CalculateGridLayout(out float brickSize, out float startX, out float startY);

        for (int row = 0; row < rows; row++) {
            for (int col = 0; col < columns; col++) {
                Vector3 position = CalculateBrickPosition(row, col, brickSize, startX, startY);
                SpawnBrick(position, brickSize, row, col);
            }
        }

        Debug.Log($"Spawned {rows * columns} bricks in {rows}x{columns} grid (size: {brickSize:F2})");
    }

    private void SpawnBrick(Vector3 position, float brickSize, int row, int col) {
        float3 pos = new float3(position.x, position.y, 0f);
        float3 colliderSize = new float3(brickSize, brickSize, brickDepth);

        Entity brickEntity = CreateBrickPhysicsEntity(pos, colliderSize);
        BrickView brickView = CreateBrickVisual(brickEntity, position, brickSize);

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

    private BrickView CreateBrickVisual(Entity entity, Vector3 position, float brickSize) {
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

        brickView.SetSize(brickSize);
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
        if (!IsWorldValid || gameCamera == null || allBricks.Count == 0)
            return;

        CalculateGridLayout(out float brickSize, out float startX, out float startY);
        float3 colliderSize = new float3(brickSize, brickSize, brickDepth);

        foreach (BrickData brick in allBricks) {
            if (brick.IsDestroyed)
                continue;

            Vector3 newPosition = CalculateBrickPosition(brick.Row, brick.Column, brickSize, startX, startY);

            if (entityManager.Exists(brick.Entity)) {
                entityManager.SetComponentData(brick.Entity, new LocalTransform {
                    Position = new float3(newPosition.x, newPosition.y, 0f),
                    Rotation = quaternion.identity,
                    Scale = 1f
                });

                UpdateBrickCollider(brick.Entity, colliderSize);
            }

            if (brick.View != null) {
                brick.View.transform.position = newPosition;
                brick.View.SetSize(brickSize);
            }
        }

        Debug.Log($"Repositioned {ActiveBrickCount} bricks (size: {brickSize:F2})");
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

        Gizmos.color = Color.yellow;
        Vector3 middleLine = new Vector3(center.x, center.y, 0f);
        Gizmos.DrawLine(
            middleLine + Vector3.left * halfWidth,
            middleLine + Vector3.right * halfWidth
        );

        Gizmos.color = Color.cyan;

        float availableWidth = (halfWidth * 2f) - (sideMargin * 2f);
        float availableHeight = halfHeight - topOffset;

        float totalHorizontalPadding = padding * (columns - 1);
        float totalVerticalPadding = padding * (rows - 1);

        float maxBrickWidth = (availableWidth - totalHorizontalPadding) / columns;
        float maxBrickHeight = (availableHeight - totalVerticalPadding) / rows;
        float brickSize = Mathf.Min(maxBrickWidth, maxBrickHeight);

        float totalGridWidth = (columns * brickSize) + ((columns - 1) * padding);
        float startX = center.x - (totalGridWidth / 2f) + (brickSize / 2f);
        float startY = center.y + halfHeight - topOffset - (brickSize / 2f);

        for (int row = 0; row < rows; row++) {
            for (int col = 0; col < columns; col++) {
                float xPos = startX + col * (brickSize + padding);
                float yPos = startY - row * (brickSize + padding);

                Gizmos.DrawWireCube(
                    new Vector3(xPos, yPos, 0f),
                    new Vector3(brickSize, brickSize, 0.1f)
                );
            }
        }
    }
}
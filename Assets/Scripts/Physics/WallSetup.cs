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
    [SerializeField] private float visualInset = 0.05f;

    [Header("Wall Visuals")]
    [SerializeField] private Sprite wallSprite;
    [SerializeField] private Color wallColor = Color.gray;
    [SerializeField] private int wallSortingOrder = -1;
    [SortingLayer]
    [SerializeField] private int wallSortingLayerID;

    private EntityManager entityManager;

    private int lastScreenWidth;
    private int lastScreenHeight;
    private float lastAspect;

    private List<Entity> wallEntities = new List<Entity>();
    private List<GameObject> wallVisuals = new List<GameObject>();
    private Transform wallVisualsParent;
    private Scene gameScene;

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
        gameScene = SceneManager.GetSceneByName("GameScene");

        if (gameCamera == null) {
            gameCamera = FindGameSceneCamera();
        }

        if (gameCamera == null) {
            Debug.LogError("WallSetup: No camera found");
            return;
        }

        CreateWallVisualsParent();

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastAspect = gameCamera.aspect;

        CalculateScreenBounds();
        CreateWalls();
    }

    private void CreateWallVisualsParent() {
        GameObject parentGO = new GameObject("WallVisuals");
        wallVisualsParent = parentGO.transform;

        if (gameScene.IsValid()) {
            SceneManager.MoveGameObjectToScene(parentGO, gameScene);
        }
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

        foreach (GameObject visual in wallVisuals) {
            if (visual != null) {
                Destroy(visual);
            }
        }

        wallVisuals.Clear();
    }

    private Camera FindGameSceneCamera() {
        Scene scene = SceneManager.GetSceneByName("GameScene");

        if (!scene.IsValid()) {
            Debug.LogWarning("GameScene not found, using Camera.main");
            return Camera.main;
        }

        foreach (GameObject rootObj in scene.GetRootGameObjects()) {
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

        float insetWidth = ScreenHalfWidth - visualInset;
        float insetHeight = ScreenHalfHeight - visualInset;

        float leftX = ScreenCenter.x - insetWidth - wallThickness / 2f;
        float rightX = ScreenCenter.x + insetWidth + wallThickness / 2f;
        float topY = ScreenCenter.y + insetHeight + wallThickness / 2f;

        float sideWallHeight = insetHeight * 2f + wallThickness * 2f;
        float topWallWidth = insetWidth * 2f + wallThickness * 2f;

        Entity leftWall = CreateWallEntity(
            new float3(leftX, ScreenCenter.y, 0f),
            new float3(wallThickness, sideWallHeight, 1f)
        );
        wallEntities.Add(leftWall);
        CreateWallVisual("LeftWall", new Vector3(leftX, ScreenCenter.y, 0f), wallThickness, sideWallHeight);

        Entity rightWall = CreateWallEntity(
            new float3(rightX, ScreenCenter.y, 0f),
            new float3(wallThickness, sideWallHeight, 1f)
        );
        wallEntities.Add(rightWall);
        CreateWallVisual("RightWall", new Vector3(rightX, ScreenCenter.y, 0f), wallThickness, sideWallHeight);

        Entity topWall = CreateWallEntity(
            new float3(ScreenCenter.x, topY, 0f),
            new float3(topWallWidth, wallThickness, 1f)
        );
        wallEntities.Add(topWall);
        CreateWallVisual("TopWall", new Vector3(ScreenCenter.x, topY, 0f), topWallWidth, wallThickness);
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
            PhysicsLayers.WallFilter,
            material
        );

        entityManager.AddComponentData(entity, new PhysicsCollider {
            Value = boxCollider
        });

        entityManager.AddComponentData(entity, new WallPhysicsComponent());

        entityManager.AddSharedComponent(entity, new PhysicsWorldIndex {
            Value = 0
        });

        return entity;
    }

    private void CreateWallVisual(string name, Vector3 position, float width, float height) {
        GameObject wallGO = new GameObject(name);
        wallGO.transform.position = position;

        if (wallVisualsParent != null) {
            wallGO.transform.SetParent(wallVisualsParent);
        }
        else if (gameScene.IsValid()) {
            SceneManager.MoveGameObjectToScene(wallGO, gameScene);
        }

        SpriteRenderer sr = wallGO.AddComponent<SpriteRenderer>();
        sr.color = wallColor;
        sr.sortingOrder = wallSortingOrder;
        sr.sortingLayerID = wallSortingLayerID;

        if (wallSprite != null) {
            sr.sprite = wallSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Adaptive;
            sr.size = new Vector2(width, height);
        }
        else {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            sr.sprite = Sprite.Create(
                tex,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                1f
            );

            wallGO.transform.localScale = new Vector3(width, height, 1f);
        }

        wallVisuals.Add(wallGO);
    }

    private void OnDestroy() {
        DestroyWalls();

        if (wallVisualsParent != null) {
            Destroy(wallVisualsParent.gameObject);
        }
    }

    private void OnDrawGizmos() {
        Camera cam = gameCamera != null ? gameCamera : Camera.main;
        if (cam == null) return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector2 center = new Vector2(cam.transform.position.x, cam.transform.position.y);

        float insetHalfWidth = halfWidth - visualInset;
        float insetHalfHeight = halfHeight - visualInset;

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(
            new Vector3(center.x, center.y, 0f),
            new Vector3(halfWidth * 2f, halfHeight * 2f, 0.1f)
        );

        Gizmos.color = Color.green;

        Gizmos.DrawWireCube(
            new Vector3(center.x - insetHalfWidth - wallThickness / 2f, center.y, 0f),
            new Vector3(wallThickness, insetHalfHeight * 2f + wallThickness * 2f, 0.1f)
        );

        Gizmos.DrawWireCube(
            new Vector3(center.x + insetHalfWidth + wallThickness / 2f, center.y, 0f),
            new Vector3(wallThickness, insetHalfHeight * 2f + wallThickness * 2f, 0.1f)
        );

        Gizmos.DrawWireCube(
            new Vector3(center.x, center.y + insetHalfHeight + wallThickness / 2f, 0f),
            new Vector3(insetHalfWidth * 2f + wallThickness * 2f, wallThickness, 0.1f)
        );

        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            new Vector3(center.x - halfWidth, center.y - halfHeight, 0f),
            new Vector3(center.x + halfWidth, center.y - halfHeight, 0f)
        );
    }
}
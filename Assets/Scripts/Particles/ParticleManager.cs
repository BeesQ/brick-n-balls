using UnityEngine;
using UnityEngine.SceneManagement;

public class ParticleManager : MonoBehaviour {
    public static ParticleManager Instance { get; private set; }

    [Header("Particle Prefabs")]
    [SerializeField] private ParticleSystem brickHitParticlePrefab;
    [SerializeField] private ParticleSystem wallHitParticlePrefab;

    [Header("Rendering")]
    [SortingLayer]
    [SerializeField] private int sortingLayerID;
    [SerializeField] private int sortingOrder = 100;

    private Transform particlesParent;
    private Scene gameScene;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start() {
        gameScene = SceneManager.GetSceneByName("GameScene");
    }

    private void OnEnable() {
        GameEvents.OnBrickHit += HandleBrickHit;
        GameEvents.OnWallHit += HandleWallHit;
    }

    private void OnDisable() {
        GameEvents.OnBrickHit -= HandleBrickHit;
        GameEvents.OnWallHit -= HandleWallHit;
    }

    private void HandleBrickHit(int brickId, int previousHealth, Vector3 position, float scale) {
        if (brickHitParticlePrefab == null)
            return;

        Color particleColor = Consts.GetColorForHealth(previousHealth);
        SpawnParticle(brickHitParticlePrefab, position, particleColor, scale);
    }

    private void HandleWallHit(Vector3 position) {
        if (wallHitParticlePrefab == null)
            return;

        SpawnParticle(wallHitParticlePrefab, position, Color.grey, 1f);
    }

    private void EnsureParticlesParent() {
        if (particlesParent != null)
            return;

        GameObject parentGO = new GameObject("Particles");

        if (gameScene.IsValid()) {
            SceneManager.MoveGameObjectToScene(parentGO, gameScene);
        }

        particlesParent = parentGO.transform;
    }

    private void SpawnParticle(ParticleSystem prefab, Vector3 position, Color color, float scale) {
        EnsureParticlesParent();

        ParticleSystem ps = Instantiate(prefab, position, Quaternion.identity, particlesParent);

        ps.transform.localScale = Vector3.one * scale;

        var main = ps.main;
        main.startColor = color;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null) {
            renderer.sortingLayerID = sortingLayerID;
            renderer.sortingOrder = sortingOrder;
        }

        ps.Play();
    }
}
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class CollisionEventBuffer : MonoBehaviour {
    public static CollisionEventBuffer Instance { get; private set; }

    public NativeQueue<BrickCollisionEvent> Events { get; private set; }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Events = new NativeQueue<BrickCollisionEvent>(Allocator.Persistent);
    }

    private void LateUpdate() {
        ProcessCollisionEvents();
    }

    private void ProcessCollisionEvents() {
        if (!Events.IsCreated)
            return;

        CollisionJobSync.LastCollisionJobHandle.Complete();

        while (Events.TryDequeue(out BrickCollisionEvent collisionEvent)) {
            HandleBrickCollision(collisionEvent);
        }
    }

    private void HandleBrickCollision(BrickCollisionEvent collisionEvent) {
        BrickView brick = BrickSpawner.Instance?.GetBrickById(collisionEvent.BrickId);

        if (brick != null && brick.IsAlive) {
            int previousHealth = brick.CurrentHealth;
            Vector3 position = brick.transform.position;
            float scale = brick.transform.localScale.x;

            brick.TakeDamage(1);

            GameEvents.BrickHit(collisionEvent.BrickId, previousHealth, position, scale);
        }
    }

    private void OnDestroy() {
        CollisionJobSync.LastCollisionJobHandle.Complete();

        if (Events.IsCreated) {
            Events.Dispose();
        }

        if (Instance == this) {
            Instance = null;
        }
    }
}
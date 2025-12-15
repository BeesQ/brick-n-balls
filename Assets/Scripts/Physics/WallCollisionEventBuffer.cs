using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class WallCollisionEventBuffer : MonoBehaviour {
    public static WallCollisionEventBuffer Instance { get; private set; }

    public NativeQueue<WallCollisionEvent> Events { get; private set; }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Events = new NativeQueue<WallCollisionEvent>(Allocator.Persistent);
    }

    private void LateUpdate() {
        ProcessCollisionEvents();
    }

    private void ProcessCollisionEvents() {
        if (!Events.IsCreated)
            return;

        WallCollisionJobSync.LastCollisionJobHandle.Complete();

        while (Events.TryDequeue(out WallCollisionEvent collisionEvent)) {
            Vector3 contactPosition = CalculateContactPosition(collisionEvent);
            GameEvents.WallHit(contactPosition);
        }
    }

    private Vector3 CalculateContactPosition(WallCollisionEvent collisionEvent) {
        float ballRadius = BallSpawner.Instance?.BallRadius ?? 0.3f;

        float3 contactPoint = collisionEvent.BallPosition - collisionEvent.NormalTowardsBall * ballRadius;

        return new Vector3(contactPoint.x, contactPoint.y, contactPoint.z);
    }

    private void OnDestroy() {
        WallCollisionJobSync.LastCollisionJobHandle.Complete();

        if (Events.IsCreated) {
            Events.Dispose();
        }

        if (Instance == this) {
            Instance = null;
        }
    }
}
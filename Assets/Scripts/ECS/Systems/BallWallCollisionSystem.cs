using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
public partial struct BallWallCollisionSystem : ISystem {
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<SimulationSingleton>();
    }

    public void OnUpdate(ref SystemState state) {
        if (WallCollisionEventBuffer.Instance == null || !WallCollisionEventBuffer.Instance.Events.IsCreated)
            return;

        SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        var collisionJob = new BallWallCollisionJob {
            BallLookup = SystemAPI.GetComponentLookup<BallPhysicsComponent>(true),
            WallLookup = SystemAPI.GetComponentLookup<WallPhysicsComponent>(true),
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            CollisionEvents = WallCollisionEventBuffer.Instance.Events.AsParallelWriter()
        };

        state.Dependency = collisionJob.Schedule(simulation, state.Dependency);
        WallCollisionJobSync.LastCollisionJobHandle = state.Dependency;
    }
}

public static class WallCollisionJobSync {
    public static Unity.Jobs.JobHandle LastCollisionJobHandle;
}

[BurstCompile]
public struct BallWallCollisionJob : ICollisionEventsJob {
    [ReadOnly] public ComponentLookup<BallPhysicsComponent> BallLookup;
    [ReadOnly] public ComponentLookup<WallPhysicsComponent> WallLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
    public NativeQueue<WallCollisionEvent>.ParallelWriter CollisionEvents;

    public void Execute(CollisionEvent collisionEvent) {
        Entity entityA = collisionEvent.EntityA;
        Entity entityB = collisionEvent.EntityB;

        bool aIsBall = BallLookup.HasComponent(entityA);
        bool bIsBall = BallLookup.HasComponent(entityB);
        bool aIsWall = WallLookup.HasComponent(entityA);
        bool bIsWall = WallLookup.HasComponent(entityB);

        Entity ballEntity = Entity.Null;
        float3 normalTowardsBall = float3.zero;

        if (aIsBall && bIsWall) {
            ballEntity = entityA;
            normalTowardsBall = collisionEvent.Normal;
        }
        else if (bIsBall && aIsWall) {
            ballEntity = entityB;
            normalTowardsBall = -collisionEvent.Normal;
        }

        if (ballEntity != Entity.Null) {
            float3 ballPosition = TransformLookup[ballEntity].Position;

            CollisionEvents.Enqueue(new WallCollisionEvent {
                BallPosition = ballPosition,
                NormalTowardsBall = normalTowardsBall
            });
        }
    }
}

public struct WallCollisionEvent {
    public float3 BallPosition;
    public float3 NormalTowardsBall;
}
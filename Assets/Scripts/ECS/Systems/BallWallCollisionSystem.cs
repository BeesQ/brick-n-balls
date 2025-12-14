using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

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
    public NativeQueue<WallCollisionEvent>.ParallelWriter CollisionEvents;

    public void Execute(CollisionEvent collisionEvent) {
        Entity entityA = collisionEvent.EntityA;
        Entity entityB = collisionEvent.EntityB;

        bool aIsBall = BallLookup.HasComponent(entityA);
        bool bIsBall = BallLookup.HasComponent(entityB);
        bool aIsWall = WallLookup.HasComponent(entityA);
        bool bIsWall = WallLookup.HasComponent(entityB);

        if ((aIsBall && bIsWall) || (bIsBall && aIsWall)) {
            CollisionEvents.Enqueue(new WallCollisionEvent());
        }
    }
}

public struct WallCollisionEvent { }
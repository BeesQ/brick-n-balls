using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;

public static class CollisionJobSync {
    public static JobHandle LastCollisionJobHandle;
}

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
public partial struct BallBrickCollisionSystem : ISystem {
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<SimulationSingleton>();
    }

    public void OnUpdate(ref SystemState state) {
        if (CollisionEventBuffer.Instance == null || !CollisionEventBuffer.Instance.Events.IsCreated)
            return;

        SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        var collisionJob = new BallBrickCollisionJob {
            BallLookup = SystemAPI.GetComponentLookup<BallPhysicsComponent>(true),
            BrickLookup = SystemAPI.GetComponentLookup<BrickPhysicsComponent>(true),
            CollisionEvents = CollisionEventBuffer.Instance.Events.AsParallelWriter()
        };

        state.Dependency = collisionJob.Schedule(simulation, state.Dependency);
        CollisionJobSync.LastCollisionJobHandle = state.Dependency;
    }
}

[BurstCompile]
public struct BallBrickCollisionJob : ICollisionEventsJob {
    [ReadOnly] public ComponentLookup<BallPhysicsComponent> BallLookup;
    [ReadOnly] public ComponentLookup<BrickPhysicsComponent> BrickLookup;
    public NativeQueue<BrickCollisionEvent>.ParallelWriter CollisionEvents;

    public void Execute(CollisionEvent collisionEvent) {
        Entity entityA = collisionEvent.EntityA;
        Entity entityB = collisionEvent.EntityB;

        // Check if this is a ball-brick collision
        bool aIsBall = BallLookup.HasComponent(entityA);
        bool bIsBall = BallLookup.HasComponent(entityB);
        bool aIsBrick = BrickLookup.HasComponent(entityA);
        bool bIsBrick = BrickLookup.HasComponent(entityB);

        // Ball (A) hits Brick (B)
        if (aIsBall && bIsBrick) {
            int brickId = BrickLookup[entityB].BrickId;
            CollisionEvents.Enqueue(new BrickCollisionEvent {
                BrickId = brickId,
                BrickEntity = entityB
            });
        }

        // Ball (B) hits Brick (A)
        else if (bIsBall && aIsBrick) {
            int brickId = BrickLookup[entityA].BrickId;
            CollisionEvents.Enqueue(new BrickCollisionEvent {
                BrickId = brickId,
                BrickEntity = entityA
            });
        }
    }
}

public struct BrickCollisionEvent {
    public int BrickId;
    public Entity BrickEntity;
}
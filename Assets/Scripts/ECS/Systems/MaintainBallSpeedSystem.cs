using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

[BurstCompile]
[UpdateInGroup(typeof(Unity.Physics.Systems.AfterPhysicsSystemGroup))]
[UpdateBefore(typeof(ConstrainToXYPlaneSystem))]
public partial struct MaintainBallSpeedSystem : ISystem {
    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        foreach (var (velocity, ball) in
            SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<BallPhysicsComponent>>()) {

            float3 currentVel = velocity.ValueRO.Linear;
            float currentSpeed = math.length(currentVel);

            if (currentSpeed < 0.001f)
                continue;

            float3 direction = currentVel / currentSpeed;
            velocity.ValueRW.Linear = direction * ball.ValueRO.Speed;
        }
    }
}
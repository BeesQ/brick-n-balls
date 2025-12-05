using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(Unity.Physics.Systems.AfterPhysicsSystemGroup))]
public partial struct ConstrainToXYPlaneSystem : ISystem {
    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        foreach (var (transform, velocity) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>>()) {
            // Lock position to Z = 0
            var pos = transform.ValueRO.Position;
            pos.z = 0f;
            transform.ValueRW.Position = pos;

            // Lock velocity to XY plane only
            var vel = velocity.ValueRO.Linear;
            vel.z = 0f;
            velocity.ValueRW.Linear = vel;

            // Lock angular velocity to Z only (rotation around Z axis)
            var angVel = velocity.ValueRO.Angular;
            angVel.x = 0f;
            angVel.y = 0f;
            velocity.ValueRW.Angular = angVel;
        }
    }
}
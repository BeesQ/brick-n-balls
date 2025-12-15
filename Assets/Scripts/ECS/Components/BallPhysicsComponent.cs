using Unity.Entities;

public struct BallPhysicsComponent : IComponentData {
    public int BallId;
    public float Speed;
}
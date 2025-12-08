using Unity.Physics;

public static class PhysicsLayers {
    public const uint BallLayer = 1 << 0;
    public const uint BrickLayer = 1 << 1;
    public const uint WallLayer = 1 << 2;

    public static CollisionFilter BallFilter => new CollisionFilter {
        BelongsTo = BallLayer,
        CollidesWith = BrickLayer | WallLayer,
        GroupIndex = 0
    };

    public static CollisionFilter BrickFilter => new CollisionFilter {
        BelongsTo = BrickLayer,
        CollidesWith = BallLayer,
        GroupIndex = 0
    };

    public static CollisionFilter WallFilter => new CollisionFilter {
        BelongsTo = WallLayer,
        CollidesWith = BallLayer,
        GroupIndex = 0
    };
}
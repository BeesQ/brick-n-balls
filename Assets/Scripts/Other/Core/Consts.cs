using UnityEngine;

public static class Consts {
    public enum BrickHealth {
        Low = 1,
        Medium = 2,
        High = 3
    }

    public static class Colors {
        public static readonly Color Red = new Color(0.9f, 0.2f, 0.2f, 1f);
        public static readonly Color Yellow = new Color(0.95f, 0.85f, 0.2f, 1f);
        public static readonly Color Green = new Color(0.2f, 0.85f, 0.3f, 1f);
    }

    public static Color GetColorForHealth(int health) {
        return health switch {
            >= (int)BrickHealth.High => Colors.Green,
            (int)BrickHealth.Medium => Colors.Yellow,
            <= (int)BrickHealth.Low => Colors.Red
        };
    }

    public static class Scoring {
        public const int PointsPerBrickHit = 1;
    }
}
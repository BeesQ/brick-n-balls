public interface IDamageable {
    int CurrentHealth { get; }
    void TakeDamage(int damage);
    bool IsAlive { get; }
}
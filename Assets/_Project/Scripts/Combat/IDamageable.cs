// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Interfaz para todo lo que puede recibir daño, sin que el atacante sepa qué es el objetivo.

public interface IDamageable
{
    bool IsDead { get; }
    void TakeDamage(int amount, DamageType type);
}

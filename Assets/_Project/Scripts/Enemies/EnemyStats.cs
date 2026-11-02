// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: ScriptableObject con los datos de cada tipo de enemigo: vida, multiplicadores de daño, ataque, velocidad y detección.

using UnityEngine;

[CreateAssetMenu(fileName = "ES_NewEnemy", menuName = "Game/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Vida")]
    [Min(1)] [SerializeField] private int maxHealth = 60;

    [Header("Daño que recibe (multiplica el daño del player)")]
    [Tooltip("1 = normal, 2 = el doble, 0.5 = la mitad, 0 = inmune")]
    [Min(0f)] [SerializeField] private float meleeDamageMultiplier = 1f;
    [Tooltip("1 = normal, 2 = el doble, 0.5 = la mitad, 0 = inmune")]
    [Min(0f)] [SerializeField] private float rangedDamageMultiplier = 1f;

    [Header("Ataque")]
    [Min(0)] [SerializeField] private int attackDamage = 10;
    [Min(0.1f)] [SerializeField] private float attackRange = 1.8f;
    [Min(0.1f)] [SerializeField] private float attackCooldown = 1.2f;
    [Tooltip("Pausa antes del primer golpe al entrar en rango")]
    [Min(0f)] [SerializeField] private float firstAttackDelay = 0.4f;

    [Header("Movimiento")]
    [Min(0f)] [SerializeField] private float patrolSpeed = 2f;
    [Min(0f)] [SerializeField] private float chaseSpeed = 4.5f;

    [Header("Detección")]
    [Tooltip("En el boss es el radio de activación")]
    [Min(0f)] [SerializeField] private float detectionRange = 8f;
    [Min(0f)] [SerializeField] private float loseTargetRange = 14f;

    public int MaxHealth => maxHealth;
    public int AttackDamage => attackDamage;
    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
    public float FirstAttackDelay => firstAttackDelay;
    public float PatrolSpeed => patrolSpeed;
    public float ChaseSpeed => chaseSpeed;
    public float DetectionRange => detectionRange;
    public float LoseTargetRange => loseTargetRange;

    private float GetDamageMultiplier(DamageType type)
    {
        return type == DamageType.Melee ? meleeDamageMultiplier : rangedDamageMultiplier;
    }

    // Daño final que recibe este enemigo. Con multiplicador 0 es inmune; si no, recibe al menos 1.
    public int ResolveDamage(int baseDamage, DamageType type)
    {
        float multiplier = GetDamageMultiplier(type);
        if (multiplier <= 0f || baseDamage <= 0) return 0;
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
    }

    // Que "perder al player" nunca sea menor que "detectarlo"
    private void OnValidate()
    {
        if (loseTargetRange < detectionRange) loseTargetRange = detectionRange;
    }
}

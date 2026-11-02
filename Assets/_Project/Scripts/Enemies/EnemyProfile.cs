// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Aplica los datos de un EnemyStats al enemigo y modifica el daño recibido con sus multiplicadores.

using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Health))]
public class EnemyProfile : MonoBehaviour, IDamageModifier
{
    [SerializeField] private EnemyStats stats;

    private void Awake()
    {
        if (stats == null)
        {
            Debug.LogWarning($"EnemyProfile ({name}): no tiene EnemyStats asignado, usa los valores de sus componentes.");
            return;
        }

        GetComponent<Health>().SetMaxHealth(stats.MaxHealth);

        EnemyAttack attack = GetComponent<EnemyAttack>();
        if (attack != null) attack.ApplyStats(stats);

        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null) ai.ApplyStats(stats);

        BossAI boss = GetComponent<BossAI>();
        if (boss != null) boss.ApplyStats(stats);
    }

    // IDamageModifier: daño base del atacante x multiplicador del EnemyStats (sin stats, no cambia nada)
    public int ModifyDamage(int amount, DamageType type)
    {
        return stats != null ? stats.ResolveDamage(amount, type) : amount;
    }
}

// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Ataque cuerpo a cuerpo del enemigo: rango, daño, cooldown y pausa antes del primer golpe.

using System;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float cooldown = 1.2f;
    [Tooltip("Pausa antes del primer golpe al entrar en rango: le da tiempo al player a reaccionar")]
    [SerializeField] private float firstAttackDelay = 0.4f;

    // Avisa cada vez que efectivamente ataca (EnemyAnimator lo escucha y reproduce la animación)
    public event Action OnAttackPerformed;

    private float nextAttackTime;

    public float Range => attackRange;

    // La usa EnemyProfile: reemplaza los valores del Inspector por los del EnemyStats
    public void ApplyStats(EnemyStats stats)
    {
        attackRange = stats.AttackRange;
        damage = stats.AttackDamage;
        cooldown = stats.AttackCooldown;
        firstAttackDelay = stats.FirstAttackDelay;
    }

    public bool IsInRange(Transform target)
    {
        return TransformUtil.FlatDistance(transform.position, target.position) <= attackRange;
    }

    public void PrepareAttack()
    {
        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + firstAttackDelay);
    }

    public bool TryAttack(IDamageable target)
    {
        if (target == null || target.IsDead) return false;
        if (Time.time < nextAttackTime) return false;

        nextAttackTime = Time.time + cooldown;
        target.TakeDamage(damage, DamageType.Melee);
        OnAttackPerformed?.Invoke();
        return true;
    }
}

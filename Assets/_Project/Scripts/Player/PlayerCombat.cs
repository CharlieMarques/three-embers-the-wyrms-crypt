// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Ataque cuerpo a cuerpo del player, con el daño sincronizado con la animación.

using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private float attackCooldown = 0.8f;

    [Header("Sincronía con la animación")]
    [Tooltip("Segundos desde que empieza el ataque hasta que entra el daño (el momento del golpe en Dagger_Attack)")]
    [SerializeField] private float hitDelay = 0.25f;
    [Tooltip("Margen extra de rango al aplicar el daño, por si el enemigo se movió un poco durante el golpe")]
    [SerializeField] private float hitRangeTolerance = 0.5f;

    private PlayerAnimator playerAnimator; // opcional
    private Health health;
    private float nextAttackTime;

    public float AttackRange => attackRange;

    private void Awake()
    {
        playerAnimator = GetComponent<PlayerAnimator>();
        health = GetComponent<Health>();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public bool IsInRange(Transform target)
    {
        return Vector3.Distance(transform.position, target.position) <= attackRange;
    }

    // Devuelve true si efectivamente empezó un ataque (no estaba en cooldown).
    public bool TryAttack(Health target)
    {
        if (target == null || target.IsDead) return false;
        if (Time.time < nextAttackTime) return false;

        // El cooldown nunca es menor que el delay: no se pisan dos golpes
        nextAttackTime = Time.time + Mathf.Max(attackCooldown, hitDelay);

        if (playerAnimator != null) playerAnimator.PlayAttack();
        StartCoroutine(HitRoutine(target));
        return true;
    }

    private IEnumerator HitRoutine(Health target)
    {
        if (hitDelay > 0f) yield return new WaitForSeconds(hitDelay);

        // Si el player murió o el enemigo ya no está (muerto, de vuelta en el pool, lejos), el golpe no entra
        if (health != null && health.IsDead) yield break;
        if (target == null || target.IsDead || !target.gameObject.activeInHierarchy) yield break;
        if (Vector3.Distance(transform.position, target.transform.position) > attackRange + hitRangeTolerance) yield break;

        // Daño base del player: el enemigo aplica su multiplicador melee (EnemyStats) por su cuenta
        target.TakeDamage(attackDamage, DamageType.Melee);
    }
}

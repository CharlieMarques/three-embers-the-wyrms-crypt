// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Ataque a distancia del player: lanza una daga del pool hacia el mouse y gasta maná.

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Mana))]
public class PlayerRangedAttack : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int damage = 25;
    [SerializeField] private int manaCost = 10;
    [SerializeField] private float cooldown = 0.4f;

    [Header("Sincronía con la animación")]
    [Tooltip("Segundos desde que empieza el lanzamiento hasta que sale el proyectil (el momento del brazo extendido en Punch)")]
    [SerializeField] private float releaseDelay = 0.2f;

    [Header("Punto de salida")]
    [SerializeField] private float spawnHeight = 1f;
    [SerializeField] private float spawnForward = 0.6f;

    private Mana mana;
    private Health health;
    private PlayerAnimator playerAnimator; // opcional
    private float nextCastTime;

    private void Awake()
    {
        mana = GetComponent<Mana>();
        health = GetComponent<Health>();
        playerAnimator = GetComponent<PlayerAnimator>();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public bool TryCast(Vector3 targetPoint)
    {
        if (projectilePrefab == null) return false;
        if (Time.time < nextCastTime) return false;

        Vector3 dir = targetPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        dir.Normalize();

        if (!mana.TrySpend(manaCost)) return false;

        // El cooldown nunca es menor que el delay: no se pisan dos lanzamientos
        nextCastTime = Time.time + Mathf.Max(cooldown, releaseDelay);
        transform.rotation = Quaternion.LookRotation(dir);

        if (playerAnimator != null) playerAnimator.PlayCast();
        StartCoroutine(ReleaseRoutine(dir));
        return true;
    }

    private IEnumerator ReleaseRoutine(Vector3 dir)
    {
        if (releaseDelay > 0f) yield return new WaitForSeconds(releaseDelay);

        // Si el player murió durante el lanzamiento, el proyectil no sale
        if (health != null && health.IsDead) yield break;

        Vector3 spawnPos = transform.position + Vector3.up * spawnHeight + dir * spawnForward;
        GameObject go = ObjectPool.Instance.Get(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));

        Projectile projectile = go.GetComponent<Projectile>();
        if (projectile != null) projectile.Launch(dir, damage, DamageType.Ranged);
    }
}

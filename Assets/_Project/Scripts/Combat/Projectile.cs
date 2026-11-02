// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Proyectil que sale del pool, avanza en línea recta y daña lo primero que toca (daga del player y fuego del boss).

using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 16f;
    [SerializeField] private float radius = 0.25f;
    [SerializeField] private float lifetime = 2f;
    [Tooltip("Qué puede golpear: Enemy + Default (paredes). NO incluyas Player ni Ground.")]
    [SerializeField] private LayerMask hitMask;
    [Tooltip("Opcional: efecto de impacto (prefab con AutoReturnToPool)")]
    [SerializeField] private GameObject impactEffect;

    private Vector3 direction;
    private int damage;
    private DamageType damageType;
    private float despawnTime;

    private void OnEnable()
    {
        despawnTime = Time.time + lifetime;
    }

    // Lo llama quien dispara, justo después de sacarlo del pool.
    // dmg es el daño base: el objetivo lo ajusta según sus resistencias (ej. EnemyStats).
    public void Launch(Vector3 dir, int dmg, DamageType type = DamageType.Ranged)
    {
        direction = dir.normalized;
        damage = dmg;
        damageType = type;
        despawnTime = Time.time + lifetime;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Update()
    {
        if (Time.time >= despawnTime)
        {
            PooledObject.Despawn(gameObject);
            return;
        }

        float step = speed * Time.deltaTime;

        if (Physics.SphereCast(transform.position, radius, direction, out RaycastHit hit, step, hitMask, QueryTriggerInteraction.Ignore))
        {
            // Cualquier cosa que se pueda dañar (Health u otro IDamageable). Las resistencias las aplica el objetivo.
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null) target.TakeDamage(damage, damageType);

            if (impactEffect != null) ObjectPool.Instance.Get(impactEffect, hit.point, Quaternion.identity);

            PooledObject.Despawn(gameObject);
            return;
        }

        transform.position += direction * step;
    }
}

// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Al morir, el enemigo suelta objetos (pociones) según una probabilidad.

using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyLoot : MonoBehaviour
{
    [Serializable]
    public class LootEntry
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float chance = 0.3f;
    }

    [SerializeField] private LootEntry[] loot;
    [SerializeField] private float scatterRadius = 0.8f;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.OnDeath += DropLoot;
    }

    private void OnDisable()
    {
        health.OnDeath -= DropLoot;
    }

    private void DropLoot()
    {
        if (loot == null) return;

        foreach (LootEntry entry in loot)
        {
            if (entry.prefab == null || UnityEngine.Random.value > entry.chance) continue;

            Vector2 offset = UnityEngine.Random.insideUnitCircle * scatterRadius;
            Vector3 pos = transform.position + new Vector3(offset.x, 0f, offset.y);
            ObjectPool.Instance.Get(entry.prefab, pos, Quaternion.identity);
        }
    }
}

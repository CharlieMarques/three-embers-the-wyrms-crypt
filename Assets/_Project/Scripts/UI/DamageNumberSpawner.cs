// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Muestra un número de daño sobre el objeto cada vez que recibe daño.

using UnityEngine;

[RequireComponent(typeof(Health))]
public class DamageNumberSpawner : MonoBehaviour
{
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private Color color = new Color(1f, 0.9f, 0.3f);
    [SerializeField] private float height = 2.2f;
    [SerializeField] private float randomOffset = 0.4f;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.OnDamaged += Spawn;
    }

    private void OnDisable()
    {
        health.OnDamaged -= Spawn;
    }

    private void Spawn(int amount)
    {
        if (damageNumberPrefab == null) return;

        Vector3 pos = transform.position + Vector3.up * height;
        pos += new Vector3(Random.Range(-randomOffset, randomOffset), 0f, Random.Range(-randomOffset, randomOffset));

        GameObject go = ObjectPool.Instance.Get(damageNumberPrefab, pos, Quaternion.identity);
        DamageNumber number = go.GetComponent<DamageNumber>();
        if (number != null) number.Setup(amount, color);
    }
}

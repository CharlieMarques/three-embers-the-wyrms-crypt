// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Número de daño flotante que sube, se desvanece y vuelve al pool.

using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class DamageNumber : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float riseSpeed = 1.5f;

    private TextMeshPro text;
    private Color baseColor;
    private float spawnTime;

    private void Awake()
    {
        text = GetComponent<TextMeshPro>();
    }

    private void OnEnable()
    {
        spawnTime = Time.time;
    }

    // Lo llama el DamageNumberSpawner justo después de sacarlo del pool
    public void Setup(int amount, Color color)
    {
        text.text = amount.ToString();
        baseColor = color;
        text.color = color;
        spawnTime = Time.time;
    }

    private void LateUpdate()
    {
        float t = (Time.time - spawnTime) / lifetime;
        if (t >= 1f)
        {
            PooledObject.Despawn(gameObject);
            return;
        }

        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        // Siempre mirando a la cámara
        if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;

        Color c = baseColor;
        c.a = 1f - t;
        text.color = c;
    }
}

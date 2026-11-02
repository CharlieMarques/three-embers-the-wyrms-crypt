// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Devuelve el objeto al pool después de unos segundos (efectos de impacto y partículas).

using UnityEngine;

public class AutoReturnToPool : MonoBehaviour
{
    [SerializeField] private float lifetime = 1f;

    private float returnTime;

    private void OnEnable()
    {
        returnTime = Time.time + lifetime;
    }

    private void Update()
    {
        if (Time.time >= returnTime) PooledObject.Despawn(gameObject);
    }
}

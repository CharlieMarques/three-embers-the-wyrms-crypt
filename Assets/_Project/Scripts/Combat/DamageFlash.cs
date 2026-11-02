// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Hace parpadear el modelo en blanco cada vez que el objeto recibe daño.

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private Health health;
    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private Coroutine routine;

    private void Awake()
    {
        health = GetComponent<Health>();
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        health.OnDamaged += Flash;
    }

    private void OnDisable()
    {
        health.OnDamaged -= Flash;
        ClearFlash();
    }

    private void Flash(int amount)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        foreach (Renderer r in renderers)
        {
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, flashColor);
            block.SetColor(EmissionColorId, flashColor * 2f);
            r.SetPropertyBlock(block);
        }

        yield return new WaitForSeconds(flashDuration);

        ClearFlash();
        routine = null;
    }

    private void ClearFlash()
    {
        // Sacar el property block vuelve al color original del material
        foreach (Renderer r in renderers) r.SetPropertyBlock(null);
    }
}

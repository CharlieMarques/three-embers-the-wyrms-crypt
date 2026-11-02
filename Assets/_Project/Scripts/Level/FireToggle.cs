// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Fuego con luz parpadeante para fogatas y antorchas; puede encenderse al completar el altar.

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FireToggle : MonoBehaviour
{
    public enum FireMode { LinkedToAltar, AlwaysLit }

    [Tooltip("Linked To Altar: se prende al completar el altar padre. Always Lit: siempre encendido.")]
    [SerializeField] private FireMode mode = FireMode.LinkedToAltar;

    [Header("Materiales")]
    [Tooltip("Texto que tiene el nombre del material de las llamas")]
    [SerializeField] private string fireMaterialKeyword = "Fire";
    [Tooltip("Material para la fogata apagada. Si queda vacío, usa el 'DarkWood' del propio modelo.")]
    [SerializeField] private Material extinguishedMaterial;
    [SerializeField] private string fallbackMaterialKeyword = "DarkWood";

    [Header("Luz")]
    [Tooltip("Point Light de la fogata. Si queda vacía, busca una entre los hijos.")]
    [SerializeField] private Light fireLight;
    [SerializeField] private float lightIntensity = 3f;
    [SerializeField] private float igniteDuration = 0.6f;
    [Tooltip("Cuánto varía la intensidad al parpadear (0 = sin parpadeo)")]
    [Range(0f, 1f)] [SerializeField] private float flickerAmount = 0.25f;
    [SerializeField] private float flickerSpeed = 8f;

    [Header("Opcional")]
    [Tooltip("Partículas de chispas que se activan al encender")]
    [SerializeField] private ParticleSystem sparks;

    public bool IsLit { get; private set; }

    private Renderer rend;
    private Altar altar;
    private Material[] originalMaterials;
    private int fireSlot = -1;
    private float baseIntensity; // intensidad actual sin parpadeo (sube durante el fade)
    private float noiseOffset;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        altar = GetComponentInParent<Altar>();
        if (fireLight == null) fireLight = GetComponentInChildren<Light>(true);
        noiseOffset = Random.value * 100f;

        originalMaterials = rend.sharedMaterials;
        for (int i = 0; i < originalMaterials.Length; i++)
        {
            Material m = originalMaterials[i];
            if (m == null) continue;

            if (fireSlot < 0 && m.name.Contains(fireMaterialKeyword)) fireSlot = i;
            if (extinguishedMaterial == null && m.name.Contains(fallbackMaterialKeyword)) extinguishedMaterial = m;
        }

        // Sin altar no hay nada que lo encienda: queda siempre prendido (caso antorchas)
        if (altar == null) mode = FireMode.AlwaysLit;

        if (fireSlot < 0 && mode == FireMode.LinkedToAltar)
        {
            Debug.LogWarning($"FireToggle ({name}): no encontré un material que contenga '{fireMaterialKeyword}', las llamas no se van a apagar.");
        }
    }

    private void OnEnable()
    {
        Altar.OnAnyAltarActivated += HandleAltarActivated;
    }

    private void OnDisable()
    {
        Altar.OnAnyAltarActivated -= HandleAltarActivated;
    }

    private void Start()
    {
        if (mode == FireMode.AlwaysLit)
        {
            Ignite(instant: true);
            return;
        }

        // Por si el altar ya estaba activado (no debería pasar, pero queda robusto)
        if (altar.IsActivated) Ignite(instant: true);
        else Extinguish();
    }

    private void Update()
    {
        if (!IsLit || fireLight == null) return;

        // Parpadeo suave con ruido Perlin: más natural que un Random por frame
        float factor = 1f;
        if (flickerAmount > 0f)
        {
            float noise = Mathf.PerlinNoise(noiseOffset, Time.time * flickerSpeed); // 0..1
            factor = 1f + (noise - 0.5f) * 2f * flickerAmount;
        }
        fireLight.intensity = baseIntensity * factor;
    }

    private void HandleAltarActivated(Altar activated)
    {
        if (mode == FireMode.LinkedToAltar && activated == altar) Ignite(instant: false);
    }

    // ---------- Estados ----------

    public void Extinguish()
    {
        IsLit = false;
        StopAllCoroutines();

        if (fireSlot >= 0 && extinguishedMaterial != null)
        {
            Material[] mats = rend.sharedMaterials;
            mats[fireSlot] = extinguishedMaterial;
            rend.sharedMaterials = mats; // solo afecta a este renderer, no al asset
        }

        if (fireLight != null)
        {
            fireLight.intensity = 0f;
            fireLight.enabled = false;
        }

        if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void Ignite(bool instant)
    {
        if (IsLit) return;
        IsLit = true;

        rend.sharedMaterials = originalMaterials; // vuelven las llamas

        if (sparks != null) sparks.Play();

        if (fireLight != null)
        {
            fireLight.enabled = true;
            if (instant)
            {
                baseIntensity = lightIntensity;
                fireLight.intensity = lightIntensity;
            }
            else
            {
                StartCoroutine(FadeInLight());
            }
        }
    }

    private IEnumerator FadeInLight()
    {
        float t = 0f;
        while (t < igniteDuration)
        {
            t += Time.deltaTime;
            baseIntensity = Mathf.Lerp(0f, lightIntensity, t / igniteDuration);
            yield return null;
        }
        baseIntensity = lightIntensity;
    }
}

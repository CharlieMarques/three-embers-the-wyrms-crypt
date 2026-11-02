// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Muerte del enemigo: lo vuelve no clickeable, espera la animación, lo achica y lo devuelve al pool.

using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyDeath : MonoBehaviour, IPoolable
{
    // Aviso global: el GameManager / ObjectiveUI escuchan esto para contar los enemigos derrotados.
    public static event Action<EnemyDeath> OnAnyEnemyDied;

    [Tooltip("El hijo con el modelo visual. Si queda vacío, usa el primer hijo.")]
    [SerializeField] private Transform visual;
    [SerializeField] private float shrinkDuration = 0.4f;
    [Tooltip("Espera extra antes de achicarse. Si tiene un animador (CharacterAnimator), se suma la duración del clip de muerte.")]
    [SerializeField] private float extraDeathDelay = 0f;

    private Health health;
    private CharacterAnimator characterAnimator;
    private Collider[] colliders;
    private Vector3 originalScale = Vector3.one;

    private void Awake()
    {
        health = GetComponent<Health>();
        characterAnimator = GetComponent<CharacterAnimator>();
        colliders = GetComponentsInChildren<Collider>();

        visual = TransformUtil.FindVisual(transform, visual);
        if (visual != null) originalScale = visual.localScale;
    }

    private void OnEnable()
    {
        health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        health.OnDeath -= HandleDeath;
    }

    // IPoolable: el pool lo reutiliza -> vida llena, clickeable y con su tamaño original
    public void OnSpawnFromPool()
    {
        health.ResetHealth();
        foreach (Collider c in colliders) c.enabled = true;
        if (visual != null) visual.localScale = originalScale;
    }

    public void OnReturnToPool() { }

    private void HandleDeath()
    {
        // Ya no se puede clickear ni bloquea al player
        foreach (Collider c in colliders) c.enabled = false;

        OnAnyEnemyDied?.Invoke(this);
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        // 1) Dejamos que se vea la animación de muerte (si tiene)
        float wait = extraDeathDelay + (characterAnimator != null ? characterAnimator.DeathDuration : 0f);
        if (wait > 0f) yield return new WaitForSeconds(wait);

        // 2) Se achica hasta desaparecer
        float t = 0f;
        while (t < shrinkDuration)
        {
            t += Time.deltaTime;
            if (visual != null) visual.localScale = Vector3.Lerp(originalScale, Vector3.zero, t / shrinkDuration);
            yield return null;
        }

        // Antes solo se desactivaba y el pool nunca lo recuperaba (creaba uno nuevo en cada invocación)
        PooledObject.DespawnOrDisable(gameObject);
    }
}

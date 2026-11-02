// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Altar de llama: el player debe quedarse en el círculo unos segundos para encenderlo; también es punto de control.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Altar : PlayerProximityZone
{
    // Aviso global: la BossDoor escucha esto para contar altares activos
    public static event Action<Altar> OnAnyAltarActivated;

    [SerializeField] private float activationTime = 3f;

    [Header("Visual")]
    [Tooltip("Partes que brillan. Si queda vacío, usa los renderers hijos (menos el Progress Fill).")]
    [SerializeField] private Renderer[] glowRenderers;
    [Tooltip("Opcional: un hijo (ej. un disco plano) que crece de 0 al tamaño completo mientras se carga")]
    [SerializeField] private Transform progressFill;
    [Tooltip("Al activarse el altar, el disco de progreso se achica hasta desaparecer en estos segundos")]
    [SerializeField] private float fillHideDuration = 0.4f;
    [SerializeField] private Color inactiveColor = new Color(0.35f, 0.1f, 0.45f);
    [SerializeField] private Color activeColor = new Color(1f, 0.75f, 0.2f);

    public bool IsActivated { get; private set; }
    public float Progress01 => Mathf.Clamp01(progress / activationTime);

    protected override Color GizmoColor => new Color(1f, 0.75f, 0.2f);

    private float progress;
    private Vector3 fillFullScale = Vector3.one;

    private void Awake()
    {
        if (progressFill != null)
        {
            fillFullScale = progressFill.localScale;
            progressFill.localScale = new Vector3(0f, fillFullScale.y, 0f);
        }

        if (glowRenderers == null || glowRenderers.Length == 0)
        {
            List<Renderer> list = new List<Renderer>();
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                if (progressFill != null && r.transform.IsChildOf(progressFill)) continue;
                list.Add(r);
            }
            glowRenderers = list.ToArray();
        }
    }

    private void Start()
    {
        UpdateVisual();
    }

    protected override void Update()
    {
        if (IsActivated) return;

        base.Update(); // llama a OnPlayerStay / OnPlayerExit
        UpdateVisual();

        if (progress >= activationTime) Activate();
    }

    protected override void OnPlayerStay()
    {
        progress += Time.deltaTime;
    }

    protected override void OnPlayerExit()
    {
        progress = 0f;
    }

    private void Activate()
    {
        IsActivated = true;
        progress = activationTime;
        UpdateVisual();

        if (progressFill != null) StartCoroutine(HideFillRoutine());

        if (RespawnManager.Instance != null) RespawnManager.Instance.SetCheckpoint(transform.position);

        OnAnyAltarActivated?.Invoke(this);
    }

    // El disco de progreso ya cumplió su función: se achica hasta desaparecer
    private IEnumerator HideFillRoutine()
    {
        Vector3 startScale = progressFill.localScale;
        float t = 0f;

        while (t < fillHideDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / fillHideDuration);
            progressFill.localScale = new Vector3(startScale.x * (1f - k), startScale.y, startScale.z * (1f - k));
            yield return null;
        }

        progressFill.gameObject.SetActive(false);
    }

    private void UpdateVisual()
    {
        float t = Progress01;

        GlowUtil.SetColor(glowRenderers, Color.Lerp(inactiveColor, activeColor, t), IsActivated ? 3f : 1f + t);

        if (progressFill != null)
        {
            progressFill.localScale = new Vector3(fillFullScale.x * t, fillFullScale.y, fillFullScale.z * t);
        }
    }
}

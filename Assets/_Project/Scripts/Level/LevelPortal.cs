// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Portal del final del nivel: aparece al derrotar al jefe y, al pisarlo, completa el nivel.

using UnityEngine;

public class LevelPortal : PlayerProximityZone
{
    [Header("Animación")]
    [Tooltip("El hijo visual. Si queda vacío, usa el primer hijo.")]
    [SerializeField] private Transform visual;
    [SerializeField] private float spinSpeed = 120f;
    [SerializeField] private float appearDuration = 0.6f;

    private bool used;
    private float enabledTime;
    private Vector3 visualBaseScale = Vector3.one;

    protected override Color GizmoColor => Color.green;

    private void Awake()
    {
        visual = TransformUtil.FindVisual(transform, visual);
        if (visual != null) visualBaseScale = visual.localScale;
    }

    private void OnEnable()
    {
        used = false;
        enabledTime = Time.time;
    }

    protected override void Update()
    {
        Animate();
        base.Update();
    }

    protected override void OnPlayerEnter()
    {
        if (used) return;
        used = true;

        if (GameManager.Instance != null) GameManager.Instance.CompleteLevel();
        else Debug.LogWarning("LevelPortal: no hay GameManager en la escena.");
    }

    private void Animate()
    {
        if (visual == null) return;

        visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

        // Aparece creciendo desde 0
        float k = Mathf.Clamp01((Time.time - enabledTime) / appearDuration);
        visual.localScale = visualBaseScale * Mathf.SmoothStep(0f, 1f, k);
    }
}

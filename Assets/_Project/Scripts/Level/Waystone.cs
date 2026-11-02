// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Piedra de teletransporte: se enciende al pisarla y pasa a ser el punto de reaparición.

using UnityEngine;

public class Waystone : PlayerProximityZone
{
    [Tooltip("Dónde reaparece el player. Si queda vacío, usa la posición del waystone.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Marcalo en el waystone de la zona de inicio")]
    [SerializeField] private bool isStartWaystone;

    [Header("Visual")]
    [Tooltip("Si queda vacío, usa todos los renderers hijos")]
    [SerializeField] private Renderer[] glowRenderers;
    [SerializeField] private Color inactiveColor = new Color(0.25f, 0.25f, 0.3f);
    [SerializeField] private Color visitedColor = new Color(0.1f, 0.35f, 0.6f);
    [SerializeField] private Color currentColor = new Color(0.2f, 0.9f, 1f);

    public Vector3 SpawnPoint => spawnPoint != null ? spawnPoint.position : transform.position;
    public bool IsActivated { get; private set; }

    private void Awake()
    {
        glowRenderers = TransformUtil.RenderersOrChildren(this, glowRenderers);
    }

    private void Start()
    {
        RefreshVisual();
        if (isStartWaystone) Activate();
    }

    // Solo al ENTRAR (no en cada frame que está adentro)
    protected override void OnPlayerEnter()
    {
        Activate();
    }

    public void Activate()
    {
        IsActivated = true;
        if (RespawnManager.Instance != null) RespawnManager.Instance.SetCheckpoint(this);
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        bool isCurrent = RespawnManager.Instance != null && RespawnManager.Instance.IsCurrent(this);

        Color color = isCurrent ? currentColor : (IsActivated ? visitedColor : inactiveColor);
        GlowUtil.SetColor(glowRenderers, color);
    }
}

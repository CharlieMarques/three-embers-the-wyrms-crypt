// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Poción de vida o maná que se agarra al caminar por encima, solo si hace falta.

using UnityEngine;

public class Potion : PlayerProximityZone
{
    public enum PotionType { Health, Mana }

    [SerializeField] private PotionType type = PotionType.Health;
    [SerializeField] private int amount = 30;
    [Tooltip("Pausa antes de poder agarrarla (para que se vea caer)")]
    [SerializeField] private float pickupDelay = 0.3f;

    [Header("Animación")]
    [Tooltip("El hijo visual. Si queda vacío, usa el primer hijo.")]
    [SerializeField] private Transform visual;
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 3f;

    private float enabledTime;
    private Vector3 visualBasePos;

    protected override Color GizmoColor => type == PotionType.Health ? Color.red : Color.blue;

    private void Awake()
    {
        visual = TransformUtil.FindVisual(transform, visual);
        if (visual != null) visualBasePos = visual.localPosition;
    }

    private void OnEnable()
    {
        enabledTime = Time.time;
    }

    protected override void Update()
    {
        Animate();
        base.Update();
    }

    protected override void OnPlayerStay()
    {
        if (Time.time < enabledTime + pickupDelay) return;

        IResource resource = type == PotionType.Health ? (IResource)PlayerRef.Health : PlayerRef.Mana;
        if (resource == null || resource.IsFull) return;

        resource.Restore(amount);
        PooledObject.Despawn(gameObject);
    }

    private void Animate()
    {
        if (visual == null) return;

        visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        visual.localPosition = visualBasePos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }
}

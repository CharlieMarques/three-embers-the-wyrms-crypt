// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Animaciones del enemigo: hereda lo común de CharacterAnimator y agrega la aparición (Awaken/Spawn) y los ataques al azar.

using UnityEngine;

public class EnemyAnimator : CharacterAnimator
{
    [Header("Aparición")]
    [Tooltip("Trigger que saca al enemigo del estado dormido (Dormant -> Spawn)")]
    [SerializeField] private string awakenTrigger = "Awaken";
    [Tooltip("Texto que tiene el nombre del clip de aparición. Si no existe, no hay espera.")]
    [SerializeField] private string spawnClipKeyword = "Spawn";
    [Tooltip("Multiplicador de la animación de Spawn (2 = el doble de rápido). " +
             "En el Animator, el estado Spawn tiene que usar el parámetro 'SpawnSpeed' como Multiplier.")]
    [SerializeField] private float spawnSpeed = 1f;
    [SerializeField] private string spawnSpeedParam = "SpawnSpeed";

    private EnemyAttack attack;
    private float spawnDuration;

    // Duración del clip de aparición (0 si no hay). La usa EnemyAI para no moverse mientras aparece.
    public float SpawnDuration { get { Init(); return spawnDuration; } }

    protected override void OnInitialized()
    {
        // La espera se acorta junto con la animación, así nunca quedan desincronizadas
        spawnDuration = FindClipLength(spawnClipKeyword) / Mathf.Max(0.01f, spawnSpeed);
    }

    protected override void Awake()
    {
        base.Awake();
        attack = GetComponent<EnemyAttack>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (attack != null) attack.OnAttackPerformed += HandleAttack;

        // Al reaparecer desde el pool, vuelve al estado inicial (Spawn / Idle / Flying).
        // Rebind vuelve los parámetros a sus valores por defecto: hay que volver a poner la velocidad del Spawn.
        ResetAnimator();
        SetFloat(spawnSpeedParam, spawnSpeed);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (attack != null) attack.OnAttackPerformed -= HandleAttack;
    }

    // Lo llama EnemyAI cuando el enemigo se despierta (o apenas aparece, si no empieza dormido)
    public void PlayAwaken()
    {
        SetTrigger(awakenTrigger);
    }

    private void HandleAttack()
    {
        PlayAttack(); // variante al azar (o alternada, según "Alternate Attacks")
    }
}

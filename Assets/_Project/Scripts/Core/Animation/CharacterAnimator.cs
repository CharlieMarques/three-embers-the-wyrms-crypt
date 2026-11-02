// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Clase base de los animadores de personajes: velocidad, ataques, golpe recibido, muerte y duración de clips.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public abstract class CharacterAnimator : MonoBehaviour
{
    [Header("Parámetros del Animator")]
    [SerializeField] protected string speedParam = "Speed";
    [SerializeField] protected string attackTrigger = "Attack";
    [SerializeField] protected string attackIndexParam = "AttackIndex";
    [SerializeField] protected string hitTrigger = "Hit";
    [SerializeField] protected string dieTrigger = "Die";

    [Header("Ataques")]
    [Tooltip("Cantidad de animaciones de ataque distintas (Attack, Attack_2 = 2)")]
    [SerializeField] protected int attackVariants = 2;
    [Tooltip("Activado: alterna 0, 1, 0, 1... Desactivado: elige al azar (puede repetir)")]
    [SerializeField] protected bool alternateAttacks = false;

    [Header("Clips especiales (se buscan por nombre)")]
    [Tooltip("Texto que tiene el nombre del clip de muerte, para medir cuánto dura")]
    [SerializeField] protected string deathClipKeyword = "Death";

    protected Health health;
    protected NavMeshAgent agent;
    protected Animator animator;

    private readonly HashSet<string> parameters = new HashSet<string>();
    private bool initialized;
    private float deathDuration;
    private int nextAttackIndex;

    // Duración del clip de muerte (0 si no hay). La usan EnemyDeath, BossAI y RespawnManager.
    public float DeathDuration { get { Init(); return deathDuration; } }

    protected virtual void Awake()
    {
        Init();
    }

    // Se inicializa la primera vez que alguien lo necesita (Awake u otro script, lo que pase primero)
    protected void Init()
    {
        if (initialized) return;
        initialized = true;

        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning($"{GetType().Name} ({name}): no encontré un Animator en los hijos.");
            return;
        }

        foreach (AnimatorControllerParameter p in animator.parameters) parameters.Add(p.name);
        deathDuration = FindClipLength(deathClipKeyword);
        OnInitialized();
    }

    // Para que los hijos midan sus propios clips (ya con el Animator encontrado)
    protected virtual void OnInitialized() { }

    protected virtual void OnEnable()
    {
        Init();
        health.OnDamaged += HandleDamaged;
        health.OnDeath += HandleDeath;
    }

    protected virtual void OnDisable()
    {
        health.OnDamaged -= HandleDamaged;
        health.OnDeath -= HandleDeath;
    }

    protected virtual void Update()
    {
        // Velocidad real del agente -> el Animator decide entre Idle / Walk / Run
        if (animator == null || agent == null) return;
        SetFloat(speedParam, agent.enabled ? agent.velocity.magnitude : 0f);
    }

    // ---------- Acciones ----------

    // Ataque eligiendo la variante sola (alternada o al azar)
    public void PlayAttack()
    {
        PlayAttack(NextAttackIndex());
    }

    // Ataque concreto (0 = Attack, 1 = Attack_2...). La usa BossAI para la ráfaga.
    public void PlayAttack(int index)
    {
        if (animator == null) return;
        SetInteger(attackIndexParam, index);
        SetTrigger(attackTrigger);
    }

    protected virtual void HandleDamaged(int amount)
    {
        // El golpe mortal no reproduce Hit: de eso se encarga Die
        if (health.Current <= 0) return;
        SetTrigger(hitTrigger);
    }

    protected virtual void HandleDeath()
    {
        // Limpiamos triggers pendientes para que nada interrumpa la animación de muerte
        ResetTrigger(hitTrigger);
        ResetTrigger(attackTrigger);
        SetTrigger(dieTrigger);
    }

    // Vuelve el Animator a su estado inicial (al reaparecer o al salir del pool)
    protected void ResetAnimator()
    {
        if (animator == null) return;
        animator.Rebind();
        animator.Update(0f);
        nextAttackIndex = 0;
    }

    // ---------- Utilidades para los hijos ----------

    protected int NextAttackIndex()
    {
        if (attackVariants <= 1) return 0;

        int index = alternateAttacks ? nextAttackIndex : Random.Range(0, attackVariants);
        nextAttackIndex = (index + 1) % attackVariants;
        return index;
    }

    protected void SetTrigger(string param)
    {
        if (animator != null && HasParameter(param)) animator.SetTrigger(param);
    }

    protected void ResetTrigger(string param)
    {
        if (animator != null && HasParameter(param)) animator.ResetTrigger(param);
    }

    protected void SetFloat(string param, float value)
    {
        if (animator != null && HasParameter(param)) animator.SetFloat(param, value);
    }

    protected void SetInteger(string param, int value)
    {
        if (animator != null && HasParameter(param)) animator.SetInteger(param, value);
    }

    protected bool HasParameter(string param)
    {
        return !string.IsNullOrEmpty(param) && parameters.Contains(param);
    }

    protected float FindClipLength(string keyword)
    {
        if (animator == null || string.IsNullOrEmpty(keyword) || animator.runtimeAnimatorController == null) return 0f;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name.Contains(keyword)) return clip.length;
        }
        return 0f;
    }
}

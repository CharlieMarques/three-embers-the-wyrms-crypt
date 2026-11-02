// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: IA del jefe Dragón Ancestral: pelea en tres fases, ráfagas de fuego, invocación de esbirros con escudo y reinicio si muere el player.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyAttack))]
public class BossAI : MonoBehaviour
{
    public static event Action<BossAI> OnBossActivated;
    public static event Action<BossAI> OnBossReset;
    public static event Action<BossAI> OnBossDefeated;
    public event Action<int> OnPhaseChanged;

    private enum State { Idle, Fight, Dead }

    [SerializeField] private string bossName = "Dragón Ancestral";

    [Header("Activación")]
    [Tooltip("Se despierta cuando el player entra en este radio (o cuando le pegan)")]
    [SerializeField] private float activationRange = 10f;

    [Header("Movimiento")]
    [SerializeField] private float chaseSpeed = 2.5f;
    [SerializeField] private float turnSpeed = 180f;

    [Header("Fases (porcentaje de vida)")]
    [Range(0f, 1f)] [SerializeField] private float phase2Threshold = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float phase3Threshold = 0.3f;

    [Header("Fase 2+: ráfaga de proyectiles")]
    [Tooltip("Prefab con Projectile cuyo Hit Mask sea Player + Default")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int projectilesPerBurst = 12;
    [SerializeField] private int projectileDamage = 10;
    [SerializeField] private float burstCooldown = 5f;
    [Tooltip("Tiempo de aviso (parpadeo) antes de disparar")]
    [SerializeField] private float burstWindup = 0.8f;
    [SerializeField] private float burstHeight = 1.5f;
    [Tooltip("Distancia desde el centro del boss donde nacen los proyectiles (mayor que su radio)")]
    [SerializeField] private float burstSpawnOffset = 1.8f;

    [Header("Invocación de minions")]
    [Tooltip("Prefab de enemigo chico (ej. Enemy_Skeleton). Sale del pool.")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int minionsPerSummon = 2;
    [Tooltip("Desde qué fase invoca. Invoca UNA vez por fase y es invulnerable mientras sus minions sigan vivos.")]
    [Range(1, 3)] [SerializeField] private int summonFromPhase = 2;
    [SerializeField] private float summonRadius = 3f;
    [Tooltip("Opcional: objeto que se muestra mientras es invulnerable (ej. una esfera/escudo)")]
    [SerializeField] private GameObject shieldVisual;

    [Header("Fase 3: furia")]
    [SerializeField] private float phase3SpeedMultiplier = 1.6f;
    [SerializeField] private float phase3BurstCooldownMultiplier = 0.6f;

    [Header("Visual")]
    [Tooltip("El hijo con el modelo. Si queda vacío, usa el primer hijo.")]
    [SerializeField] private Transform visual;
    [Tooltip("Si queda vacío, usa todos los renderers hijos")]
    [SerializeField] private Renderer[] glowRenderers;
    [SerializeField] private Color phase1Color = new Color(0.6f, 0.2f, 1f);
    [SerializeField] private Color phase2Color = new Color(1f, 0.5f, 0.1f);
    [SerializeField] private Color phase3Color = new Color(1f, 0.1f, 0.1f);
    [SerializeField] private float deathDuration = 1.2f;

    [Header("Animaciones (si tiene un animador, ej. EnemyAnimator)")]
    [Tooltip("Animación de ataque que reproduce mientras carga la ráfaga (0 = Attack, 1 = Attack_2)")]
    [SerializeField] private int burstAttackAnimation = 1;

    public string BossName => bossName;
    public Health Health => health;
    public int Phase => phase;

    private NavMeshAgent agent;
    private Health health;
    private EnemyAttack attack;
    private CharacterAnimator characterAnimator; // opcional: modelos animados (ej. dragón)
    private Health playerHealth;

    private State state = State.Idle;
    private int phase = 1;
    private bool isCasting;
    private float nextBurstTime;
    private int lastSummonPhase;                          // fase en la que invocó por última vez
    private readonly List<Health> minions = new List<Health>();
    private Vector3 homePosition;
    private Vector3 visualBaseScale = Vector3.one;

    private float CurrentSpeed => phase >= 3 ? chaseSpeed * phase3SpeedMultiplier : chaseSpeed;
    private float CurrentBurstCooldown => phase >= 3 ? burstCooldown * phase3BurstCooldownMultiplier : burstCooldown;
    private Color PhaseColor => phase == 1 ? phase1Color : (phase == 2 ? phase2Color : phase3Color);

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        attack = GetComponent<EnemyAttack>();
        characterAnimator = GetComponent<CharacterAnimator>();

        agent.stoppingDistance = attack.Range * 0.8f;
        agent.speed = chaseSpeed;
        homePosition = transform.position;

        visual = TransformUtil.FindVisual(transform, visual);
        if (visual != null) visualBaseScale = visual.localScale;
        glowRenderers = TransformUtil.RenderersOrChildren(this, glowRenderers);
        if (shieldVisual != null) shieldVisual.SetActive(false);
    }

    // La usa EnemyProfile (antes del Awake): reemplaza los valores del Inspector por los del EnemyStats.
    // La vida y el ataque cuerpo a cuerpo los aplica EnemyProfile directo en Health y EnemyAttack.
    public void ApplyStats(EnemyStats stats)
    {
        chaseSpeed = stats.ChaseSpeed;
        activationRange = stats.DetectionRange;
    }

    private void OnEnable()
    {
        health.OnHealthChanged += HandleHealthChanged;
        health.OnDamaged += HandleDamaged;
        health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        health.OnHealthChanged -= HandleHealthChanged;
        health.OnDamaged -= HandleDamaged;
        health.OnDeath -= HandleDeath;
    }

    private void Start()
    {
        SetPhaseVisual();

        playerHealth = PlayerRef.Health;
        if (playerHealth != null) playerHealth.OnDeath += HandlePlayerDeath;
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDeath -= HandlePlayerDeath;
    }

    private void Update()
    {
        if (state == State.Dead || !agent.isOnNavMesh) return;

        UpdateMinionShield();

        if (!PlayerRef.IsAlive) return;

        Transform player = PlayerRef.Transform;

        if (state == State.Idle)
        {
            if (PlayerRef.FlatDistanceTo(transform.position) <= activationRange) Activate();
            return;
        }

        // Mientras carga una ráfaga, solo gira hacia el player
        if (isCasting)
        {
            TransformUtil.RotateTowardsFlat(transform, player.position, turnSpeed);
            return;
        }

        // Habilidades especiales
        if (phase >= 2 && projectilePrefab != null && Time.time >= nextBurstTime)
        {
            StartCoroutine(BurstRoutine());
            return;
        }
        // Una invocación por fase, y solo si no quedan minions vivos de antes
        if (minionPrefab != null && phase >= summonFromPhase && lastSummonPhase < phase && AliveMinions() == 0)
        {
            Summon();
        }

        // Cuerpo a cuerpo
        if (attack.IsInRange(player))
        {
            if (agent.hasPath) agent.ResetPath();
            TransformUtil.RotateTowardsFlat(transform, player.position, turnSpeed);
            attack.TryAttack(playerHealth);
        }
        else
        {
            agent.SetDestination(player.position);
        }
    }

    // ---------- Estados ----------

    private void Activate()
    {
        state = State.Fight;
        agent.speed = CurrentSpeed;
        nextBurstTime = Time.time + 2f;
        attack.PrepareAttack();

        OnBossActivated?.Invoke(this);
    }

    private void SetPhase(int newPhase)
    {
        phase = newPhase;
        agent.speed = CurrentSpeed;
        SetPhaseVisual();

        if (phase == 2) nextBurstTime = Time.time + 0.5f; // primera ráfaga casi enseguida
        // La invocación de la nueva fase se dispara sola en Update (si no quedan minions vivos)

        OnPhaseChanged?.Invoke(phase);
    }

    // ---------- Habilidades ----------

    private IEnumerator BurstRoutine()
    {
        isCasting = true;
        if (agent.hasPath) agent.ResetPath();

        // Animación de "cargar el aliento" durante el aviso
        if (characterAnimator != null) characterAnimator.PlayAttack(burstAttackAnimation);

        // Aviso: parpadeo blanco para que el player pueda alejarse
        float t = 0f;
        while (t < burstWindup)
        {
            t += Time.deltaTime;
            float pulse = Mathf.PingPong(t * 8f, 1f);
            GlowUtil.SetColor(glowRenderers, Color.Lerp(PhaseColor, Color.white, pulse), 3f);
            yield return null;
        }

        SetPhaseVisual();
        FireBurst();

        nextBurstTime = Time.time + CurrentBurstCooldown;
        isCasting = false;
    }

    private void FireBurst()
    {
        float angleOffset = UnityEngine.Random.Range(0f, 360f);
        float step = 360f / projectilesPerBurst;

        for (int i = 0; i < projectilesPerBurst; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, angleOffset + i * step, 0f) * Vector3.forward;
            Vector3 pos = transform.position + Vector3.up * burstHeight + dir * burstSpawnOffset;

            GameObject go = ObjectPool.Instance.Get(projectilePrefab, pos, Quaternion.LookRotation(dir));
            Projectile projectile = go.GetComponent<Projectile>();
            if (projectile != null) projectile.Launch(dir, projectileDamage, DamageType.Ranged);
        }
    }

    private void Summon()
    {
        lastSummonPhase = phase;
        minions.Clear();

        for (int i = 0; i < minionsPerSummon; i++)
        {
            Vector2 circle = UnityEngine.Random.insideUnitCircle.normalized * summonRadius;
            Vector3 point = transform.position + new Vector3(circle.x, 0f, circle.y);

            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                GameObject go = ObjectPool.Instance.Get(minionPrefab, hit.position, Quaternion.identity);
                Health minionHealth = go.GetComponent<Health>();
                if (minionHealth != null) minions.Add(minionHealth);
            }
        }

        UpdateMinionShield(); // se vuelve invulnerable enseguida
    }

    // Invulnerable mientras quede al menos un minion vivo
    private void UpdateMinionShield()
    {
        bool shielded = AliveMinions() > 0;
        if (health.Invulnerable == shielded) return;

        health.Invulnerable = shielded;
        if (shieldVisual != null) shieldVisual.SetActive(shielded);
    }

    private int AliveMinions()
    {
        int alive = 0;
        foreach (Health m in minions)
        {
            // Un minion muerto vuelve al pool (se desactiva): ese también cuenta como "muerto"
            if (m != null && !m.IsDead && m.gameObject.activeInHierarchy) alive++;
        }
        return alive;
    }

    // ---------- Eventos ----------

    private void HandleHealthChanged(int current, int max)
    {
        if (state == State.Dead || max <= 0) return;

        float ratio = (float)current / max;
        int newPhase = ratio <= phase3Threshold ? 3 : (ratio <= phase2Threshold ? 2 : 1);

        if (newPhase > phase) SetPhase(newPhase);
    }

    private void HandleDamaged(int amount)
    {
        // Si le pegan desde lejos (proyectil) mientras está dormido, se despierta
        if (state == State.Idle && PlayerRef.IsAlive) Activate();
    }

    private void HandleDeath()
    {
        state = State.Dead;
        StopAllCoroutines();
        isCasting = false;

        if (agent.isOnNavMesh) agent.ResetPath();
        agent.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        if (shieldVisual != null) shieldVisual.SetActive(false);

        OnBossDefeated?.Invoke(this);
        StartCoroutine(DeathRoutine());
    }

    private void HandlePlayerDeath()
    {
        if (state != State.Fight) return;

        // Reinicio de la pelea: vuelve a su lugar, se cura y vuelve a fase 1
        StopAllCoroutines();
        isCasting = false;
        state = State.Idle;
        phase = 1;
        lastSummonPhase = 0; // en el próximo intento vuelve a invocar en cada fase
        agent.speed = chaseSpeed;
        health.ResetHealth();
        SetPhaseVisual();
        if (agent.isOnNavMesh) agent.SetDestination(homePosition);

        OnBossReset?.Invoke(this);
    }

    // ---------- Visual / utilidades ----------

    private IEnumerator DeathRoutine()
    {
        // Primero dejamos que se vea completa la animación de muerte (si tiene animador)
        float animWait = characterAnimator != null ? characterAnimator.DeathDuration : 0f;
        if (animWait > 0f) yield return new WaitForSeconds(animWait);

        Vector3 basePos = visual != null ? visual.localPosition : Vector3.zero;
        float t = 0f;

        while (t < deathDuration)
        {
            t += Time.deltaTime;
            float k = t / deathDuration;

            if (visual != null)
            {
                // Tiembla y se hunde mientras se achica
                Vector3 shake = UnityEngine.Random.insideUnitSphere * 0.15f * (1f - k);
                visual.localPosition = basePos + shake + Vector3.down * k;
                visual.localScale = Vector3.Lerp(visualBaseScale, Vector3.zero, k * k);
            }
            GlowUtil.SetColor(glowRenderers, Color.Lerp(PhaseColor, Color.white, k), 2f + k * 4f);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    private void SetPhaseVisual()
    {
        GlowUtil.SetColor(glowRenderers, PhaseColor, 2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, activationRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, summonRadius);
    }
}

// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: IA de los enemigos comunes: dormido, aparición, patrulla, persecución y ataque al player.

using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyAttack))]
public class EnemyAI : MonoBehaviour, IPoolable
{
    private enum State { Dormant, Spawning, Patrol, Chase, Attack, Dead }

    [Header("Emboscada")]
    [Tooltip("Empieza dormido (ej. esqueleto enterrado) y se despierta cuando el player se acerca o le pegan")]
    [SerializeField] private bool startDormant = false;
    [SerializeField] private float awakenRange = 5f;
    [Tooltip("Oculta el modelo mientras duerme (por si el primer frame del Spawn no lo muestra enterrado)")]
    [SerializeField] private bool hideWhileDormant = false;

    [Header("Detección")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float loseTargetRange = 14f;

    [Header("Patrulla")]
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Persecución")]
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float turnSpeed = 540f;
    [Tooltip("Si el destino está a más de estos grados de donde mira, primero gira en el lugar y después camina")]
    [SerializeField] private float turnInPlaceAngle = 45f;

    private NavMeshAgent agent;
    private Health health;
    private EnemyAttack attack;
    private EnemyAnimator enemyAnimator;

    private State state;
    private Vector3 homePosition;
    private float waitTimer;
    private float spawnEndTime;
    private bool awakenPending;
    private bool awakenedByPlayer; // si lo despertó el player, al terminar el spawn va directo a perseguirlo
    private Renderer[] renderers;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        attack = GetComponent<EnemyAttack>();
        enemyAnimator = GetComponent<EnemyAnimator>();
        renderers = GetComponentsInChildren<Renderer>(true);

        // Que frene un poco antes del rango de ataque para no empujar al player
        agent.stoppingDistance = attack.Range * 0.8f;
    }

    private void OnEnable()
    {
        health.OnDamaged += HandleDamaged;
        health.OnDamageBlocked += HandleDamageBlocked;
        health.OnDeath += HandleDeath;

        agent.enabled = true;
        homePosition = transform.position;

        if (startDormant) EnterDormant();
        else Awaken(byPlayer: false);
    }

    // IPoolable: los que salen del pool (ej. invocados por el boss) nunca empiezan dormidos
    public void OnSpawnFromPool()
    {
        homePosition = transform.position;
        if (state == State.Dormant) Awaken(byPlayer: false);
    }

    public void OnReturnToPool() { }

    private void OnDisable()
    {
        health.OnDamaged -= HandleDamaged;
        health.OnDamageBlocked -= HandleDamageBlocked;
        health.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        if (state == State.Dead) return;

        // El trigger del Animator se dispara acá (y no en OnEnable) porque EnemyAnimator
        // reinicia el Animator en su OnEnable y borraría el trigger
        if (awakenPending)
        {
            awakenPending = false;
            if (enemyAnimator != null) enemyAnimator.PlayAwaken();

            // Si tiene animación de aparición, se queda quieto mientras dura
            float spawnTime = enemyAnimator != null ? enemyAnimator.SpawnDuration : 0f;
            if (spawnTime > 0f) spawnEndTime = Time.time + spawnTime;
            else FinishSpawn();
        }

        if (!agent.isOnNavMesh) return;

        if (state == State.Dormant)
        {
            if (PlayerRef.IsAlive && PlayerRef.FlatDistanceTo(transform.position) <= awakenRange)
            {
                Awaken(byPlayer: true);
            }
            return;
        }

        if (state == State.Spawning)
        {
            if (!awakenPending && Time.time >= spawnEndTime) FinishSpawn();
            return;
        }

        // Sin player o player muerto -> solo patrulla
        if (!PlayerRef.IsAlive)
        {
            if (state != State.Patrol) EnterPatrol();
            UpdatePatrol();
            return;
        }

        Transform player = PlayerRef.Transform;
        float distance = PlayerRef.FlatDistanceTo(transform.position);

        switch (state)
        {
            case State.Patrol:
                if (distance <= detectionRange) EnterChase();
                else UpdatePatrol();
                break;

            case State.Chase:
                if (distance > loseTargetRange)
                {
                    EnterPatrol();
                }
                else if (attack.IsInRange(player))
                {
                    EnterAttack();
                }
                else
                {
                    agent.SetDestination(player.position);
                    TurnBeforeMoving();
                }
                break;

            case State.Attack:
                if (!attack.IsInRange(player))
                {
                    EnterChase();
                    break;
                }
                TransformUtil.RotateTowardsFlat(transform, player.position, turnSpeed);
                attack.TryAttack(PlayerRef.Health);
                break;
        }
    }

    // La usa EnemyProfile (antes del Awake): reemplaza los valores del Inspector por los del EnemyStats
    public void ApplyStats(EnemyStats stats)
    {
        detectionRange = stats.DetectionRange;
        loseTargetRange = stats.LoseTargetRange;
        patrolSpeed = stats.PatrolSpeed;
        chaseSpeed = stats.ChaseSpeed;
    }

    // ---------- Estados ----------

    private void EnterDormant()
    {
        state = State.Dormant;
        awakenPending = false;
        health.Invulnerable = true; // la pila de huesos no recibe daño (pero un golpe la despierta)
        if (agent.isOnNavMesh) agent.ResetPath();
        SetVisible(!hideWhileDormant);
    }

    // Sale del piso: reproduce Spawn (si tiene). Es invulnerable hasta que termina la animación.
    private void Awaken(bool byPlayer)
    {
        state = State.Spawning;
        awakenPending = true;
        awakenedByPlayer = byPlayer;
        health.Invulnerable = true;
        spawnEndTime = float.MaxValue; // se define cuando se dispara la animación
        if (agent.isOnNavMesh) agent.ResetPath();
        SetVisible(true);
    }

    // Terminó de aparecer: ya se le puede pegar. Si lo despertó el player, lo persigue sin pasar por la patrulla.
    private void FinishSpawn()
    {
        health.Invulnerable = false;

        if (awakenedByPlayer && PlayerRef.IsAlive) EnterChase();
        else EnterPatrol();
    }

    private void EnterPatrol()
    {
        state = State.Patrol;
        agent.speed = patrolSpeed;
        agent.isStopped = false;
        waitTimer = patrolWaitTime;
        if (agent.isOnNavMesh) agent.ResetPath();
    }

    private void UpdatePatrol()
    {
        if (agent.pathPending) return;

        // Todavía yendo hacia el punto de patrulla
        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.1f)
        {
            TurnBeforeMoving();
            return;
        }

        waitTimer -= Time.deltaTime;
        if (waitTimer > 0f) return;
        waitTimer = patrolWaitTime;

        // Elegimos un punto al azar alrededor de su posición inicial
        Vector3 randomPoint = homePosition + Random.insideUnitSphere * patrolRadius;
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void EnterChase()
    {
        state = State.Chase;
        agent.speed = chaseSpeed;
        // El destino y el giro se manejan en Update (estado Chase)
    }

    private void EnterAttack()
    {
        state = State.Attack;
        StopMoving();
        attack.PrepareAttack(); // pequeña pausa antes del primer golpe
    }

    // ---------- Movimiento ----------

    // Si el próximo tramo del camino queda muy de costado o detrás, frena y gira en el lugar.
    // Cuando ya mira más o menos hacia ahí, el agente vuelve a caminar.
    private void TurnBeforeMoving()
    {
        if (!agent.hasPath || agent.pathPending)
        {
            agent.isStopped = false;
            return;
        }

        Vector3 dir = agent.steeringTarget - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f)
        {
            agent.isStopped = false;
            return;
        }

        if (Vector3.Angle(transform.forward, dir) > turnInPlaceAngle)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; // sin esto sigue "patinando" con la inercia
            TransformUtil.RotateTowardsFlat(transform, transform.position + dir, turnSpeed);
        }
        else
        {
            agent.isStopped = false;
        }
    }

    private void StopMoving()
    {
        if (!agent.isOnNavMesh) return;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    // ---------- Eventos de Health ----------

    private void HandleDamaged(int amount)
    {
        // Si le pegan dormido, se despierta
        if (state == State.Dormant)
        {
            Awaken(byPlayer: true);
            return;
        }

        // Si le pegan mientras patrulla, va directo a buscar al player
        if (state == State.Patrol && PlayerRef.IsAlive) EnterChase();
    }

    private void HandleDamageBlocked(int amount)
    {
        // Golpe a la pila de huesos: no hace daño, pero lo despierta
        if (state == State.Dormant) Awaken(byPlayer: true);
    }

    private void HandleDeath()
    {
        state = State.Dead;
        if (agent.isOnNavMesh) agent.ResetPath();
        agent.enabled = false;
    }

    // ---------- Utilidades ----------

    private void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
        {
            if (r != null) r.enabled = visible;
        }
    }

    // Dibuja los rangos en la Scene view al seleccionar el enemigo (ayuda a armar el nivel)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? homePosition : transform.position, patrolRadius);
        if (startDormant)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, awakenRange);
        }
    }
}

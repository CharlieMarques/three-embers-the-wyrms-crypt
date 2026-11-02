// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Guarda el último punto de control y reaparece al player ahí después de morir.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    public static event Action OnPlayerRespawned;

    [Tooltip("Segundos desde que muere hasta que reaparece (incluye la animación de muerte)")]
    [SerializeField] private float respawnDelay = 2f;
    [Tooltip("Segundos extra que el cuerpo queda visible después de la animación de muerte")]
    [SerializeField] private float corpseVisibleTime = 0.4f;
    [Tooltip("Si queda vacío, se usa la posición inicial del player")]
    [SerializeField] private Transform initialSpawn;
    [Tooltip("Si queda vacío, la busca en la Main Camera")]
    [SerializeField] private CameraFollow cameraFollow;

    private GameObject player;
    private Health playerHealth;
    private Mana playerMana;
    private PlayerController playerController;
    private PlayerMovement playerMovement;
    private CharacterAnimator playerAnimator; // opcional: para esperar la animación de muerte
    private Renderer[] playerRenderers;

    private Vector3 respawnPoint;
    private Waystone currentWaystone;

    private void Awake()
    {
        Instance = this;

        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("RespawnManager: no hay ningún objeto con tag Player en la escena.");
            enabled = false;
            return;
        }

        playerHealth = player.GetComponent<Health>();
        playerMana = player.GetComponent<Mana>();
        playerController = player.GetComponent<PlayerController>();
        playerMovement = player.GetComponent<PlayerMovement>();
        playerAnimator = player.GetComponent<CharacterAnimator>();
        playerRenderers = player.GetComponentsInChildren<Renderer>();

        if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();

        respawnPoint = initialSpawn != null ? initialSpawn.position : player.transform.position;
    }

    private void OnEnable()
    {
        if (playerHealth != null) playerHealth.OnDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnDeath -= HandlePlayerDeath;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Lo llama un Waystone cuando el player lo pisa
    public void SetCheckpoint(Waystone waystone)
    {
        if (currentWaystone == waystone) return;

        Waystone previous = currentWaystone;
        currentWaystone = waystone;
        respawnPoint = waystone.SpawnPoint;

        if (previous != null) previous.RefreshVisual();
        waystone.RefreshVisual();
    }

    // Lo llama un Altar al activarse (checkpoint sin waystone)
    public void SetCheckpoint(Vector3 point)
    {
        Waystone previous = currentWaystone;
        currentWaystone = null;
        respawnPoint = point;

        if (previous != null) previous.RefreshVisual();
    }

    public bool IsCurrent(Waystone waystone) => currentWaystone == waystone;

    private void HandlePlayerDeath()
    {
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        playerMovement.Stop();

        // Primero dejamos ver la animación de muerte (sin pasarnos del respawnDelay) y recién ahí lo ocultamos
        float deathTime = playerAnimator != null ? playerAnimator.DeathDuration + corpseVisibleTime : 0f;
        deathTime = Mathf.Min(deathTime, respawnDelay);
        if (deathTime > 0f) yield return new WaitForSeconds(deathTime);

        SetPlayerVisible(false);
        yield return new WaitForSeconds(respawnDelay - deathTime);

        // Nos aseguramos de que el punto esté sobre el NavMesh
        Vector3 point = respawnPoint;
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, 3f, NavMesh.AllAreas)) point = hit.position;

        playerController.Teleport(point);
        playerHealth.ResetHealth();
        if (playerMana != null) playerMana.ResetMana();

        SetPlayerVisible(true);
        if (cameraFollow != null) cameraFollow.SnapToTarget();

        OnPlayerRespawned?.Invoke();
    }

    private void SetPlayerVisible(bool visible)
    {
        foreach (Renderer r in playerRenderers)
        {
            if (r != null) r.enabled = visible;
        }
    }
}

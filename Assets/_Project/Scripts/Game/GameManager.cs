// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Flujo del nivel: panel de muerte, activación del portal al derrotar al jefe y fin del nivel.

using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Fin del nivel")]
    [Tooltip("El portal final, DESACTIVADO en la escena. Se activa al morir el boss.")]
    [SerializeField] private GameObject levelPortal;
    [SerializeField] private float portalDelay = 1.5f;
    [Tooltip("Vacío = mostrar pantalla de victoria. Con nombre = cargar esa escena (siguiente nivel).")]
    [SerializeField] private string nextSceneName = "";

    [Header("Pantallas")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject victoryPanel;

    private Health playerHealth;
    private bool levelCompleted;

    // Lo usa el PauseMenu para no pausar sobre la pantalla de victoria
    public bool IsLevelCompleted => levelCompleted;

    private void Awake()
    {
        Instance = this;

        if (deathPanel != null) deathPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (levelPortal != null) levelPortal.SetActive(false);
    }

    private void OnEnable()
    {
        BossAI.OnBossDefeated += HandleBossDefeated;
        RespawnManager.OnPlayerRespawned += HideDeathPanel;
    }

    private void OnDisable()
    {
        BossAI.OnBossDefeated -= HandleBossDefeated;
        RespawnManager.OnPlayerRespawned -= HideDeathPanel;
    }

    private void Start()
    {
        playerHealth = PlayerRef.Health;
        if (playerHealth != null) playerHealth.OnDeath += ShowDeathPanel;
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDeath -= ShowDeathPanel;
        if (Instance == this) Instance = null;
    }

    // ---------- Muerte ----------

    private void ShowDeathPanel()
    {
        if (deathPanel != null) deathPanel.SetActive(true);
    }

    private void HideDeathPanel()
    {
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    // ---------- Boss y portal ----------

    private void HandleBossDefeated(BossAI boss)
    {
        StartCoroutine(ShowPortalRoutine());
    }

    private IEnumerator ShowPortalRoutine()
    {
        yield return new WaitForSeconds(portalDelay);
        if (levelPortal != null) levelPortal.SetActive(true);
    }

    // Lo llama el LevelPortal cuando el player lo pisa
    public void CompleteLevel()
    {
        if (levelCompleted) return;
        levelCompleted = true;

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneLoader.Load(nextSceneName);
            return;
        }

        if (victoryPanel != null) victoryPanel.SetActive(true);
        Time.timeScale = 0f; // congela el juego detrás de la pantalla (el SceneLoader lo restaura)
    }

    // ---------- Botones ----------

    public void BackToMenu()
    {
        SceneLoader.LoadMainMenu();
    }

    public void Retry()
    {
        SceneLoader.ReloadCurrent();
    }
}

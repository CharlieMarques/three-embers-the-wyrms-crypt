// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Menú de pausa con ESC: congela el juego y ofrece continuar, reiniciar o volver al menú.

using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    public static bool IsPaused { get; private set; }

    private void Awake()
    {
        IsPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

        if (IsPaused) Resume();
        else Pause();
    }

    private void OnDestroy()
    {
        // Al cambiar de escena no queremos que quede marcado como pausado
        IsPaused = false;
    }

    public void Pause()
    {
        if (SceneLoader.IsLoading) return;
        if (GameManager.Instance != null && GameManager.Instance.IsLevelCompleted) return; // ya está en victoria

        IsPaused = true;
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        IsPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Restart()
    {
        SceneLoader.ReloadCurrent(); // el SceneLoader restaura timeScale al cargar
    }

    public void GoToMainMenu()
    {
        SceneLoader.LoadMainMenu();
    }
}

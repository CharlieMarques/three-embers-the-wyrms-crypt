// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Muestra el objetivo actual del nivel y el contador de enemigos derrotados.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class ObjectiveUI : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [Tooltip("Opcional: texto aparte para el contador. Si queda vacío, se muestra debajo del objetivo.")]
    [SerializeField] private TMP_Text enemiesText;
    [SerializeField] private bool showEnemyCounter = true;

    private string currentObjective = "";
    private readonly HashSet<EnemyDeath> levelEnemies = new HashSet<EnemyDeath>();
    private int totalEnemies;
    private int killedEnemies;

    private void OnEnable()
    {
        BossDoor.OnAltarProgress += HandleAltarProgress;
        BossDoor.OnDoorOpened += HandleDoorOpened;
        BossAI.OnBossActivated += HandleBossActivated;
        BossAI.OnBossReset += HandleBossReset;
        BossAI.OnBossDefeated += HandleBossDefeated;
        EnemyDeath.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        BossDoor.OnAltarProgress -= HandleAltarProgress;
        BossDoor.OnDoorOpened -= HandleDoorOpened;
        BossAI.OnBossActivated -= HandleBossActivated;
        BossAI.OnBossReset -= HandleBossReset;
        BossAI.OnBossDefeated -= HandleBossDefeated;
        EnemyDeath.OnAnyEnemyDied -= HandleEnemyDied;
    }

    private void Start()
    {
        // Enemigos activos al iniciar el nivel (los del pool están desactivados y no cuentan)
        foreach (EnemyDeath enemy in FindObjectsByType<EnemyDeath>(FindObjectsSortMode.None))
        {
            levelEnemies.Add(enemy);
        }
        totalEnemies = levelEnemies.Count;
        Refresh();
    }

    // ---------- Contador de enemigos ----------

    private void HandleEnemyDied(EnemyDeath enemy)
    {
        // Remove devuelve true solo la primera vez y solo si era un enemigo del nivel
        if (levelEnemies.Remove(enemy))
        {
            killedEnemies++;
            Refresh();
        }
    }

    // ---------- Objetivo ----------

    private void HandleAltarProgress(int activated, int total)
    {
        SetObjective($"Encendé las llamas: {activated}/{total}");
    }

    private void HandleDoorOpened()
    {
        SetObjective("La cripta del Dragón se abrió");
    }

    private void HandleBossActivated(BossAI boss)
    {
        SetObjective($"Derrotá al {boss.BossName}");
    }

    private void HandleBossReset(BossAI boss)
    {
        SetObjective($"Volvé a enfrentar al {boss.BossName}");
    }

    private void HandleBossDefeated(BossAI boss)
    {
        SetObjective("Entrá al portal");
    }

    // ---------- Texto ----------

    private void SetObjective(string value)
    {
        currentObjective = value;
        Refresh();
    }

    private void Refresh()
    {
        string counter = showEnemyCounter && totalEnemies > 0 ? $"Enemigos: {killedEnemies}/{totalEnemies}" : "";

        if (enemiesText != null)
        {
            if (text != null) text.text = currentObjective;
            enemiesText.text = counter;
        }
        else if (text != null)
        {
            text.text = string.IsNullOrEmpty(counter)
                ? currentObjective
                : $"{currentObjective}\n<size=80%>{counter}</size>";
        }
    }
}

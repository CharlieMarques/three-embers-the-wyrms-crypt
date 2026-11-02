// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Barra de vida del jefe con su nombre y fase; aparece al empezar la pelea.

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [Tooltip("Image con Image Type = Filled, Fill Method = Horizontal")]
    [SerializeField] private Image fill;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("Opcional: muestra 'Fase 2', 'Fase 3'")]
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private float smoothSpeed = 1.5f;
    [SerializeField] private float hideDelayOnDefeat = 1.5f;

    private BossAI boss;
    private float targetFill = 1f;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
    }

    private void OnEnable()
    {
        BossAI.OnBossActivated += Show;
        BossAI.OnBossReset += HandleReset;
        BossAI.OnBossDefeated += HandleDefeated;
    }

    private void OnDisable()
    {
        BossAI.OnBossActivated -= Show;
        BossAI.OnBossReset -= HandleReset;
        BossAI.OnBossDefeated -= HandleDefeated;
        Unbind();
    }

    private void Update()
    {
        if (fill != null) fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, targetFill, smoothSpeed * Time.deltaTime);
    }

    private void Show(BossAI newBoss)
    {
        StopAllCoroutines();
        Unbind();

        boss = newBoss;
        boss.Health.OnHealthChanged += UpdateBar;
        boss.OnPhaseChanged += UpdatePhase;

        if (nameText != null) nameText.text = boss.BossName;
        UpdatePhase(boss.Phase);

        targetFill = (float)boss.Health.Current / boss.Health.Max;
        if (fill != null) fill.fillAmount = targetFill;

        if (root != null) root.SetActive(true);
    }

    private void HandleReset(BossAI b)
    {
        Unbind();
        if (root != null) root.SetActive(false);
    }

    private void HandleDefeated(BossAI b)
    {
        targetFill = 0f;
        Unbind();
        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelayOnDefeat);
        if (root != null) root.SetActive(false);
    }

    private void UpdateBar(int current, int max)
    {
        targetFill = max > 0 ? (float)current / max : 0f;
    }

    private void UpdatePhase(int phase)
    {
        if (phaseText != null) phaseText.text = phase > 1 ? $"Fase {phase}" : "";
    }

    private void Unbind()
    {
        if (boss == null) return;
        boss.Health.OnHealthChanged -= UpdateBar;
        boss.OnPhaseChanged -= UpdatePhase;
        boss = null;
    }
}

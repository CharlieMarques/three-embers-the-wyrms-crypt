// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Orbe de vida o maná estilo Diablo II que se vacía según el valor del player.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrbUI : MonoBehaviour
{
    public enum Resource { Health, Mana }

    [SerializeField] private Resource resource = Resource.Health;
    [SerializeField] private Image fill;
    [Tooltip("Opcional: texto '75/100'")]
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private float smoothSpeed = 3f;

    private IResource source;
    private float targetFill = 1f;

    private void Start()
    {
        source = resource == Resource.Health ? (IResource)PlayerRef.Health : PlayerRef.Mana;
        if (source == null)
        {
            Debug.LogWarning($"OrbUI: el player no tiene {resource}.");
            return;
        }

        source.OnValueChanged += UpdateValue;
        UpdateValue(source.Current, source.Max);

        if (fill != null) fill.fillAmount = targetFill; // arranca lleno, sin animación
    }

    private void OnDestroy()
    {
        if (source != null) source.OnValueChanged -= UpdateValue;
    }

    private void Update()
    {
        if (fill != null) fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, targetFill, smoothSpeed * Time.deltaTime);
    }

    private void UpdateValue(float current, int max)
    {
        targetFill = max > 0 ? current / max : 0f;
        if (valueText != null) valueText.text = $"{Mathf.FloorToInt(current)}/{max}";
    }
}

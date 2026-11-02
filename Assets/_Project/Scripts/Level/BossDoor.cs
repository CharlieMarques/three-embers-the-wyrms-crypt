// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Puerta de la cripta del Dragón: se abre cuando las tres llamas están encendidas.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossDoor : MonoBehaviour
{
    public static event Action<int, int> OnAltarProgress;
    public static event Action OnDoorOpened;

    [Tooltip("Si queda vacío, usa TODOS los altares de la escena")]
    [SerializeField] private Altar[] requiredAltars;
    [Tooltip("La parte visual que baja al abrirse. Si queda vacío, baja el objeto entero.")]
    [SerializeField] private Transform doorVisual;
    [SerializeField] private float openDepth = 3f;
    [SerializeField] private float openDuration = 1.5f;

    public bool IsOpen { get; private set; }

    private void OnEnable()
    {
        Altar.OnAnyAltarActivated += HandleAltarActivated;
    }

    private void OnDisable()
    {
        Altar.OnAnyAltarActivated -= HandleAltarActivated;
    }

    private void Start()
    {
        if (requiredAltars == null || requiredAltars.Length == 0)
        {
            requiredAltars = FindObjectsByType<Altar>(FindObjectsSortMode.None);
        }
        if (doorVisual == null) doorVisual = transform;

        OnAltarProgress?.Invoke(CountActivated(), requiredAltars.Length);
    }

    private void HandleAltarActivated(Altar altar)
    {
        if (IsOpen) return;

        int activated = CountActivated();
        OnAltarProgress?.Invoke(activated, requiredAltars.Length);

        if (activated >= requiredAltars.Length) StartCoroutine(OpenRoutine());
    }

    private int CountActivated()
    {
        int count = 0;
        foreach (Altar a in requiredAltars)
        {
            if (a != null && a.IsActivated) count++;
        }
        return count;
    }

    private IEnumerator OpenRoutine()
    {
        IsOpen = true;

        // Liberar el paso: NavMesh y colisiones
        foreach (NavMeshObstacle obstacle in GetComponentsInChildren<NavMeshObstacle>()) obstacle.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

        OnDoorOpened?.Invoke();

        // Animación: la puerta se hunde en el piso
        Vector3 start = doorVisual.position;
        Vector3 end = start + Vector3.down * openDepth;
        float t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            doorVisual.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / openDuration));
            yield return null;
        }

        doorVisual.gameObject.SetActive(false);
    }
}

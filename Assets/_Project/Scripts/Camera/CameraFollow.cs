// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Cámara isométrica que sigue al player con suavizado, al estilo Diablo II.

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Ángulo isométrico")]
    [SerializeField] private float pitch = 50f;    // inclinación hacia abajo (90 = top view puro)
    [SerializeField] private float yaw = 45f;      // giro diagonal típico isométrico
    [SerializeField] private float distance = 18f; // qué tan lejos está la cámara

    [Header("Suavizado")]
    [SerializeField] private float smoothTime = 0.15f;

    private Vector3 velocity;

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desired = target.position - rotation * Vector3.forward * distance;

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        transform.rotation = rotation;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    // Salta directo a la posición (sin suavizado). Útil al respawnear.
    public void SnapToTarget()
    {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = target.position - rotation * Vector3.forward * distance;
        transform.rotation = rotation;
        velocity = Vector3.zero;
    }
}
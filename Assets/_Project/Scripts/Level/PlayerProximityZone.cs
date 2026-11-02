// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Clase base para todo lo que reacciona cuando el player entra, se queda o sale de un radio.

using UnityEngine;
using UnityEngine.Serialization;

public abstract class PlayerProximityZone : MonoBehaviour
{
    [Header("Zona")]
    // FormerlySerializedAs: conserva el valor que ya tenían en el Inspector con su nombre viejo
    [FormerlySerializedAs("pickupRadius")]
    [FormerlySerializedAs("activationRadius")]
    [SerializeField] protected float radius = 1.5f;

    protected bool PlayerInside { get; private set; }

    // Color del círculo en la Scene view
    protected virtual Color GizmoColor => Color.cyan;

    protected virtual void Update()
    {
        bool inside = PlayerRef.IsAlive && PlayerRef.FlatDistanceTo(transform.position) <= radius;

        if (inside && !PlayerInside)
        {
            PlayerInside = true;
            OnPlayerEnter();
        }
        else if (!inside && PlayerInside)
        {
            PlayerInside = false;
            OnPlayerExit();
        }

        if (inside) OnPlayerStay();
    }

    protected virtual void OnDisable()
    {
        // Al desactivarse (ej. vuelve al pool) se "olvida" del player, así la próxima vez detecta la entrada
        PlayerInside = false;
    }

    // El player entró al radio (una sola vez por entrada)
    protected virtual void OnPlayerEnter() { }
    // El player sigue adentro (cada frame, incluido el de entrada)
    protected virtual void OnPlayerStay() { }
    // El player salió del radio (o murió adentro)
    protected virtual void OnPlayerExit() { }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = GizmoColor;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}

// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Funciones comunes de posición y rotación: distancia en el plano, girar hacia un punto y buscar el modelo visual.

using UnityEngine;

public static class TransformUtil
{
    // Distancia en el plano (ignora la altura): en un juego top-down es la que importa
    public static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // Gira 'target' hacia 'point' solo en Y, como mucho 'degreesPerSecond' grados por segundo
    public static void RotateTowardsFlat(Transform target, Vector3 point, float degreesPerSecond)
    {
        Vector3 dir = point - target.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion look = Quaternion.LookRotation(dir);
        target.rotation = Quaternion.RotateTowards(target.rotation, look, degreesPerSecond * Time.deltaTime);
    }

    // El modelo visual: el asignado en el Inspector o, si no hay, el primer hijo
    public static Transform FindVisual(Transform root, Transform assigned)
    {
        if (assigned != null) return assigned;
        return root.childCount > 0 ? root.GetChild(0) : null;
    }

    // Los renderers asignados en el Inspector o, si no hay, todos los de los hijos
    public static Renderer[] RenderersOrChildren(Component root, Renderer[] assigned)
    {
        if (assigned != null && assigned.Length > 0) return assigned;
        return root.GetComponentsInChildren<Renderer>();
    }
}

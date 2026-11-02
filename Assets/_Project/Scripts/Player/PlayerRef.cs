// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Acceso global y cacheado al player (posición, vida, maná) para cualquier script, incluidos los enemigos del pool.

using UnityEngine;

public static class PlayerRef
{
    private static Transform cachedTransform;
    private static Health cachedHealth;
    private static Mana cachedMana;

    public static Transform Transform { get { Refresh(); return cachedTransform; } }
    public static Health Health { get { Refresh(); return cachedHealth; } }
    public static Mana Mana { get { Refresh(); return cachedMana; } }

    // true si existe un player vivo
    public static bool IsAlive
    {
        get
        {
            Refresh();
            return cachedTransform != null && cachedHealth != null && !cachedHealth.IsDead;
        }
    }

    private static void Refresh()
    {
        if (cachedTransform != null) return; // ya lo tenemos (si se recarga la escena, vuelve a buscar)

        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return;

        cachedTransform = go.transform;
        cachedHealth = go.GetComponent<Health>();
        cachedMana = go.GetComponent<Mana>();
    }

    // Distancia en el plano (ignora la altura)
    public static float FlatDistanceTo(Vector3 point)
    {
        Transform t = Transform;
        return t != null ? TransformUtil.FlatDistance(t.position, point) : float.MaxValue;
    }
}

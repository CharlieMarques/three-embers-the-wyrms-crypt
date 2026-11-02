// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Se agrega a cada objeto del pool: recuerda su prefab, lo devuelve al pool y avisa a sus componentes IPoolable.

using UnityEngine;

public class PooledObject : MonoBehaviour
{
    public GameObject Prefab { get; private set; }

    private ObjectPool pool;
    private IPoolable[] poolables;

    public void Init(ObjectPool owner, GameObject prefab)
    {
        pool = owner;
        Prefab = prefab;
        poolables = GetComponentsInChildren<IPoolable>(true);
    }

    public void ReturnToPool()
    {
        if (pool != null) pool.Release(this);
        else Destroy(gameObject);
    }

    // Los llama el ObjectPool
    public void NotifySpawned()
    {
        foreach (IPoolable p in poolables) p.OnSpawnFromPool();
    }

    public void NotifyReturned()
    {
        foreach (IPoolable p in poolables) p.OnReturnToPool();
    }

    // Devuelve el objeto al pool si salió de uno; si no, lo destruye.
    public static void Despawn(GameObject go)
    {
        PooledObject item = go.GetComponent<PooledObject>();
        if (item != null) item.ReturnToPool();
        else Destroy(go);
    }

    // Devuelve el objeto al pool si salió de uno; si no, solo lo desactiva (ej. enemigos puestos en la escena).
    public static void DespawnOrDisable(GameObject go)
    {
        PooledObject item = go.GetComponent<PooledObject>();
        if (item != null) item.ReturnToPool();
        else go.SetActive(false);
    }
}

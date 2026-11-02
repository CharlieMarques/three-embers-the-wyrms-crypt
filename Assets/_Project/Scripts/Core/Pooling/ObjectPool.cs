// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Pool de objetos: reutiliza instancias desactivadas en lugar de crear y destruir (proyectiles, enemigos, pociones, efectos).

using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [Serializable]
    public class PrewarmEntry
    {
        public GameObject prefab;
        public int amount = 10;
    }

    [Tooltip("Opcional: objetos que se crean al iniciar para que no haya tirones la primera vez")]
    [SerializeField] private PrewarmEntry[] prewarm;

    private static ObjectPool instance;

    public static ObjectPool Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ObjectPool>();
                if (instance == null) instance = new GameObject("ObjectPool").AddComponent<ObjectPool>();
            }
            return instance;
        }
    }

    // Una cola de objetos libres por cada prefab
    private readonly Dictionary<GameObject, Queue<PooledObject>> pools = new Dictionary<GameObject, Queue<PooledObject>>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (prewarm == null) return;
        foreach (PrewarmEntry entry in prewarm)
        {
            if (entry.prefab == null) continue;
            for (int i = 0; i < entry.amount; i++)
            {
                PooledObject item = Create(entry.prefab, transform.position, Quaternion.identity);
                item.gameObject.SetActive(false);
                GetQueue(entry.prefab).Enqueue(item);
            }
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        Queue<PooledObject> queue = GetQueue(prefab);

        // Sacamos uno libre (salteando los que se hayan destruido por algún motivo)
        PooledObject item = null;
        while (item == null && queue.Count > 0) item = queue.Dequeue();

        if (item == null)
        {
            // No había libres: creamos uno nuevo YA en su posición
            // (importante para enemigos con NavMeshAgent, que se ubican en el NavMesh al activarse)
            item = Create(prefab, position, rotation);
        }
        else
        {
            // Lo movemos mientras está desactivado y recién después lo activamos
            item.transform.SetPositionAndRotation(position, rotation);
        }

        item.gameObject.SetActive(true);
        item.NotifySpawned(); // ya está activo y ubicado: los IPoolable pueden prepararse
        return item.gameObject;
    }

    public void Release(PooledObject item)
    {
        if (item == null || !item.gameObject.activeSelf) return; // ya estaba devuelto

        item.NotifyReturned();
        item.gameObject.SetActive(false);
        GetQueue(item.Prefab).Enqueue(item);
    }

    private PooledObject Create(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject go = Instantiate(prefab, position, rotation, transform);

        PooledObject item = go.GetComponent<PooledObject>();
        if (item == null) item = go.AddComponent<PooledObject>();
        item.Init(this, prefab);
        return item;
    }

    private Queue<PooledObject> GetQueue(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Queue<PooledObject> queue))
        {
            queue = new Queue<PooledObject>();
            pools.Add(prefab, queue);
        }
        return queue;
    }
}

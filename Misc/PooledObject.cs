using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Events;

/// <summary>
/// Placed on the root GameObject of a prefab to give it automatic object pooling.
/// </summary>
public class PooledObject : MonoBehaviour
{
    private static Dictionary<int, Pool> pools = new Dictionary<int, Pool>();
    //private static Dictionary<int, ObjectPool<GameObject>> pools = new Dictionary<int, ObjectPool<GameObject>>();

    private class Pool
    {
        GameObject prefab;
        Queue<GameObject> pool = new Queue<GameObject>();
        int maxSize;
        int currentSize = 0;
        public bool ObjectAvaliable
        {
            get
            {
                return pool.Count > 0 || currentSize < maxSize;
            }
        }

        public Pool(int maxSize, GameObject prefab)
        {
            this.maxSize = maxSize;
            this.prefab = prefab;
        }

        public GameObject Get()
        {
            GameObject obj = null;

            //Debug.Log(pool.Count);
            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
                if (obj == null)
                {
                    //currentSize--;
                    obj = Get();
                }
            }
            else if (currentSize < maxSize)
            {
                currentSize++;
                obj = Instantiate(prefab);
            }

            //Debug.Log($"{prefab.GetInstanceID()} size: {currentSize}/{maxSize}");
            return obj;
        }

        public void Release(GameObject obj)
        {
            pool.Enqueue(obj);
        }

        public void Remove(GameObject obj)
        {
            currentSize--;
            Destroy(obj);
        }
    }

    [SerializeField]
    private Renderer[] renderers;
    [SerializeField]
    private int initialPoolSize = 10;
    [SerializeField]
    private int maximumPoolSize = 20;
    [SerializeField]
    private int poolID;
    [SerializeField]
    private GameObject prefab;
    private bool valid = false;

    public bool ObjectAvaliable => pools.ContainsKey(poolID) ? pools[poolID].ObjectAvaliable : true;
    public UnityEvent<GameObject> OnGet;
    public System.Action<GameObject> OnGetReset;
    public UnityEvent<GameObject> OnRelease;
    public System.Action<GameObject> OnReleaseReset;

    private void OnValidate()
    {
        if (gameObject.GetInstanceID() < 0) return;
        poolID = gameObject.GetInstanceID();
    }

    private void Start()
    {
        renderers = gameObject.GetComponentsInChildren<Renderer>();
    }

    private void OnDestroy()
    {
        RemoveObject();
    }

    public GameObject RequestObject()
    {
        CheckForPool();
        GameObject obj = pools[poolID].Get();
        if (obj == null) return null;
        obj.SetActive(true);
        obj.GetComponent<PooledObject>().prefab = prefab;
        obj.GetComponent<PooledObject>().valid = true;
        OnGet?.Invoke(obj);
        OnGetReset?.Invoke(obj);
        OnReleaseReset = null;
        OnGetReset = null;
        return obj;
    }

    public void ReleaseObject()
    {
        CheckForPool();
        if (!pools.ContainsKey(poolID))
        {
            Debug.LogWarning($"Pool for {poolID} not found! \nDestroying object instead");
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        gameObject.SetActive(false);
        OnRelease?.Invoke(gameObject);
        OnReleaseReset?.Invoke(gameObject);
        pools[poolID].Release(gameObject);
    }

    public void RemoveObject()
    {
        if (!valid) return;
        CheckForPool();
        if (!pools.ContainsKey(poolID))
        {
            Debug.LogWarning($"Pool for {poolID} not found! \nDestroying object instead");
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        pools[poolID].Remove(gameObject);
    }

    private bool IsVisible()
    {
        bool vis = false;
        foreach (Renderer r in renderers)
            if (r.isVisible)
            {
                vis = true;
                break;
            }
        return vis;
    }

    private void CheckForPool()
    {
        if (gameObject.GetInstanceID() > 0)
        {
            poolID = gameObject.GetInstanceID();
        }

        if (!pools.ContainsKey(poolID))
        {
            //ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            //    () => Instantiate(prefab),
            //    obj => { obj.SetActive(true); OnGet?.Invoke(obj); },
            //    obj => { OnRelease?.Invoke(obj); obj.SetActive(false); },
            //    obj => Destroy(obj),
            //    true, initialPoolSize, maximumPoolSize);
            //pools.Add(poolID, pool);
            //Debug.Log($"Creating new pool with ID {poolID}");
            pools.Add(poolID, new Pool(maximumPoolSize, prefab));
        }
    }
}

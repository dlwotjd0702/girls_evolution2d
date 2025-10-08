using UnityEngine;
using System.Collections.Generic;

public class SimpleUIPool : MonoBehaviour
{
    public GameObject prefab;
    public int defaultCount = 10;

    private readonly Queue<GameObject> pool = new Queue<GameObject>();
    public static SimpleUIPool Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SimpleUIPool] Duplicate instance detected. Destroying this.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!prefab)
        {
            Debug.LogError("[SimpleUIPool] Prefab is null.");
            return;
        }

        for (int i = 0; i < defaultCount; i++)
            AddNewToPool();
    }

    private void AddNewToPool()
    {
        var go = Instantiate(prefab, transform);
        go.SetActive(false);
        pool.Enqueue(go);
    }

    public GameObject Get(Transform parent = null)
    {
        if (!prefab)
        {
            Debug.LogError("[SimpleUIPool] Prefab is null.");
            return null;
        }
        if (pool.Count == 0) AddNewToPool();
        var go = pool.Dequeue();
        go.SetActive(true);
        if (parent != null) go.transform.SetParent(parent, false);

        var rt = go.transform as RectTransform;
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
        return go;
    }

    public void Return(GameObject go)
    {
        if (!go) return;
        go.SetActive(false);
        go.transform.SetParent(transform, false);
        pool.Enqueue(go);
    }
}
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
        Instance = this;
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
        if (pool.Count == 0)
            AddNewToPool();
        var go = pool.Dequeue();
        go.SetActive(true);
        if (parent != null)
            go.transform.SetParent(parent, false);
        // 위치는 여기서 건드리지 않음!
        return go;
    }

    public void Return(GameObject go)
    {
        go.SetActive(false);
        go.transform.SetParent(transform, false);
        pool.Enqueue(go);
    }
}
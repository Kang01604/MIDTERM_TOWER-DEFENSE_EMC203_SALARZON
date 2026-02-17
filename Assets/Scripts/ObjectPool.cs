using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance;

    [System.Serializable]
    public struct PoolConfig
    {
        public PoolType type;
        public GameObject prefab;
        public int initialSize;
    }

    public List<PoolConfig> configs;
    // Dictionary using ENUM instead of String/Tag
    private Dictionary<PoolType, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        Instance = this;
        InitializePools();
    }

    void InitializePools()
    {
        poolDictionary = new Dictionary<PoolType, Queue<GameObject>>();

        foreach (var config in configs)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < config.initialSize; i++)
            {
                GameObject obj = Instantiate(config.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(this.transform);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(config.type, objectPool);
        }
    }

    public GameObject Spawn(PoolType type, Vector3 position)
    {
        if (!poolDictionary.ContainsKey(type)) return null;

        GameObject obj = poolDictionary[type].Dequeue();
        
        obj.SetActive(true);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;

        poolDictionary[type].Enqueue(obj); // Put back in line
        return obj;
    }
}
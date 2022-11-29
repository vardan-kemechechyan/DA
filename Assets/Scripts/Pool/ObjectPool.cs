using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ObjectPool : MonoBehaviour
{
    private static ObjectPool instance;

    [SerializeField] List<Data> data;
    public Dictionary<string, Pool> pools;

    [SerializeField] Configuration configuration;

    public void Fill() 
    {
        data = new List<Data>();

        // Alphabet
        //data.Add(new Data 
        //{ 
        //    path = $"Alphabet", 
        //    size = 1, 
        //    prefabs = Resources.LoadAll($"Alphabet", typeof(GameObject)).Cast<GameObject>().ToArray() 
        //});

        // Props
        foreach (var location in configuration.locations) 
        {
            data.Add(new Data 
            { 
                path = $"Props/{location.id}", 
                size = configuration.sceneryPerLevel 
            });
        }

        foreach (var d in data) 
        {
            d.prefabs = Resources.LoadAll(d.path, typeof(GameObject)).Cast<GameObject>().ToArray();
        }
    }

    public void Initialize(Configuration config)
    {
        instance = this;

        pools = new Dictionary<string, Pool>();

        foreach (var d in data)
            pools.Add(d.path, new Pool { size = d.size, prefabs = d.prefabs});
    }

    [Serializable]
    public class Pool 
    {
        public int size;
        public GameObject[] prefabs;

        Dictionary<string, Queue<GameObject>> source = new Dictionary<string, Queue<GameObject>>();

        public int Count()
        {
            return source.Count;
        }

        public void Fill()
        {
            foreach (var s in source)
                foreach (var o in s.Value)
                    Destroy(o, 0.1f);

            source.Clear();

            var value = new Dictionary<string, Queue<GameObject>>();

            foreach (var prefab in prefabs)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();

                for (int i = 0; i < size; i++)
                {
                    if (!prefab)
                    {
                        Debug.LogError($"Pool has missing prefab {prefab.name}");
                        return;
                    }

                    var obj = Instantiate(prefab);
                    obj.name = obj.name.Replace("(Clone)", $"({i})");
                    obj.transform.SetParent(instance.transform);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                value.Add(prefab.name, objectPool);
            }

            source = value;
        }

        /// <summary>
        /// If name is null or empty will return random pooled object
        /// </summary>
        /// <param name="source"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public GameObject Spawn(string name = null)
        {
            var p = source.Keys;

            if (string.IsNullOrEmpty(name))
                name = source.Keys.ElementAt(UnityEngine.Random.Range(0, source.Keys.Count));
            //name = pool.ElementAt(UnityEngine.Random.Range(0, sources.Count)).Key;

            if (!source.ContainsKey(name))
            {
                Debug.LogWarning($"Pool {name} doesn't exist!");
                return null;
            }

            var obj = source[name].Dequeue();
            obj.SetActive(true);
            source[name].Enqueue(obj);

            return obj;
        }
    }

    [Serializable]
    public class Data 
    {
        public string path;
        public int size;
        public GameObject[] prefabs;
    }
}

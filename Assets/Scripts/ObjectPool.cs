using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : MonoBehaviour
{
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly Stack<T> _inactive = new Stack<T>();

    public ObjectPool(T prefab, Transform parent, int initialCapacity = 0)
    {
        _prefab = prefab;
        _parent = parent;

        // Pre-instantiate if needed
        for (int i = 0; i < initialCapacity; i++)
        {
            T instance = Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            _inactive.Push(instance);
        }
    }

    public T Get()
    {
        if (_inactive.Count > 0)
        {
            T instance = _inactive.Pop();
            instance.gameObject.SetActive(true);
            return instance;
        }
        else
        {
            // Instantiate new if pool exhausted
            T instance = Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(true);
            return instance;
        }
    }

    public void Return(T instance)
    {
        instance.gameObject.SetActive(false);
        _inactive.Push(instance);
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace GodTower.Effects
{
    /// <summary>Thin wrapper around <see cref="ObjectPool{T}"/> for pooled effect prefabs.</summary>
    public sealed class PrefabPool
    {
        readonly GameObject _prefab;
        readonly Transform _parent;
        readonly ObjectPool<GameObject> _pool;
        readonly HashSet<GameObject> _inUse = new HashSet<GameObject>();

        public PrefabPool(GameObject prefab, Transform parent, int prewarm = 0, int maxSize = 64)
        {
            _prefab = prefab;
            _parent = parent;
            _pool = new ObjectPool<GameObject>(Create, OnGet, OnRelease, OnDestroyItem, collectionCheck: false, prewarm, maxSize);

            var warm = new GameObject[prewarm];
            for (int i = 0; i < prewarm; i++) warm[i] = Get();
            for (int i = 0; i < prewarm; i++) Release(warm[i]);
        }

        public GameObject Get()
        {
            GameObject instance = _pool.Get();
            _inUse.Add(instance);
            return instance;
        }

        /// <summary>Returns an instance to the pool. Safe to call twice or with an already-destroyed object.</summary>
        public void Release(GameObject instance)
        {
            if (instance == null || !_inUse.Remove(instance)) return;
            _pool.Release(instance);
        }

        GameObject Create()
        {
            GameObject instance = Object.Instantiate(_prefab, _parent);
            instance.SetActive(false);
            return instance;
        }

        void OnGet(GameObject instance)
        {
            instance.transform.localScale = _prefab.transform.localScale;
            instance.SetActive(true);
        }

        static void OnRelease(GameObject instance) => instance.SetActive(false);

        static void OnDestroyItem(GameObject instance)
        {
            if (instance != null) Object.Destroy(instance);
        }
    }
}

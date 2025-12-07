using System.Collections.Generic;
using UnityEngine;

namespace TerrariumEngine
{
    /// <summary>
    /// Generic object pool for efficient object reuse
    /// </summary>
    /// <typeparam name="T">Type of object to pool (must inherit from MonoBehaviour)</typeparam>
    public class ObjectPool<T> where T : MonoBehaviour
    {
        private Queue<T> _pool = new Queue<T>();
        private T _prefab;
        private Transform _parent;
        private int _initialSize;
        private int _maxSize;
        
        public int ActiveCount { get; private set; }
        public int PooledCount => _pool.Count;
        public int TotalCount => ActiveCount + PooledCount;
        
        public ObjectPool(T prefab, Transform parent, int initialSize = 10, int maxSize = 100)
        {
            _prefab = prefab;
            _parent = parent;
            _initialSize = initialSize;
            _maxSize = maxSize;
            
            // Pre-populate the pool
            for (int i = 0; i < _initialSize; i++)
            {
                T obj = CreateNewObject();
                obj.gameObject.SetActive(false);
                _pool.Enqueue(obj);
            }
        }
        
        /// <summary>
        /// Get an object from the pool
        /// </summary>
        /// <returns>Pooled object or null if pool is empty and max size reached</returns>
        public T Get()
        {
            T obj;
            
            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else if (TotalCount < _maxSize)
            {
                obj = CreateNewObject();
            }
            else
            {
                Debug.LogWarning($"ObjectPool<{typeof(T).Name}>: Pool is full, cannot create more objects");
                return null;
            }
            
            // Ensure the object is active when retrieved from pool
            if (obj != null && !obj.gameObject.activeInHierarchy)
            {
                obj.gameObject.SetActive(true);
            }
            ActiveCount++;
            return obj;
        }
        
        /// <summary>
        /// Return an object to the pool
        /// </summary>
        /// <param name="obj">Object to return</param>
        public void Return(T obj)
        {
            if (obj == null) return;
            
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
            ActiveCount--;
        }
        
        /// <summary>
        /// Return all active objects to the pool
        /// </summary>
        public void ReturnAll()
        {
            // This is a simplified version - in a real implementation,
            // you'd need to track active objects to return them all
            Debug.LogWarning("ObjectPool.ReturnAll() is not fully implemented. Consider tracking active objects.");
        }
        
        /// <summary>
        /// Clear the entire pool
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                T obj = _pool.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }
            ActiveCount = 0;
        }
        
        private T CreateNewObject()
        {
            T obj = Object.Instantiate(_prefab, _parent);
            return obj;
        }
    }
}

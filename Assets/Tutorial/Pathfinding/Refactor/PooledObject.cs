using UnityEngine;

namespace RuntimePathfinding
{
    public class PooledObject : MonoBehaviour
    {
        public ObjectPool pool
        {
            get
            {
                return _poolInstance;
            }

            set
            {
                _poolInstance = value;
            }
        }

        [System.NonSerialized]
        ObjectPool _poolInstance;
        public void ReturnToPool()
        {
            if (pool)
            {
                pool.AddObject(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    
        public T GetPooledInstance<T> () where T : PooledObject
        {
            if (!_poolInstance)
            {
                _poolInstance = ObjectPool.GetPool(this);
            }
            return (T)_poolInstance.GetObject();
        }
    }
}
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

        /// <summary>
        /// 将对象回收之前需要进行的处理
        /// </summary>
        public virtual void BeforeReturnToPool()
        {

        }

        [System.NonSerialized]
        ObjectPool _poolInstance;
        public void ReturnToPool()
        {
            BeforeReturnToPool();
            if (pool)
            {
                pool.AddObject(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
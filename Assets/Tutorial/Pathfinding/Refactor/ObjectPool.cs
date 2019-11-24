using System.Collections.Generic;
using UnityEngine;
namespace RuntimePathfinding
{
    public class ObjectPool : MonoBehaviour
    {
        PooledObject _prefab;
        List<PooledObject> _availableObjectList = new List<PooledObject>();

        /// <summary>
        /// 获取一个GameObject的对象池
        /// </summary>
        /// <param name="prefab">要生成对象池的对象</param>
        public static ObjectPool GetPool(PooledObject prefab)
        {
            GameObject obj;
            ObjectPool pool;
            if (Application.isEditor)
            {
                obj = GameObject.Find(prefab.name + " Pool");
                if (obj)
                {
                    pool = obj.GetComponent<ObjectPool>();
                    if (pool)
                    {
                        return pool;
                    }
                }
            }
            obj = new GameObject(prefab.name + " Pool");
            pool = obj.AddComponent<ObjectPool>();
            pool._prefab = prefab;
            return pool;
        }

        /// <summary>
        /// 从对象池中获取一个可用对象
        /// </summary>
        public PooledObject GetObject()
        {
            PooledObject obj;
            int lastAvailableIndex = _availableObjectList.Count - 1;
            if(lastAvailableIndex >= 0)
            {
                obj = _availableObjectList[lastAvailableIndex];
                _availableObjectList.RemoveAt(lastAvailableIndex);
                obj.gameObject.SetActive(true);
            }
            else
            {
                obj = Instantiate<PooledObject>(_prefab);
                obj.transform.SetParent(transform, false);
                obj.pool = this;
            }
            return obj;
        }

        /// <summary>
        /// 向对象池添加一个对象
        /// </summary>
        public void AddObject(PooledObject obj)
        {
            obj.gameObject.SetActive(false);
            _availableObjectList.Add(obj);
        }
    }

}
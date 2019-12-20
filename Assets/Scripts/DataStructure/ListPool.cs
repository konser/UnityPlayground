using System;
using System.Collections.Generic;
using UnityEngine;

public static class ListPool<T>
{
    private static List<List<T>> pool = new List<List<T>>();
    private static HashSet<List<T>> inpoolSet = new HashSet<List<T>>();
    private static List<List<T>> largePool = new List<List<T>>(MAX_LARGE_POOL_SIZE);

    private const int LARGE_POOL_OBJECT_COUNT_THRESHOLD = 3000;
    private const int MAX_LARGE_POOL_SIZE = 10;

    public static List<T> GetList(int capacity)
    {
        lock (pool)
        {
            List<List<T>> currentPool = pool;
            if (capacity > LARGE_POOL_OBJECT_COUNT_THRESHOLD)
            {
                currentPool = largePool;
            }
            int index = FindListFromPool(capacity, currentPool);

            if (index != -1)
            {
                List<T> list = currentPool[index];
                inpoolSet.Remove(list);
                currentPool[index] = currentPool[currentPool.Count - 1];
                currentPool.RemoveAt(currentPool.Count-1);
                return list;
            }
            return new List<T>(capacity);
        }
    }

    public static void Release(List<T> list)
    {
        list.Clear();
        lock (pool)
        {
            if (inpoolSet.Add(list) == false)
            {
                Debug.LogError("Pool a list twice!!!");
                return;
            }

            if (list.Capacity > LARGE_POOL_OBJECT_COUNT_THRESHOLD)
            {
                largePool.Add(list);
                if (largePool.Count > MAX_LARGE_POOL_SIZE)
                {
                    largePool.RemoveAt(0);
                }
            }
            else
            {
                pool.Add(list);
            }
        }
    }

    private static int FindListFromPool(int capacity, List<List<T>> listPool)
    {
        for (int i = 0; i < listPool.Count; i++)
        {
            int index = listPool.Count - 1 - i;
            if (listPool[index].Capacity > capacity)
            {
                return index;
            }
        }
        return -1;
    }
}

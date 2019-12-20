using FibonacciHeap;
using System;
using System.Collections.Generic;

public interface IPriorityQueue<TElement,TPriority>
{
    void Insert(TElement item, TPriority priority);
    TElement Top();
    TElement Pop();
    int Count();
}

/// <summary>
/// 优先队列(FibonacciHeap 是最小堆，所以值小的优先)
/// </summary>
public class PriorityQueue<TElement,TPriority> : IPriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    private readonly FibonacciHeap<TElement, TPriority> heap;
    private Dictionary<TElement, FibonacciHeapNode<TElement, TPriority>> fibonacciNodeDic;
    public PriorityQueue(TPriority minPriority)
    {
        heap = new FibonacciHeap<TElement, TPriority>(minPriority);
        fibonacciNodeDic = new Dictionary<TElement, FibonacciHeapNode<TElement, TPriority>>(100);
    }

    /// <summary>
    /// 插入新结点
    /// </summary>
    public void Insert(TElement item, TPriority priority)
    {
        FibonacciHeapNode<TElement,TPriority> node = new FibonacciHeapNode<TElement, TPriority>(item, priority);
        heap.Insert(node);
        fibonacciNodeDic[item] = node;
    }

    /// <summary>
    /// 更新结点的值，新值更小才能成功更新
    /// </summary>
    public void Decrease(TElement item, TPriority priority)
    {
        if (fibonacciNodeDic.ContainsKey(item))
        {
            heap.DecreaseKey(fibonacciNodeDic[item], priority);
        }
    }

    public bool Contains(TElement item)
    {
        return fibonacciNodeDic.ContainsKey(item);
    }

    public TElement Top()
    {
        return heap.Min().Data;
    }
    public TElement Pop()
    {
        fibonacciNodeDic.Remove(heap.Min().Data);
        return heap.RemoveMin().Data;
    }

    public int Count()
    {
        return heap.Size();
    }

    public void ClearQueue()
    {
        heap.Clear();
        fibonacciNodeDic.Clear();
    }
}

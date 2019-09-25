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

    public PriorityQueue(TPriority minPriority)
    {
        heap = new FibonacciHeap<TElement, TPriority>(minPriority);
    }

    public void Insert(TElement item, TPriority priority)
    {
        heap.Insert(new FibonacciHeapNode<TElement, TPriority>(item,priority));
    }
    public TElement Top()
    {
        return heap.Min().Data;
    }
    public TElement Pop()
    {
        return heap.RemoveMin().Data;
    }

    public int Count()
    {
        return heap.Size();
    }
}

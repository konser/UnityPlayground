using System;
using System.Collections.Generic;
using System.Threading;
using Task = System.Threading.Tasks.Task;

public class ProducerConsumeQueue<T> : IDisposable where T : class
{
    private Queue<T> _taskQueue = new Queue<T>();
    private Action<T> _taskHandlerFunc;

    private EventWaitHandle _eventWaitHandle = new AutoResetEvent(false);
    private Task _worker;
    readonly object _locker = new object();
    public ProducerConsumeQueue(Action<T> handler)
    {
        this._taskHandlerFunc = handler;
        _worker = Task.Factory.StartNew(Work);
    }

    public void EnqueueTask(T task)
    {
        lock (_locker)
        {
            _taskQueue.Enqueue(task);
        }
        // 每当入队一个任务 通知工作线程执行
        _eventWaitHandle.Set();
    }
    public void Dispose()
    {
        EnqueueTask(null);
        // 等待当前的工作线程执行完
        _worker.Wait();
        _worker.Dispose();
        // 释放所有资源
        _eventWaitHandle.Close();
        UnityEngine.Debug.Log("Task over");
    }

    // 注意这里的_worker虽然是单独线程
    // 使用该类时不开新线程的话，这个循环还是主线程的，会卡死主线程
    void Work()
    {
        while (true)
        {
            T task = null;
            // 只有当队列里有Null任务（这里作为线程的终止条件）
            // 该线程才会退出循环
            lock (_locker)
            {
                if (_taskQueue.Count > 0)
                {
                    task = _taskQueue.Dequeue();
                    if (task == null)
                    {
                        return;
                    }
                }
            }
            // 任务不为空时 执行任务 进入下次循环
            if (task != null)
            {
                _taskHandlerFunc.Invoke(task);
            }
            // 任务为空 等待下一个任务，然后进入下次循环
            else
            {
                _eventWaitHandle.WaitOne();
            }

        }
    }
}

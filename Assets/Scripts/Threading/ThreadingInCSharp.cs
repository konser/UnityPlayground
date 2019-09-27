using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ThreadingInCSharp : MonoBehaviour
{
    public string instanceName = "Club 1";
    public int count;
    private static SemaphoreSlim _sem = new SemaphoreSlim(3);
    void Start()
    {
        // Test waitHandle
        //new Thread(Waiter).Start();
        //Thread.Sleep(TimeSpan.FromSeconds(2));
        //_waitHandle.Set();
        // 任务处理线程 不卡死主线程
        new Thread(ExecuteQueueTask).Start();
    }

    static void ExecuteQueueTask()
    {
        using (ProducerConsumeQueue<string> queue = new ProducerConsumeQueue<string>(Log))
        {
            queue.EnqueueTask("Start Log handler.");
            for (int i = 0; i < 10; i++)
            {
                queue.EnqueueTask("Handle log " + i);
            }
            queue.EnqueueTask("Complete");
        }
    }
    static void Log(string log)
    {
        Debug.Log(log);
        Thread.Sleep(1000);
    }

    void Enter(object id)
    {
        Debug.Log($"{instanceName}: {id} enter");
        _sem.Wait();
        Debug.Log($"{instanceName}: {id} is in");
        Thread.Sleep(1000*(int)id);
        Debug.Log($"{instanceName}: {id} is leave");
        _sem.Release();
    }

    static EventWaitHandle _waitHandle = new AutoResetEvent(false);
    void Waiter()
    {
        Debug.Log("Waiting...");
        _waitHandle.WaitOne();
        Debug.Log("Notified!!");
    }

}

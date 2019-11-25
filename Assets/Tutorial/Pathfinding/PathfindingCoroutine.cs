using System.Collections;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace RuntimePathfinding
{
    /// <summary>
    /// 封装寻路协程
    /// </summary>
    public class PathfindingCoroutine : IEnumerator
    {
        IEnumerator _routine;
        public bool isDone;
#if UNITY_EDITOR
        private float _runningTime;
        private System.Diagnostics.Stopwatch _stopwatch;
        private bool _inited;
        private void RecordRunningTime()
        {
            if (_inited == false)
            {
                _stopwatch = Stopwatch.StartNew();
                _inited = true;
            }
            else
            {
                _runningTime += _stopwatch.ElapsedMilliseconds;
            }
        }
#endif

        public PathfindingCoroutine(IEnumerator enumerator)
        {
            _routine = enumerator;
        }
        public object Current
        {
            get
            {
                return _routine.Current;
            }
        }

        public bool MoveNext()
        {
#if UNITY_EDITOR
            RecordRunningTime();
#endif
            bool next = _routine.MoveNext();
            if (next)
            {

            }
            else
            {
                isDone = true;
#if UNITY_EDITOR
                _stopwatch.Stop();
                Debug.Log("Coroutine total running time : " + _runningTime);
#endif
            }
            return next;
        }

        public void Reset()
        {
            _routine.Reset();
        }
    }
}